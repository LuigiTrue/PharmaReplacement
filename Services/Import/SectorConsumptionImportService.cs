using System.Diagnostics;
using System.Text.RegularExpressions;
using CsvHelper;
using RepyPharma.Models.Analytics;
using RepyPharma.Services.Import.Interfaces;

namespace RepyPharma.Services.Import;

/// <summary>Obtém o preço mensal do CSV setorial; pondera somente preços positivos distintos, sem acessar o banco.</summary>
public sealed class SectorConsumptionImportService(ILogger<SectorConsumptionImportService> logger) : ISectorConsumptionImportService
{
    public async Task<SectorConsumptionReportDto> ProcessSectorConsumptionCsvAsync(Stream csvStream, string fileName,
        IProgress<ConsumptionImportProgressDto>? progress = null, CancellationToken cancellationToken = default)
    {
        var watch = Stopwatch.StartNew();
        var issues = new List<ConsumptionImportIssueDto>();
        var records = new List<SectorConsumptionRecordDto>();
        progress?.Report(new("Lendo valores unitários do relatório de saídas por setor..."));
        logger.LogInformation("Iniciando leitura de preços: {File}.", fileName);
        try
        {
            if (!csvStream.CanRead || !string.Equals(Path.GetExtension(fileName), ".csv", StringComparison.OrdinalIgnoreCase))
                Issue(issues, "InvalidSectorFile", "Selecione um CSV legível com os valores unitários.");
            else await ReadCsvAsync(csvStream, records, issues, cancellationToken);
            if (records.Count == 0) Issue(issues, "NoSectorRecords", "Nenhum produto com coluna de preço unitário foi encontrado.");
            var products = ConsolidateMonthlyProductValues(records, issues);
            logger.LogInformation("Preços de {File}: {Records} linhas, {Products} produtos, {Errors} exceções de processamento, duração {Elapsed}.",
                fileName, records.Count, products.Count, issues.Count(i => i.Severity >= AnalysisSeverity.Error), watch.Elapsed);
            return new()
            {
                SourceFileName = Path.GetFileName(fileName), Records = records.ToArray(), Products = products,
                SectorCount = records.Where(r => r.SectorCode.Length > 0).Select(r => r.SectorCode).Distinct().Count(),
                Issues = issues.ToArray()
            };
        }
        catch (Exception ex) when (ex is CsvFormatException or IOException or OverflowException or System.Text.DecoderFallbackException)
        {
            logger.LogWarning(ex, "Falha ao interpretar preços de {File}.", fileName);
            Issue(issues, "SectorReadFailure", "Não foi possível interpretar o CSV de preços. Confira a estrutura e as aspas.");
            return new() { SourceFileName = Path.GetFileName(fileName), Issues = issues.ToArray() };
        }
    }

    private static async Task ReadCsvAsync(Stream stream, List<SectorConsumptionRecordDto> records,
        List<ConsumptionImportIssueDto> issues, CancellationToken token)
    {
        using var csv = CsvReportFormat.CreateReader(stream);
        Columns? header = null;
        var state = new Context();
        while (await csv.ReadAsync(token))
        {
            token.ThrowIfCancellationRequested();
            var fields = Enumerable.Range(0, csv.FieldCount).Select(csv.GetString).ToArray();
            if (fields.All(string.IsNullOrWhiteSpace)) continue;
            var labels = fields.Select(CsvReportFormat.Header).ToArray();
            if (header is not null)
            {
                var product = Regex.Match(CsvReportFormat.Field(fields, header.Product), @"^(\d+)(?:\s*-\s*(.*))?$");
                if (product.Success)
                {
                    records.Add(ParseProductRow(fields, header, state, product, csv.LineNumber));
                    continue;
                }
            }
            if (labels.FirstOrDefault(l => l.Length > 0)?.StartsWith("OBS", StringComparison.Ordinal) == true) continue;
            if (ParseContextHeader(fields, "SETOR") is { } sector)
            { state.SectorCode = sector.Code; state.SectorName = sector.Name; state.SpeciesCode = ""; state.SpeciesDescription = ""; continue; }
            if (ParseContextHeader(fields, "ESPECIE") is { } species)
            { state.SpeciesCode = species.Code; state.SpeciesDescription = species.Name; continue; }
            if (labels.Contains("PRODUTO"))
            {
                header = ParseColumns(labels, issues, csv.LineNumber);
                continue;
            }
            // Datas, unidades e totais de encerramento não participam da obtenção do custo.
            if (labels.Any(l => l.StartsWith("TOTAL", StringComparison.Ordinal) || l.StartsWith("PERIODO", StringComparison.Ordinal))) continue;
            if (header is null) continue;
            Issue(issues, "InvalidSectorProduct", "Linha de produto sem código Soul MV válido.", csv.LineNumber);
        }
    }

    private static Columns? ParseColumns(string[] labels, List<ConsumptionImportIssueDto> issues, long row)
    {
        var product = Array.IndexOf(labels, "PRODUTO");
        var price = Array.FindIndex(labels, l => l is "VLUNITARIO" or "VLRUNITARIO" or "VALORUNITARIO");
        var description = Array.FindIndex(labels, l => l is "DESCRICAO" or "DESCRICAODOPRODUTO");
        if (product < 0 || price < 0)
        { Issue(issues, "InvalidSectorColumns", "O cabeçalho deve conter Produto e Vl Unitário.", row); return null; }
        var quantity = Array.FindIndex(labels, l => l is "QTDMOVIMENT" or "QTDMOVIMENTADA" or "QUANTIDADE" or "QUANTIDADEMOVIMENTADA");
        var total = Array.FindIndex(labels, l => l is "VALORTOTAL" or "VLTOTAL" or "VLRTOTAL");
        return new(product, price, description, quantity, total);
    }

    private static SectorConsumptionRecordDto ParseProductRow(string[] fields, Columns header, Context state, Match product, long row)
    {
        var code = product.Groups[1].Value;
        var price = ReadDecimal(fields, header.Price);
        return new()
        {
            RowNumber = row, ProductCode = code,
            Description = product.Groups[2].Success ? product.Groups[2].Value.Trim() : CsvReportFormat.Field(fields, header.Description),
            SectorCode = state.SectorCode, SectorName = state.SectorName,
            SpeciesCode = state.SpeciesCode, SpeciesDescription = state.SpeciesDescription, UnitValue = price,
            MovementQuantity = ReadDecimal(fields, header.Quantity), MovementTotalValue = ReadDecimal(fields, header.Total)
        };
    }

    private IReadOnlyList<MonthlyProductFinancialDto> ConsolidateMonthlyProductValues(
        List<SectorConsumptionRecordDto> records, List<ConsumptionImportIssueDto> issues)
    {
        var products = new List<MonthlyProductFinancialDto>();
        foreach (var group in records.GroupBy(r => r.ProductCode))
        {
            decimal? price = null;
            try
            {
                price = CalculateUnitProductPrice(group.Key, group.ToArray());
            }
            catch (UnitPriceProcessingException ex)
            {
                logger.LogError(ex, "Falha no custo mensal do produto {ProductCode}: {Code}.", ex.ProductCode, ex.Code);
                Issue(issues, ex.Code, ex.Message, group.First().RowNumber, group.Key);
            }
            products.Add(new()
            {
                ProductCode = group.Key, Description = group.First().Description, UnitPurchasePrice = price,
                SectorCount = group.Where(r => r.SectorCode.Length > 0).Select(r => r.SectorCode).Distinct().Count()
            });
        }
        return products;
    }

    private static decimal CalculateUnitProductPrice(string code, IReadOnlyList<SectorConsumptionRecordDto> records)
    {
        if (records.Any(r => !r.UnitValue.HasValue || r.UnitValue < 0))
            throw new UnitPriceProcessingException("InvalidUnitPrice", code, "O produto possui valor unitário vazio, inválido ou negativo.");

        // Linhas de preço zero não fornecem custo nem integram os somatórios ponderados.
        var pricedRows = records.Where(r => r.UnitValue > 0).ToArray();
        var prices = pricedRows.Select(r => r.UnitValue!.Value).Distinct().ToArray();
        if (prices.Length == 0)
            throw new UnitPriceProcessingException("NoValidUnitPrice", code, "Nenhum valor unitário positivo disponível para o produto; valores zerados foram desconsiderados.");
        if (prices.Length == 1)
        {
            var price = prices[0];
            if (price >= 1_000_000_000_000m || price % 0.000001m != 0)
                throw new UnitPriceProcessingException("UnitPricePrecision", code, "O preço informado não pode ser preservado na precisão numeric(18,6) existente.");
            return price;
        }

        if (pricedRows.Any(r => !r.MovementQuantity.HasValue || !r.MovementTotalValue.HasValue))
            throw new UnitPriceProcessingException("InvalidWeightedPriceData", code,
                "Há preços positivos distintos, mas Qtd Moviment. ou Valor Total está ausente/inválido em alguma linha de preço positivo.");
        try
        {
            // Manter os sinais das movimentações e todos os dígitos até a divisão final.
            var quantity = pricedRows.Sum(r => r.MovementQuantity!.Value);
            var total = pricedRows.Sum(r => r.MovementTotalValue!.Value);
            if (quantity == 0)
                throw new UnitPriceProcessingException("ZeroWeightedQuantity", code,
                    "Não é possível calcular o custo ponderado: o somatório de Qtd Moviment. das linhas de preço positivo é zero.");
            var calculated = total / quantity;
            if (calculated <= 0 || calculated >= 1_000_000_000_000m)
                throw new UnitPriceProcessingException("InvalidWeightedPrice", code,
                    "O quociente Valor Total / Qtd Moviment. não produz um custo positivo compatível com a precisão do banco.");
            // A coluna existente armazena seis casas; não arredondar os termos ou preços de origem.
            var price = decimal.Round(calculated, 6, MidpointRounding.AwayFromZero);
            if (price <= 0 || price >= 1_000_000_000_000m)
                throw new UnitPriceProcessingException("UnitPricePrecision", code,
                    "O custo ponderado não pode ser representado como valor positivo em numeric(18,6).");
            return price;
        }
        catch (OverflowException ex)
        {
            throw new UnitPriceProcessingException("UnitPriceArithmeticFailure", code,
                "Os valores do produto excedem a precisão decimal durante o cálculo ponderado.", ex);
        }
    }

    private static decimal? ReadDecimal(string[] fields, int index)
        => CsvReportFormat.TryDecimal(CsvReportFormat.Field(fields, index), out var value) ? value : null;

    private static (string Code, string Name)? ParseContextHeader(string[] fields, string expected)
    {
        var index = Array.FindIndex(fields, f => !string.IsNullOrWhiteSpace(f));
        if (index < 0 || !CsvReportFormat.Header(fields[index]).StartsWith(expected, StringComparison.Ordinal)) return null;
        var inline = Regex.Match(CsvReportFormat.Label(fields[index]), @"^(?:SETOR|ESPECIE)\s*:\s*(\d+)\s*[-:]?\s*(.*)$");
        if (inline.Success) return (inline.Groups[1].Value, inline.Groups[2].Value.Trim());
        var values = fields.Skip(index + 1).Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => f.Trim()).ToArray();
        return values.Length >= 2 ? (values[0], values[1]) : ("", "");
    }
    private static void Issue(List<ConsumptionImportIssueDto> issues, string code, string message, long? row = null, string? product = null)
        => issues.Add(new() { Code = code, Message = message, RowNumber = row, ItemCode = product, Severity = AnalysisSeverity.Error });
    private sealed record Columns(int Product, int Price, int Description, int Quantity, int Total);
    private sealed class Context
    {
        internal string SectorCode = "", SectorName = "", SpeciesCode = "", SpeciesDescription = "";
    }
}
