using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RepyPharma.Domain.Entities;
using RepyPharma.Models.Analytics;

namespace RepyPharma.Services.Import;

public partial class ConsumptionImportService
{
    private readonly SemaphoreSlim _combinedGate = new(1, 1);
    private readonly ConcurrentDictionary<Guid, PreparedMonthlyImport> _previews = new();

    public void DiscardCombinedPreview(Guid validationId) => _previews.TryRemove(validationId, out _);

    public async Task<ConsumptionReportsValidationDto> PrepareCombinedImportAsync(Stream? dailyStream, string dailyFileName,
        Stream? sectorStream, string sectorFileName, DateOnly referenceMonth,
        IProgress<ConsumptionImportProgressDto>? progress = null, CancellationToken cancellationToken = default)
    {
        var month = new DateOnly(referenceMonth.Year, referenceMonth.Month, 1);
        if (!await _combinedGate.WaitAsync(0, cancellationToken))
            return RejectedPreview(month, dailyFileName, sectorFileName, "OperationInProgress", "Já existe uma importação em processamento nesta sessão.");
        var watch = Stopwatch.StartNew();
        try
        {
            foreach (var (id, preview) in _previews)
                if (preview.ExpiresAt <= clock.GetUtcNow() || preview.Parsed.Result.ConsumptionPeriod!.StartDate == month) _previews.TryRemove(id, out _);
            if (dailyStream is null || sectorStream is null || string.IsNullOrWhiteSpace(dailyFileName) || string.IsNullOrWhiteSpace(sectorFileName))
                return RejectedPreview(month, dailyFileName, sectorFileName, "BothReportsRequired", "Anexe os dois relatórios CSV obrigatórios.");
            if (!string.Equals(Path.GetExtension(sectorFileName), ".csv", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(sectorFileName).Length > 255)
                return RejectedPreview(month, dailyFileName, sectorFileName, "InvalidSectorFile", "O relatório setorial deve ser um CSV com nome de até 255 caracteres.");
            logger.LogInformation("Iniciando prévia conjunta de {Month}: diário {Daily}, setorial {Sector}.", month, dailyFileName, sectorFileName);
            progress?.Report(new("Lendo relatório de consumo diário..."));
            var dailyBytes = await ReadBoundedAsync(dailyStream, cancellationToken);
            var parsed = await Task.Run(async () =>
            {
                using var input = new MemoryStream(dailyBytes, writable: false);
                return await ReadAndValidateAsync(input, dailyFileName, new() { ReferenceMonth = month }, cancellationToken, closedMonthly: true);
            }, cancellationToken);
            progress?.Report(new("Processando relatório de saídas por setor..."));
            var sectorBytes = await ReadBoundedAsync(sectorStream, cancellationToken);
            var sector = await Task.Run(async () =>
            {
                using var input = new MemoryStream(sectorBytes, writable: false);
                return await sectorImporter.ProcessSectorConsumptionCsvAsync(input, sectorFileName, progress, cancellationToken);
            }, cancellationToken);
            progress?.Report(new("Associando preços aos códigos dos produtos..."));
            var codes = parsed.Monthly.Select(p => p.SoulMvProductCode!).Distinct().ToArray();
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            var catalog = await context.Items.AsNoTracking().Where(i => codes.Contains(i.Code)).ToDictionaryAsync(i => i.Code, cancellationToken);
            if (parsed.Monthly.Count > 0 && parsed.Monthly.All(m => m.IsComplete))
            {
                var periodEnd = month.AddDays(DateTime.DaysInMonth(month.Year, month.Month) - 1);
                var previousDaily = await context.DailyConsumptions.AsNoTracking().Where(d => d.Date >= month && d.Date <= periodEnd).ToListAsync(cancellationToken);
                var previousMonthly = await context.MonthlyConsumptions.AsNoTracking().Where(m => m.Year == month.Year && m.Month == month.Month).ToListAsync(cancellationToken);
                var incoming = parsed.Monthly.Where(m => catalog.ContainsKey(m.SoulMvProductCode!)).Select(m => catalog[m.SoulMvProductCode!].Id).ToArray();
                ValidatePreviousMonthlyCoverage(incoming, previousDaily, previousMonthly, new() { StartDate = month, EndDate = periodEnd }, (List<ConsumptionImportIssueDto>)parsed.Result.Issues);
            }
            var validation = await Task.Run(() => ValidateConsumptionImport(parsed, sector, catalog, month), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (validation.CanConfirm)
            {
                // A confirmação recebe somente o token: dados, competência e preços vêm deste snapshot privado.
                // Nenhuma entidade EF ou coleção diária mutável é enviada ao componente.
                parsed.Result.Issues = validation.Issues.Select(CloneIssue).ToList();
                _previews[validation.ValidationId] = new(parsed, sector.SourceFileName, validation.ExpiresAt,
                    validation.Products.ToDictionary(p => p.ProductCode, p => p.UnitPurchasePrice),
                    catalog.ToDictionary(p => p.Key, p => new CatalogIdentity(p.Value.Id, Label(p.Value.Unit))));
            }
            logger.LogDebug("Hashes dos documentos validados: diário {DailyHash}, setorial {SectorHash}.",
                Convert.ToHexString(SHA256.HashData(dailyBytes)), Convert.ToHexString(SHA256.HashData(sectorBytes)));
            logger.LogInformation("Prévia de {Month}: {DailyRecords} diários, {MonthlyRecords} mensais, {Prices} preços informados, {Errors} impeditivas. Confirmável {CanConfirm}. Duração {Elapsed}.",
                month, validation.DailyRecordCount, validation.MonthlyRecordCount, validation.AssignedPriceCount,
                validation.SignificantDiscrepancyCount, validation.CanConfirm, watch.Elapsed);
            foreach (var issue in validation.Issues)
                logger.Log(issue.Severity >= AnalysisSeverity.Error ? LogLevel.Error : LogLevel.Warning,
                    "Conciliação {Code}, produto {Product}, linha {Row}: {Message}", issue.Code, issue.ItemCode, issue.RowNumber, issue.Message);
            return validation;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is IOException or OverflowException or NpgsqlException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Falha na prévia conjunta de {Month}; nenhum dado foi gravado.", month);
            return RejectedPreview(month, dailyFileName, sectorFileName, "PreviewFailure", "Não foi possível concluir a prévia. Verifique os arquivos e a conexão com o cadastro. Nenhum consumo foi gravado.");
        }
        finally { _combinedGate.Release(); }
    }

    public async Task<CombinedConsumptionImportResultDto> ConfirmCombinedImportAsync(Guid validationId,
        IProgress<ConsumptionImportProgressDto>? progress = null, CancellationToken cancellationToken = default)
    {
        if (!await _combinedGate.WaitAsync(0, cancellationToken))
            return FailedConfirmation(validationId, "OperationInProgress", "Já existe uma importação em processamento nesta sessão.");
        var watch = Stopwatch.StartNew();
        PreparedMonthlyImport? snapshot = null;
        try
        {
            if (!_previews.TryRemove(validationId, out snapshot) || snapshot.ExpiresAt <= clock.GetUtcNow())
                return FailedConfirmation(validationId, "InvalidPreview", "Prévia inválida, expirada ou já utilizada. Processe novamente os dois relatórios.");
            progress?.Report(new("Validando condições para persistência..."));
            await PersistAsync(snapshot.Parsed, cancellationToken, snapshot, progress);
            logger.LogInformation("Confirmação conjunta {ValidationId}: {Status}, diário {DailyFile}, setorial {SectorFile}, duração {Elapsed}.",
                validationId, snapshot.Parsed.Result.Status, snapshot.Parsed.Result.SourceFileName, snapshot.SectorFileName, watch.Elapsed);
            return CombinedResult(validationId, snapshot);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Importação conjunta {ValidationId} interrompida; a transação não confirmada será revertida.", validationId);
            if (snapshot is null) return new() { ValidationId = validationId, Status = ConsumptionOperationStatus.Cancelled };
            snapshot.Parsed.Result.Status = ConsumptionOperationStatus.Cancelled;
            return CombinedResult(validationId, snapshot);
        }
        catch (Exception ex) when (ex is DbUpdateException or NpgsqlException or InvalidOperationException or OverflowException)
        {
            logger.LogError(ex, "Falha na confirmação conjunta {ValidationId}. A transação não confirmada foi revertida.", validationId);
            var result = snapshot is null ? new CombinedConsumptionImportResultDto { ValidationId = validationId } : CombinedResult(validationId, snapshot);
            result.Status = ConsumptionOperationStatus.Failed;
            result.Issues = result.Issues.Append(new() { Code = "PersistenceFailure", Severity = AnalysisSeverity.Error,
                Message = "A atualização não foi concluída. Processe novamente os relatórios antes de tentar outra confirmação." }).ToArray();
            return result;
        }
        finally { _combinedGate.Release(); }
    }

    private ConsumptionReportsValidationDto ValidateConsumptionImport(ParsedImport parsed, SectorConsumptionReportDto sector,
        Dictionary<string, Item> catalog, DateOnly month)
    {
        var issues = parsed.Result.Issues.Select(CloneIssue).Concat(sector.Issues.Select(CloneIssue)).ToList();
        var prices = sector.Products.ToDictionary(p => p.ProductCode);
        var products = new List<ConsumptionDiscrepancyDto>();
        // O diário é a fonte exclusiva do histórico. O outro CSV fornece somente o preço por código.
        // Não se comparam competências, unidades, quantidades, totais ou conjuntos dos dois arquivos.
        foreach (var daily in parsed.Monthly.OrderBy(d => d.SoulMvProductCode, StringComparer.Ordinal))
        {
            var code = daily.SoulMvProductCode!;
            catalog.TryGetValue(code, out var item);
            prices.TryGetValue(code, out var source);
            var categories = new List<ConsumptionCorrespondence>();
            var productIssues = issues.Where(i => i.ItemCode == code).ToList();
            if (item is null)
            {
                categories.Add(ConsumptionCorrespondence.UnknownProduct);
                AddIssue(productIssues, "UnknownItem", "Código do consumo diário não cadastrado em items.Code.", item: code);
            }
            else if (Label(item.Unit) != Label(daily.Unit!))
                AddIssue(productIssues, "RegisteredUnitMismatch", $"Unidade do diário '{daily.Unit}' difere do cadastro '{item.Unit}'.", item: code);

            // O serviço setorial resolve o preço único ou ponderado. O diário nunca fornece os pesos do custo.
            var price = source?.UnitPurchasePrice;
            if (price.HasValue) categories.Add(ConsumptionCorrespondence.UnitPriceAvailable);
            else
            {
                categories.Add(ConsumptionCorrespondence.FinancialUnavailable);
                if (source is null)
                {
                    var exception = new UnitPriceProcessingException("MissingUnitPrice", code,
                        "Nenhum registro de preço disponível para o produto do diário; não será atribuído um custo.");
                    logger.LogError(exception, "Falha no custo mensal do produto {ProductCode}: {Code}.", code, exception.Code);
                    AddIssue(productIssues, exception.Code, exception.Message, item: code);
                }
            }
            products.Add(new()
            {
                ProductCode = code, Description = daily.Description ?? "", DailyUnit = daily.Unit, RegisteredUnit = item?.Unit,
                DailyQuantity = daily.TotalQuantity, AverageDailyConsumption = daily.AverageDailyConsumption, UnitPurchasePrice = price,
                Categories = categories.ToArray(), Issues = productIssues.ToArray()
            });
            foreach (var issue in productIssues) if (!issues.Contains(issue)) issues.Add(issue);
        }
        return new()
        {
            ValidationId = Guid.NewGuid(), ReferenceMonth = month, DailyFileName = parsed.Result.SourceFileName,
            SectorFileName = sector.SourceFileName, ExpiresAt = clock.GetUtcNow().AddMinutes(reportOptions.Value.PreviewLifetimeMinutes),
            DailyProductCount = parsed.Monthly.Count, SectorProductCount = sector.Products.Count,
            DailyRecordCount = parsed.Daily.Count, MonthlyRecordCount = parsed.Monthly.Count,
            AssignedPriceCount = products.Count(p => p.UnitPurchasePrice.HasValue),
            SignificantDiscrepancyCount = issues.Where(i => i.ItemCode is not null && i.Severity >= AnalysisSeverity.Error).Select(i => i.ItemCode).Distinct().Count()
                + issues.Count(i => i.ItemCode is null && i.Severity >= AnalysisSeverity.Error),
            MissingPriceCount = products.Count(p => !p.UnitPurchasePrice.HasValue),
            UnknownProductCount = products.Count(p => p.Categories.Contains(ConsumptionCorrespondence.UnknownProduct)),
            SectorCount = sector.SectorCount, Products = products.ToArray(), Issues = issues.ToArray(),
            CanConfirm = parsed.Monthly.Count > 0 && !HasErrors(issues)
        };
    }

    private async Task<byte[]> ReadBoundedAsync(Stream input, CancellationToken token)
    {
        if (!input.CanRead) throw new InvalidDataException("Arquivo ilegível.");
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await input.ReadAsync(buffer, token)) > 0)
        {
            if (output.Length + read > reportOptions.Value.MaximumFileSize) throw new InvalidDataException("Arquivo acima do tamanho permitido.");
            await output.WriteAsync(buffer.AsMemory(0, read), token);
        }
        return output.ToArray();
    }
    private static ConsumptionReportsValidationDto RejectedPreview(DateOnly month, string daily, string sector, string code, string message)
        => new() { ValidationId = Guid.NewGuid(), ReferenceMonth = month, DailyFileName = Path.GetFileName(daily), SectorFileName = Path.GetFileName(sector),
            Issues = [new() { Code = code, Message = message, Severity = AnalysisSeverity.Error }] };
    private static CombinedConsumptionImportResultDto FailedConfirmation(Guid id, string code, string message)
        => new() { ValidationId = id, Status = ConsumptionOperationStatus.Failed, Issues = [new() { Code = code, Message = message, Severity = AnalysisSeverity.Error }] };
    private static CombinedConsumptionImportResultDto CombinedResult(Guid id, PreparedMonthlyImport snapshot)
    {
        var r = snapshot.Parsed.Result;
        return new() { ValidationId = id, ImportId = r.ImportId, SourceFileName = r.SourceFileName,
            SectorSourceFileName = snapshot.SectorFileName, Status = r.Status, StartedAt = r.StartedAt, CompletedAt = r.CompletedAt,
            ConsumptionPeriod = r.ConsumptionPeriod, Issues = r.Issues.ToArray() };
    }
    private static ConsumptionImportIssueDto CloneIssue(ConsumptionImportIssueDto i) => new()
    { Code = i.Code, Message = i.Message, Severity = i.Severity, RowNumber = i.RowNumber, ColumnName = i.ColumnName, ItemCode = i.ItemCode };
    private sealed record CatalogIdentity(int Id, string Unit);
    private sealed record PreparedMonthlyImport(ParsedImport Parsed, string SectorFileName, DateTimeOffset ExpiresAt,
        IReadOnlyDictionary<string, decimal?> Prices, IReadOnlyDictionary<string, CatalogIdentity> Catalog);
}
