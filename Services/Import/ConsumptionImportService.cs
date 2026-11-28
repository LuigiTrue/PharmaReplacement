using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.Extensions.Options;
using RepyPharma.Data;
using RepyPharma.Domain.Entities;
using RepyPharma.Models.Analytics;
using RepyPharma.Services.Import.Interfaces;

namespace RepyPharma.Services.Import;

/// <summary>Validação e atualização transacional do consumo informado pelo Soul MV.</summary>
public partial class ConsumptionImportService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<ConsumptionImportService> logger,
    TimeProvider clock,
    ISectorConsumptionImportService sectorImporter,
    IOptions<ConsumptionReportsImportOptions> reportOptions) : IConsumptionImportService
{
    private static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly TimeZoneInfo Brasilia = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public async Task<ConsumptionImportValidationDto> ValidateImportAsync(Stream csvStream, string fileName,
        ConsumptionImportOptionsDto? options = null, CancellationToken cancellationToken = default)
    {
        var parsed = await ReadAndValidateAsync(csvStream, fileName, options, cancellationToken);
        return new() { IsValid = parsed.Result.Status == ConsumptionOperationStatus.Pending,
            DetectedPeriod = parsed.Result.ConsumptionPeriod, Issues = parsed.Result.Issues };
    }

    public async Task<ConsumptionImportResultDto> ImportDailyConsumptionAsync(Stream csvStream, string fileName,
        ConsumptionImportOptionsDto? options = null, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), Brasilia).DateTime);
        if (options?.ReferenceMonth is { } reference && new DateOnly(reference.Year, reference.Month, 1) < new DateOnly(today.Year, today.Month, 1))
            return new() { SourceFileName = Path.GetFileName(fileName), Status = ConsumptionOperationStatus.Failed,
                Issues = [new() { Code = "BothReportsRequired", Severity = AnalysisSeverity.Error,
                    Message = "Competências encerradas exigem os relatórios diário e setorial, prévia e confirmação conjunta. O envio diário isolado é reservado ao mês atual." }] };
        var parsed = await ReadAndValidateAsync(csvStream, fileName, options, cancellationToken);
        if (parsed.Result.Status != ConsumptionOperationStatus.Pending) return parsed.Result;
        try
        {
            await PersistAsync(parsed, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            parsed.Result.Status = ConsumptionOperationStatus.Cancelled;
        }
        catch (Exception ex) when (ex is DbUpdateException or NpgsqlException or InvalidOperationException)
        {
            logger.LogError(ex, "Falha ao gravar consumo da competência {Month}.", parsed.Result.ConsumptionPeriod!.StartDate);
            parsed.Result.Status = ConsumptionOperationStatus.Failed;
            parsed.Result.Issues = parsed.Result.Issues.Append(new ConsumptionImportIssueDto
            {
                Code = "PersistenceFailure", Severity = AnalysisSeverity.Error,
                Message = "Não foi possível concluir a gravação. Verifique a conexão e as migrations do banco antes de tentar novamente."
            }).ToArray();
        }
        return parsed.Result;
    }

    public async Task<ConsumptionImportResultDto?> GetImportResultAsync(Guid importId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var record = await context.ConsumptionImports.AsNoTracking().SingleOrDefaultAsync(r => r.Id == importId, cancellationToken);
        return record is null ? null : ToResult(record);
    }

    public async Task<IReadOnlyList<ConsumptionImportResultDto>> GetImportsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var records = await context.ConsumptionImports.AsNoTracking().OrderByDescending(r => r.ImportedAt).ToListAsync(cancellationToken);
        return records.Select(ToResult).ToArray();
    }

    private static ConsumptionImportResultDto ToResult(ConsumptionImportRecord record) => new()
    {
        ImportId = record.Id, SourceFileName = record.SourceFileName, Status = ConsumptionOperationStatus.Completed,
        CompletedAt = record.ImportedAt,
        ConsumptionPeriod = new() { StartDate = record.ReferenceMonth, EndDate = record.RecordedThrough }
    };

    private async Task<ParsedImport> ReadAndValidateAsync(Stream stream, string fileName, ConsumptionImportOptionsDto? options,
        CancellationToken cancellationToken, bool closedMonthly = false)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var issues = new List<ConsumptionImportIssueDto>();
        var result = new ConsumptionImportResultDto
        {
            ImportId = Guid.NewGuid(), SourceFileName = Path.GetFileName(fileName),
            StartedAt = clock.GetUtcNow(), Status = ConsumptionOperationStatus.Failed, Issues = issues
        };
        var daily = new List<DailyConsumptionDto>();
        var monthly = new List<MonthlyConsumptionDto>();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!stream.CanRead) AddIssue(issues, "UnreadableStream", "O arquivo não permite leitura.");
            if (!string.Equals(Path.GetExtension(fileName), ".csv", StringComparison.OrdinalIgnoreCase) || result.SourceFileName.Length > 255)
                AddIssue(issues, "InvalidFileName", "Selecione um arquivo CSV com nome de até 255 caracteres.");
            if (options?.ReferenceMonth is not { } reference)
            {
                AddIssue(issues, "ReferenceMonthRequired", "A competência do card é obrigatória.");
                return new(result, daily, monthly);
            }
            var month = new DateOnly(reference.Year, reference.Month, 1);
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), Brasilia).DateTime);
            var currentMonth = new DateOnly(today.Year, today.Month, 1);
            if (closedMonthly && month >= currentMonth)
                AddIssue(issues, "OpenMonthlyPeriod", "O fechamento conjunto exige uma competência encerrada. Para o mês atual, utilize o fluxo parcial existente.");
            if (month > currentMonth)
                AddIssue(issues, "FutureMonth", "Não é possível importar consumo de uma competência futura.");
            var end = month == currentMonth ? today : month.AddDays(DateTime.DaysInMonth(month.Year, month.Month) - 1);
            result.ConsumptionPeriod = new() { StartDate = month, EndDate = end };
            if (HasErrors(issues)) return new(result, daily, monthly);

            var headerDays = new HashSet<int>();
            var rows = await ReadCsvAsync(stream, month, headerDays, issues, cancellationToken);
            if (rows.Count == 0) AddIssue(issues, "NoProducts", "Nenhuma linha válida de produto encontrada.");
            var expectedDays = Enumerable.Range(1, end.Day).ToHashSet();
            if (!headerDays.SetEquals(expectedDays))
                AddIssue(issues, "InvalidReportPeriod", $"O relatório do card {month:MM/yyyy} deve conter exatamente os dias 01 a {end.Day:00}. " +
                    "Meses anteriores exigem o mês completo; o mês atual exige os dias de 1 até hoje, no horário de Brasília.");
            var products = MergeProductRecords(rows, issues, cancellationToken);
            daily = NormalizeDailyConsumptions(products, month, issues, cancellationToken);
            ValidateConsumptionRecords(products, daily, expectedDays, issues);
            monthly = CalculateMonthlyConsumptions(products, daily, month);
            ValidateMonthlyTotals(monthly, issues);
            if (!HasErrors(issues)) result.Status = ConsumptionOperationStatus.Pending;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            result.Status = ConsumptionOperationStatus.Cancelled;
        }
        catch (Exception ex) when (ex is CsvFormatException or IOException or DecoderFallbackException or OverflowException)
        {
            AddIssue(issues, "ReadFailure", "Não foi possível interpretar o CSV. Confira a estrutura, as aspas e os valores numéricos.");
            logger.LogWarning(ex, "CSV de consumo inválido: {File}.", result.SourceFileName);
        }
        return new(result, daily, monthly);
    }

    private static async Task<List<ProductRow>> ReadCsvAsync(Stream stream, DateOnly month, HashSet<int> headerDays,
        List<ConsumptionImportIssueDto> issues, CancellationToken cancellationToken)
    {
        using var csv = CsvReportFormat.CreateReader(stream);
        Header? header = null;
        var foundHeader = false;
        var rows = new List<ProductRow>();
        while (await csv.ReadAsync(cancellationToken))
        {
            var fields = Enumerable.Range(0, csv.FieldCount).Select(csv.GetString).ToArray();
            if (fields.All(string.IsNullOrWhiteSpace)) continue;
            if (Label(fields.FirstOrDefault(f => !string.IsNullOrWhiteSpace(f)) ?? string.Empty) == "PRODUTO")
            {
                foundHeader = true;
                header = ParseHeader(fields, csv.LineNumber, month, issues);
                if (header is not null) headerDays.UnionWith(header.Days.Values);
                continue;
            }
            if (header is null)
            {
                AddIssue(issues, "MissingHeader", "Linha sem cabeçalho válido anterior.", csv.LineNumber);
                continue;
            }
            var row = ParseProductRow(fields, header, csv.LineNumber, issues);
            if (row is not null) rows.Add(row);
        }
        if (!foundHeader) AddIssue(issues, "NoHeaders", "Nenhum cabeçalho Soul MV identificado.");
        return rows;
    }
    private static Header? ParseHeader(string[] fields, long row, DateOnly month, List<ConsumptionImportIssueDto> issues)
    {
        var code = Array.FindIndex(fields, f => Label(f) == "PRODUTO");
        var unit = Array.FindIndex(fields, f => Label(f) is "UNIDADE" or "UNIDADE DE CONSUMO");
        var description = Array.FindIndex(fields, f => Label(f) is "DESCRICAO" or "DESCRICAO DO PRODUTO");
        // No relatório Soul MV a descrição está na coluna sem título entre Produto e Unidade.
        if (description < 0 && code >= 0 && code + 1 < unit) description = code + 1;
        var days = new Dictionary<int, int>();
        var invalid = false;
        for (var index = 0; index < fields.Length; index++)
        {
            if (!int.TryParse(fields[index].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var day)) continue;
            if (day < 1 || day > DateTime.DaysInMonth(month.Year, month.Month) || days.ContainsValue(day))
            {
                AddIssue(issues, "InvalidHeaderDay", $"Dia inválido ou repetido no cabeçalho: {day}.", row, fields[index]);
                invalid = true;
            }
            else days.Add(index, day);
        }
        var total = Array.FindIndex(fields, f => Label(f) == "TOTAL");
        var average = Array.FindIndex(fields, f => Label(f) == "MEDIA");
        if (code < 0 || unit < 0 || description < 0 || (days.Count == 0 && total < 0 && average < 0))
        {
            AddIssue(issues, "InvalidHeader", "Cabeçalho sem Produto, descrição, Unidade ou dias válidos.", row);
            return null;
        }
        return invalid ? null : new(code, description, unit, days, total, average);
    }

    private static ProductRow? ParseProductRow(string[] fields, Header header, long row, List<ConsumptionImportIssueDto> issues)
    {
        var code = Field(fields, header.Code).Trim();
        var description = Field(fields, header.Description).Trim();
        var unit = Field(fields, header.Unit).Trim();
        if (code.Length == 0 || !code.All(char.IsAsciiDigit) || description.Length == 0 || unit.Length == 0)
        {
            AddIssue(issues, "InvalidProduct", "Código numérico, descrição e unidade são obrigatórios.", row, item: code);
            return null;
        }
        var quantities = new Dictionary<int, decimal>();
        foreach (var (index, day) in header.Days)
        {
            var quantity = ParseQuantity(Field(fields, index), row, $"Dia {day:00}", code, issues);
            if (quantity.HasValue) quantities.Add(day, quantity.Value);
        }
        var total = header.Total < 0 ? null : ParseQuantity(Field(fields, header.Total), row, "Total", code, issues);
        var average = header.Average < 0 ? null : ParseQuantity(Field(fields, header.Average), row, "Média", code, issues);
        return new(code, description, unit, row, quantities, total, average);
    }

    private static decimal? ParseQuantity(string value, long row, string column, string code, List<ConsumptionImportIssueDto> issues)
    {
        if (CsvReportFormat.TryDecimal(value, out var quantity)) return quantity;
        AddIssue(issues, "InvalidQuantity", $"Valor vazio ou inválido: '{value}'. Consumo indisponível; não convertido para zero.", row, column, code);
        return null;
    }

    private static Dictionary<string, List<ProductRow>> MergeProductRecords(List<ProductRow> rows,
        List<ConsumptionImportIssueDto> issues, CancellationToken cancellationToken)
    {
        var products = new Dictionary<string, List<ProductRow>>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!products.TryGetValue(row.Code, out var blocks)) products[row.Code] = blocks = [];
            if (blocks.Count > 0 && (blocks[0].Description != row.Description || blocks[0].Unit != row.Unit))
                AddIssue(issues, "ProductMetadataMismatch", "Descrição ou unidade divergente entre blocos do produto.", row.Row, item: row.Code);
            blocks.Add(row);
        }
        foreach (var (code, blocks) in products)
        {
            if (blocks.Where(b => b.Total.HasValue).Select(b => b.Total).Distinct().Skip(1).Any() ||
                blocks.Where(b => b.Average.HasValue).Select(b => b.Average).Distinct().Skip(1).Any())
                AddIssue(issues, "ConflictingReportTotals", "Valores consolidados divergentes entre linhas do mesmo produto; comparação indisponível.", item: code);
        }
        return products;
    }

    private static List<DailyConsumptionDto> NormalizeDailyConsumptions(Dictionary<string, List<ProductRow>> products,
        DateOnly month, List<ConsumptionImportIssueDto> issues, CancellationToken cancellationToken)
    {
        var daily = new List<DailyConsumptionDto>();
        foreach (var (code, blocks) in products.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var days = new Dictionary<int, decimal>();
            var conflicts = new HashSet<int>();
            foreach (var block in blocks)
            foreach (var (day, quantity) in block.Quantities)
            {
                if (!days.TryAdd(day, quantity))
                {
                    AddIssue(issues, "DuplicateDay", $"Dia {day:00} duplicado: {days[day]} / {quantity}. Não somado.", block.Row, $"Dia {day:00}", code);
                    if (days[day] != quantity) conflicts.Add(day);
                }
            }
            // Duplicatas idênticas são contadas uma vez; valores conflitantes ficam indisponíveis.
            foreach (var (day, quantity) in days.OrderBy(d => d.Key).Where(d => !conflicts.Contains(d.Key)))
                daily.Add(new() { SoulMvProductCode = code, Description = blocks[0].Description, Unit = blocks[0].Unit,
                    Date = new(month.Year, month.Month, day), Quantity = quantity });
        }
        return daily;
    }

    private static void ValidateConsumptionRecords(Dictionary<string, List<ProductRow>> products, List<DailyConsumptionDto> daily,
        HashSet<int> expectedDays, List<ConsumptionImportIssueDto> issues)
    {
        var grouped = daily.ToLookup(d => d.SoulMvProductCode!);
        foreach (var code in products.Keys)
        {
            var days = grouped[code].Select(d => d.Date.Day).ToHashSet();
            if (!days.SetEquals(expectedDays))
                AddIssue(issues, "IncompleteHistory", $"O produto deve apresentar todos os dias de 01 a {expectedDays.Max():00}, sem lacunas nem dias adicionais.", item: code);
        }
        foreach (var day in daily)
            if (day.Quantity % 0.001m != 0 || Math.Abs(day.Quantity) >= 1_000_000_000_000_000m)
                AddIssue(issues, "QuantityPrecision", "A quantidade não cabe na precisão de 18 dígitos e 3 casas decimais do banco; não será arredondada.",
                    column: $"Dia {day.Date.Day:00}", item: day.SoulMvProductCode);
    }

    private static List<MonthlyConsumptionDto> CalculateMonthlyConsumptions(Dictionary<string, List<ProductRow>> products,
        List<DailyConsumptionDto> daily, DateOnly month)
        => daily.GroupBy(d => d.SoulMvProductCode!).Select(group =>
        {
            var blocks = products[group.Key];
            var days = group.Count();
            var total = group.Sum(d => d.Quantity);
            return new MonthlyConsumptionDto
            {
                SoulMvProductCode = group.Key, Description = blocks[0].Description, Unit = blocks[0].Unit,
                Year = month.Year, Month = month.Month, TotalQuantity = total, RecordedDays = days,
                IsComplete = days == DateTime.DaysInMonth(month.Year, month.Month),
                // No mês em andamento, a média representa somente os dias disponíveis, sem zeros futuros.
                AverageDailyConsumption = CalculateDailyAverage(total, days),
                ReportedTotalQuantity = ConsistentValue(blocks.Select(b => b.Total)),
                ReportedAverageDailyConsumption = ConsistentValue(blocks.Select(b => b.Average))
            };
        }).ToList();

    private static decimal CalculateDailyAverage(decimal total, int days)
        => decimal.Round(total / days, 3, MidpointRounding.AwayFromZero);

    private static decimal? ConsistentValue(IEnumerable<decimal?> values)
    {
        var distinct = values.Where(v => v.HasValue).Distinct().ToArray();
        return distinct.Length == 1 ? distinct[0] : null;
    }

    private static void ValidateMonthlyTotals(IEnumerable<MonthlyConsumptionDto> monthly, List<ConsumptionImportIssueDto> issues)
    {
        foreach (var record in monthly)
        {
            if (Math.Abs(record.TotalQuantity) >= 1_000_000_000_000_000m)
                AddIssue(issues, "MonthlyQuantityPrecision", "O total mensal excede a precisão do banco.", item: record.SoulMvProductCode);
            if (record.ReportedTotalQuantity is { } total && record.TotalQuantity != total)
                AddIssue(issues, "MonthlyTotalMismatch", $"Total calculado {record.TotalQuantity.ToString(Br)}; Soul MV {total.ToString(Br)}. " +
                    "Os registros diários foram preservados; pequenas diferenças podem resultar de arredondamento.",
                    item: record.SoulMvProductCode, severity: AnalysisSeverity.Warning);
            if (record.ReportedAverageDailyConsumption is { } average && record.AverageDailyConsumption!.Value != decimal.Round(average, 3, MidpointRounding.AwayFromZero))
                AddIssue(issues, "MonthlyAverageMismatch", $"Média calculada sobre os dias disponíveis {record.AverageDailyConsumption.Value.ToString(Br)}; Soul MV {average.ToString(Br)}.",
                    item: record.SoulMvProductCode, severity: AnalysisSeverity.Warning);
        }
    }

    private async Task PersistAsync(ParsedImport parsed, CancellationToken cancellationToken,
        PreparedMonthlyImport? combined = null, IProgress<ConsumptionImportProgressDto>? progress = null)
    {
        var issues = (List<ConsumptionImportIssueDto>)parsed.Result.Issues;
        var period = parsed.Result.ConsumptionPeriod!;
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var lockMonth = period.StartDate.Year * 12 + period.StartDate.Month;
        await context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({1380997443}, {lockMonth})", cancellationToken);
        var codes = parsed.Monthly.Select(m => m.SoulMvProductCode!).ToArray();
        var items = await context.Items.AsNoTracking().Where(i => codes.Contains(i.Code)).ToDictionaryAsync(i => i.Code, cancellationToken);
        foreach (var product in parsed.Monthly)
        {
            if (!items.TryGetValue(product.SoulMvProductCode!, out var item))
                AddIssue(issues, "UnknownItem", "Produto não cadastrado. Cadastre ou confira o código Soul MV antes de importar.", item: product.SoulMvProductCode);
            else if (Label(item.Unit) != Label(product.Unit!))
                AddIssue(issues, "UnitMismatch", $"Unidade do cadastro '{item.Unit}' difere da unidade do relatório '{product.Unit}'. Não foi feita conversão automática.", item: product.SoulMvProductCode);
            else if (combined is not null && (!combined.Catalog.TryGetValue(item.Code, out var validated)
                || validated.Id != item.Id || validated.Unit != Label(item.Unit)))
                AddIssue(issues, "CatalogChanged", "O cadastro do produto mudou após a prévia. Processe novamente os relatórios.", item: item.Code);
        }
        if (HasErrors(issues)) { parsed.Result.Status = ConsumptionOperationStatus.Failed; return; }

        var ids = items.Values.Select(i => i.Id).ToArray();
        var dailyQuery = context.DailyConsumptions.Where(d => d.Date >= period.StartDate && d.Date <= period.EndDate);
        var monthlyQuery = context.MonthlyConsumptions.Where(m => m.Year == period.StartDate.Year && m.Month == period.StartDate.Month);
        var existingDaily = await (combined is null ? dailyQuery.Where(d => ids.Contains(d.ItemId)) : dailyQuery).ToListAsync(cancellationToken);
        var existingMonthly = await (combined is null ? monthlyQuery.Where(m => ids.Contains(m.ItemId)) : monthlyQuery).ToListAsync(cancellationToken);
        if (combined is not null) ValidatePreviousMonthlyCoverage(ids, existingDaily, existingMonthly, period, issues);
        if (HasErrors(issues)) { parsed.Result.Status = ConsumptionOperationStatus.Failed; return; }

        progress?.Report(new("Registrando consumo diário..."));
        await SaveDailyConsumptionsAsync(context, parsed.Daily, items, existingDaily, cancellationToken);
        progress?.Report(new("Consolidando informações mensais..."));
        // Reconsulta o diário efetivamente gravado dentro da mesma transação, antes de consolidar.
        var persisted = await context.DailyConsumptions.AsNoTracking().Where(d => ids.Contains(d.ItemId)
            && d.Date >= period.StartDate && d.Date <= period.EndDate).ToListAsync(cancellationToken);
        foreach (var monthly in parsed.Monthly)
        {
            var itemDays = persisted.Where(d => d.ItemId == items[monthly.SoulMvProductCode!].Id).ToArray();
            if (itemDays.Length != monthly.RecordedDays || itemDays.Sum(d => d.Quantity) != monthly.TotalQuantity)
                throw new InvalidOperationException("O diário persistido não corresponde ao conjunto validado.");
            monthly.TotalQuantity = itemDays.Sum(d => d.Quantity);
            monthly.AverageDailyConsumption = CalculateDailyAverage(monthly.TotalQuantity, itemDays.Length);
            monthly.RecordedDays = itemDays.Length;
            monthly.IsComplete = itemDays.Length == DateTime.DaysInMonth(monthly.Year, monthly.Month);
        }
        progress?.Report(new("Registrando consumo mensal..."));
        await SaveMonthlyConsumptionsAsync(context, parsed.Monthly, items, existingMonthly, combined?.Prices, cancellationToken);
        progress?.Report(new("Finalizando atualização..."));
        var importedAt = clock.GetUtcNow();
        context.ConsumptionImports.Add(new()
        {
            Id = parsed.Result.ImportId, ReferenceMonth = period.StartDate, RecordedThrough = period.EndDate,
            SourceFileName = parsed.Result.SourceFileName, ImportedAt = importedAt
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        parsed.Result.Status = ConsumptionOperationStatus.Completed;
        parsed.Result.CompletedAt = importedAt;
        logger.LogInformation("Consumo de {Month} gravado até {Through}. Importação {ImportId}. Diário {DailyFile}, setorial {SectorFile}.",
            period.StartDate.ToString("MM/yyyy", Br), period.EndDate.ToString("dd/MM/yyyy", Br), parsed.Result.ImportId,
            parsed.Result.SourceFileName, combined?.SectorFileName);
    }

    private static async Task SaveDailyConsumptionsAsync(AppDbContext context, List<DailyConsumptionDto> daily,
        Dictionary<string, Item> items, List<DailyConsumption> existing, CancellationToken token)
    {
        var indexed = existing.ToDictionary(d => (d.ItemId, d.Date));
        foreach (var day in daily)
        {
            token.ThrowIfCancellationRequested();
            var id = items[day.SoulMvProductCode!].Id;
            if (indexed.TryGetValue((id, day.Date), out var record)) record.Quantity = day.Quantity;
            else context.DailyConsumptions.Add(new() { ItemId = id, Date = day.Date, Quantity = day.Quantity });
        }
        await context.SaveChangesAsync(token);
    }

    private static async Task SaveMonthlyConsumptionsAsync(AppDbContext context, List<MonthlyConsumptionDto> monthly,
        Dictionary<string, Item> items, List<MonthlyConsumption> existing, IReadOnlyDictionary<string, decimal?>? prices, CancellationToken token)
    {
        var indexed = existing.ToDictionary(m => m.ItemId);
        foreach (var value in monthly)
        {
            token.ThrowIfCancellationRequested();
            var id = items[value.SoulMvProductCode!].Id;
            if (!indexed.TryGetValue(id, out var entity))
            {
                entity = new() { ItemId = id, Year = value.Year, Month = value.Month };
                context.MonthlyConsumptions.Add(entity);
            }
            entity.TotalQuantity = value.TotalQuantity;
            entity.AverageDailyConsumption = value.AverageDailyConsumption!.Value;
            entity.RecordedDays = value.RecordedDays;
            entity.IsComplete = value.IsComplete;
            // O fechamento conjunto substitui inclusive preços antigos por null quando não há custo confiável.
            // O fluxo parcial anterior continua preservando o preço já informado.
            if (prices is not null) entity.UnitPurchasePrice = prices[value.SoulMvProductCode!];
        }
        await context.SaveChangesAsync(token);
    }

    private static void ValidatePreviousMonthlyCoverage(int[] incoming, List<DailyConsumption> daily,
        List<MonthlyConsumption> monthly, DateRangeDto period, List<ConsumptionImportIssueDto> issues)
    {
        var previous = daily.Select(d => d.ItemId).Concat(monthly.Select(m => m.ItemId)).ToHashSet();
        if (previous.Count == 0) return;
        if (!previous.SetEquals(incoming))
            AddIssue(issues, "ReimportProductSetChanged", "O conjunto de produtos difere da competência já gravada. Nenhum registro será excluído ou combinado silenciosamente; regularize o conjunto antes de confirmar.");
        var expected = Enumerable.Range(1, DateTime.DaysInMonth(period.StartDate.Year, period.StartDate.Month)).ToHashSet();
        if (previous.Any(id => !daily.Where(d => d.ItemId == id).Select(d => d.Date.Day).ToHashSet().SetEquals(expected))
            || monthly.Any(m => !m.IsComplete || m.RecordedDays != expected.Count))
            AddIssue(issues, "ReimportCoverageChanged", "O histórico anterior tem cobertura diferente de um fechamento completo. É necessário regularizá-lo antes de substituir a competência.");
    }

    private static bool HasErrors(IEnumerable<ConsumptionImportIssueDto> issues) => issues.Any(i => i.Severity >= AnalysisSeverity.Error);
    private static string Field(string[] fields, int index) => index >= 0 && index < fields.Length ? fields[index] : string.Empty;
    private static string Label(string value) => CsvReportFormat.Label(value);
    private static void AddIssue(List<ConsumptionImportIssueDto> issues, string code, string message, long? row = null,
        string? column = null, string? item = null, AnalysisSeverity severity = AnalysisSeverity.Error)
        => issues.Add(new() { Code = code, Message = message, RowNumber = row, ColumnName = column, ItemCode = item, Severity = severity });
    private sealed record ParsedImport(ConsumptionImportResultDto Result, List<DailyConsumptionDto> Daily, List<MonthlyConsumptionDto> Monthly);
    private sealed record Header(int Code, int Description, int Unit, Dictionary<int, int> Days, int Total, int Average);
    private sealed record ProductRow(string Code, string Description, string Unit, long Row,
        Dictionary<int, decimal> Quantities, decimal? Total, decimal? Average);
}
