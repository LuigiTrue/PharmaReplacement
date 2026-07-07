using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RepyPharma.Data;
using RepyPharma.Domain.Entities;
using RepyPharma.Services.Import.Interfaces;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace RepyPharma.Services.Import;

public partial class ConsumptionAverageReportService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ILogger<ConsumptionAverageReportService> logger) : IConsumptionAverageReportService
{
    private const string MonthlyPeriodKind = "monthly";
    private const string WeeklyPeriodKind = "weekly";
    private const string CurrentPeriodKind = "current";

    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;
    private readonly ILogger<ConsumptionAverageReportService> _logger = logger;

    public async Task<ConsumptionAverageImportResult> ImportPdfAsync(string filePath)
    {
        var result = new ConsumptionAverageImportResult();

        if (!File.Exists(filePath))
        {
            result.AddError($"Arquivo de relatório não encontrado: {filePath}");
            return result;
        }

        ConsumptionAverageReportParseResult parsedReport;
        try
        {
            parsedReport = ParseReport(filePath);
        }
        catch (Exception ex)
        {
            result.AddError($"Falha ao ler relatório '{filePath}': {ex.Message}");
            _logger.LogError(ex, "Falha ao ler relatório de médias de saída {FilePath}", filePath);
            return result;
        }

        if (parsedReport.Items.Count == 0)
        {
            result.AddError("Nenhum item com coluna Média foi encontrado no relatório.");
            return result;
        }

        result.ParsedItems = parsedReport.Items.Count;
        result.ReportStartDate = parsedReport.ReportStartDate;
        result.ReportEndDate = parsedReport.ReportEndDate;
        result.ReportGeneratedAt = parsedReport.ReportGeneratedAt;
        result.CoverageDays = parsedReport.CoverageDays;
        result.AveragePeriodKind = parsedReport.AveragePeriodKind;

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var itemCodes = parsedReport.Items
                .Select(item => item.Code)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var itemsByCode = await context.Items
                .Where(item => itemCodes.Contains(item.Code))
                .ToDictionaryAsync(item => item.Code, StringComparer.OrdinalIgnoreCase);

            var existingRecords = await context.ItemConsumptionAverages
                .Where(average =>
                    itemCodes.Contains(average.ItemCode) &&
                    average.ReportStartDate == parsedReport.ReportStartDate &&
                    average.ReportEndDate == parsedReport.ReportEndDate)
                .ToDictionaryAsync(average => average.ItemCode, StringComparer.OrdinalIgnoreCase);

            var importedAt = DateTime.UtcNow;
            var sourceFileName = Path.GetFileName(filePath);

            foreach (var parsedItem in parsedReport.Items)
            {
                itemsByCode.TryGetValue(parsedItem.Code, out var item);
                if (item is null)
                    result.MissingItems++;

                var itemName = item?.Name ?? parsedItem.Name;
                var averageRecord = existingRecords.TryGetValue(parsedItem.Code, out var existingRecord)
                    ? existingRecord
                    : new ItemConsumptionAverage
                    {
                        ItemCode = parsedItem.Code,
                        ReportStartDate = parsedReport.ReportStartDate,
                        ReportEndDate = parsedReport.ReportEndDate
                    };

                averageRecord.ItemId = item?.Id;
                averageRecord.ItemName = itemName;
                averageRecord.ReportGeneratedAt = parsedReport.ReportGeneratedAt;
                averageRecord.CoverageDays = parsedReport.CoverageDays;
                averageRecord.AveragePeriodKind = parsedReport.AveragePeriodKind;
                averageRecord.MonthlyAverageOutput = parsedReport.AveragePeriodKind == MonthlyPeriodKind
                    ? parsedItem.AverageOutput
                    : null;
                averageRecord.WeeklyAverageOutput = parsedReport.AveragePeriodKind == WeeklyPeriodKind
                    ? parsedItem.AverageOutput
                    : null;
                averageRecord.CurrentAverageOutput = parsedReport.AveragePeriodKind == CurrentPeriodKind
                    ? parsedItem.AverageOutput
                    : null;
                averageRecord.TotalOutput = parsedItem.TotalOutput;
                averageRecord.StockBalance = parsedItem.StockBalance;
                averageRecord.ProjectedCoverageDays = parsedItem.ProjectedCoverageDays;
                averageRecord.SourceFileName = sourceFileName;
                averageRecord.ImportedAt = importedAt;

                if (averageRecord.Id == 0)
                {
                    await context.ItemConsumptionAverages.AddAsync(averageRecord);
                    result.CreatedRecords++;
                }
                else
                {
                    result.UpdatedRecords++;
                }
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.AddError($"Falha ao gravar médias de saída: {ex.Message}");
            _logger.LogError(ex, "Falha ao gravar médias de saída importadas do relatório {FilePath}", filePath);
        }

        return result;
    }

    private static ConsumptionAverageReportParseResult ParseReport(string filePath)
    {
        using var document = PdfDocument.Open(filePath);

        DateTime? reportStartDate = null;
        DateTime? reportEndDate = null;
        DateTime? reportGeneratedAt = null;
        var items = new List<ConsumptionAverageReportItem>();

        foreach (var page in document.GetPages())
        {
            var lines = ExtractLines(page);
            var readingAverageSection = false;

            foreach (var line in lines)
            {
                if (reportStartDate is null || reportEndDate is null)
                    TryReadReportPeriod(line, out reportStartDate, out reportEndDate);

                if (reportGeneratedAt is null)
                    reportGeneratedAt = TryReadReportGeneratedAt(line);

                if (IsAverageSectionHeader(line))
                {
                    readingAverageSection = true;
                    continue;
                }

                if (!readingAverageSection)
                    continue;

                var parsedItem = TryParseAverageLine(line);
                if (parsedItem is not null)
                    items.Add(parsedItem);
            }
        }

        if (reportStartDate is null || reportEndDate is null)
            throw new InvalidOperationException("Não foi possível identificar o período do relatório.");

        var coverageDays = (reportEndDate.Value.Date - reportStartDate.Value.Date).Days + 1;
        if (coverageDays <= 0)
            throw new InvalidOperationException("O período do relatório é inválido.");

        return new ConsumptionAverageReportParseResult
        {
            ReportStartDate = ToUtcDate(reportStartDate.Value),
            ReportEndDate = ToUtcDate(reportEndDate.Value),
            ReportGeneratedAt = reportGeneratedAt.HasValue
                ? DateTime.SpecifyKind(reportGeneratedAt.Value, DateTimeKind.Utc)
                : null,
            CoverageDays = coverageDays,
            AveragePeriodKind = GetAveragePeriodKind(coverageDays),
            Items = items
                .GroupBy(item => item.Code, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.Last())
                .ToList()
        };
    }

    private static List<string> ExtractLines(Page page)
    {
        return page.GetWords()
            .GroupBy(word => Math.Round(word.BoundingBox.Bottom, 0))
            .OrderByDescending(group => group.Key)
            .Select(group => string.Join(
                " ",
                group
                    .OrderBy(word => word.BoundingBox.Left)
                    .Select(word => word.Text)))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
    }

    private static bool IsAverageSectionHeader(string line)
    {
        return line.Contains("Produto", StringComparison.OrdinalIgnoreCase) &&
               line.Contains("Total", StringComparison.OrdinalIgnoreCase) &&
               line.Contains("Média", StringComparison.OrdinalIgnoreCase);
    }

    private static ConsumptionAverageReportItem? TryParseAverageLine(string line)
    {
        var parts = WhiteSpaceRegex().Split(line.Trim());
        if (parts.Length < 8 || !CodeRegex().IsMatch(parts[0]))
            return null;

        var values = parts[^6..];
        if (!values.All(IsDecimal))
            return null;

        var code = parts[0];
        var itemName = ExtractItemName(line, code, values[0]);

        return new ConsumptionAverageReportItem
        {
            Code = code,
            Name = itemName,
            TotalOutput = ParseDecimal(values[2]),
            AverageOutput = ParseDecimal(values[3]),
            StockBalance = ParseDecimal(values[4]),
            ProjectedCoverageDays = ParseDecimal(values[5])
        };
    }

    private static string ExtractItemName(string line, string code, string firstValue)
    {
        var codeIndex = line.IndexOf(code, StringComparison.Ordinal);
        var valueIndex = line.LastIndexOf(firstValue, StringComparison.Ordinal);

        if (codeIndex < 0 || valueIndex <= codeIndex)
            return string.Empty;

        return line[(codeIndex + code.Length)..valueIndex].Trim();
    }

    private static void TryReadReportPeriod(string line, out DateTime? startDate, out DateTime? endDate)
    {
        startDate = null;
        endDate = null;

        var match = ReportPeriodRegex().Match(line);
        if (!match.Success)
            return;

        startDate = ParseDate(match.Groups["start"].Value);
        endDate = ParseDate(match.Groups["end"].Value);
    }

    private static DateTime? TryReadReportGeneratedAt(string line)
    {
        var match = ReportGeneratedAtRegex().Match(line);
        if (!match.Success)
            return null;

        return DateTime.TryParseExact(
            match.Groups["generatedAt"].Value,
            "dd/MM/yyyy HH:mm",
            CultureInfo.GetCultureInfo("pt-BR"),
            DateTimeStyles.None,
            out var generatedAt)
            ? generatedAt
            : null;
    }

    private static DateTime ParseDate(string value)
    {
        return DateTime.ParseExact(
            value,
            "dd/MM/yyyy",
            CultureInfo.GetCultureInfo("pt-BR"),
            DateTimeStyles.None);
    }

    private static bool IsDecimal(string value)
    {
        return DecimalRegex().IsMatch(value);
    }

    private static decimal ParseDecimal(string value)
    {
        return decimal.Parse(
            value.Replace(',', '.'),
            NumberStyles.Number | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture);
    }

    private static DateTime ToUtcDate(DateTime date)
    {
        return DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
    }

    private static string GetAveragePeriodKind(int coverageDays)
    {
        if (coverageDays <= 8)
            return WeeklyPeriodKind;

        if (coverageDays >= 28)
            return MonthlyPeriodKind;

        return CurrentPeriodKind;
    }

    [GeneratedRegex(@"^\d{1,6}$")]
    private static partial Regex CodeRegex();

    [GeneratedRegex(@"^-?\d+(?:,\d+)?$")]
    private static partial Regex DecimalRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpaceRegex();

    [GeneratedRegex(@"Per[ií]odo\s+de\s+(?<start>\d{2}/\d{2}/\d{4})\s+at[eé]\s+(?<end>\d{2}/\d{2}/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex ReportPeriodRegex();

    [GeneratedRegex(@"\bEm:\s*(?<generatedAt>\d{2}/\d{2}/\d{4}\s+\d{2}:\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex ReportGeneratedAtRegex();

    private sealed class ConsumptionAverageReportParseResult
    {
        public DateTime ReportStartDate { get; init; }
        public DateTime ReportEndDate { get; init; }
        public DateTime? ReportGeneratedAt { get; init; }
        public int CoverageDays { get; init; }
        public string AveragePeriodKind { get; init; } = string.Empty;
        public List<ConsumptionAverageReportItem> Items { get; init; } = new();
    }

    private sealed class ConsumptionAverageReportItem
    {
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public decimal TotalOutput { get; init; }
        public decimal AverageOutput { get; init; }
        public decimal StockBalance { get; init; }
        public decimal ProjectedCoverageDays { get; init; }
    }
}
