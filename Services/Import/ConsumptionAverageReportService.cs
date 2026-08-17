using System.Globalization;
using System.Text;
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
    private const double ColumnAlignmentTolerance = 8.0;

    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;
    private readonly ILogger<ConsumptionAverageReportService> _logger = logger;

    public PdfImportValidationResult ValidateConsumptionReportPdf(string filePath)
    {
        if (!File.Exists(filePath))
            return PdfImportValidationResult.Invalid("Arquivo de relatório não encontrado.");

        try
        {
            var parsedReport = ParseReport(filePath);
            if (parsedReport.Items.Count == 0)
                return PdfImportValidationResult.Invalid("O arquivo não é um relatório de consumo válido.");

            return PdfImportValidationResult.Valid("Relatório de consumo válido.");
        }
        catch
        {
            return PdfImportValidationResult.Invalid("O arquivo não é um relatório de consumo válido.");
        }
    }

    public async Task<ConsumptionAverageImportResult> ImportPdfAsync(string filePath)
    {
        var result = new ConsumptionAverageImportResult();

        var validation = ValidateConsumptionReportPdf(filePath);
        if (!validation.IsValid)
        {
            result.AddError(validation.Message);
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
            AverageColumnPositions? averageColumns = null;

            foreach (var line in lines)
            {
                if (reportStartDate is null || reportEndDate is null)
                    TryReadReportPeriod(line.Text, out reportStartDate, out reportEndDate);

                if (reportGeneratedAt is null)
                    reportGeneratedAt = TryReadReportGeneratedAt(line.Text);

                if (TryIdentifyAverageColumns(line.Words, out var identifiedColumns))
                {
                    averageColumns = identifiedColumns;
                    continue;
                }

                if (averageColumns is null)
                    continue;

                var parsedItem = TryParseAverageLine(line.Words, averageColumns);
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

    private static List<PdfLine> ExtractLines(Page page)
    {
        return page.GetWords()
            .GroupBy(word => Math.Round(word.BoundingBox.Bottom, 0))
            .OrderByDescending(group => group.Key)
            .Select(group =>
            {
                var words = group
                    .OrderBy(word => word.BoundingBox.Left)
                    .ToList();

                return new PdfLine
                {
                    Words = words,
                    Text = string.Join(" ", words.Select(word => word.Text))
                };
            })
            .Where(line => !string.IsNullOrWhiteSpace(line.Text))
            .ToList();
    }

    private static bool TryIdentifyAverageColumns(
        IReadOnlyList<Word> words,
        out AverageColumnPositions positions)
    {
        positions = null!;

        var productHeader = FindHeaderWord(words, "PRODUTO");
        var totalHeader = FindHeaderWord(words, "TOTAL");
        var averageHeader = FindHeaderWord(words, "MEDIA");
        var balanceHeader = FindHeaderWord(words, "SALDO");
        var projectionHeader = words.FirstOrDefault(word =>
            NormalizeHeader(word.Text).StartsWith("PROJEC", StringComparison.Ordinal));

        if (productHeader is null ||
            totalHeader is null ||
            averageHeader is null ||
            balanceHeader is null ||
            projectionHeader is null)
        {
            return false;
        }

        var totalRight = totalHeader.BoundingBox.Right;
        var firstDailyColumnRight = words
            .Where(word =>
                word.BoundingBox.Right < totalRight &&
                CodeRegex().IsMatch(word.Text))
            .Select(word => word.BoundingBox.Right)
            .DefaultIfEmpty(totalRight)
            .Min();

        positions = new AverageColumnPositions
        {
            FirstDailyColumnRight = firstDailyColumnRight,
            TotalOutputRight = totalRight,
            AverageOutputRight = averageHeader.BoundingBox.Right,
            StockBalanceRight = balanceHeader.BoundingBox.Right,
            ProjectedCoverageRight = projectionHeader.BoundingBox.Right
        };

        return true;
    }

    private static Word? FindHeaderWord(IReadOnlyList<Word> words, string header)
    {
        return words.FirstOrDefault(word =>
            string.Equals(
                NormalizeHeader(word.Text),
                header,
                StringComparison.Ordinal));
    }

    private static string NormalizeHeader(string value)
    {
        return string.Concat(
            value
                .Normalize(NormalizationForm.FormD)
                .Where(character =>
                    CharUnicodeInfo.GetUnicodeCategory(character) !=
                    UnicodeCategory.NonSpacingMark &&
                    char.IsLetter(character)))
            .ToUpperInvariant();
    }

    private static ConsumptionAverageReportItem? TryParseAverageLine(
        IReadOnlyList<Word> words,
        AverageColumnPositions positions)
    {
        var codeWord = words
            .Where(word =>
                word.BoundingBox.Right < positions.FirstDailyColumnRight &&
                CodeRegex().IsMatch(word.Text))
            .OrderBy(word => word.BoundingBox.Left)
            .FirstOrDefault();

        if (codeWord is null ||
            !TryExtractDecimalAtColumn(words, positions.TotalOutputRight, out var totalOutput) ||
            !TryExtractDecimalAtColumn(words, positions.AverageOutputRight, out var averageOutput) ||
            !TryExtractDecimalAtColumn(words, positions.StockBalanceRight, out var stockBalance) ||
            !TryExtractDecimalAtColumn(words, positions.ProjectedCoverageRight, out var projectedCoverage))
        {
            return null;
        }

        var itemName = string.Join(
            " ",
            words
                .Where(word =>
                    word.BoundingBox.Left > codeWord.BoundingBox.Left &&
                    word.BoundingBox.Right <
                    positions.FirstDailyColumnRight - ColumnAlignmentTolerance)
                .OrderBy(word => word.BoundingBox.Left)
                .Select(word => word.Text));

        return new ConsumptionAverageReportItem
        {
            Code = codeWord.Text,
            Name = itemName,
            TotalOutput = totalOutput,
            AverageOutput = averageOutput,
            StockBalance = stockBalance,
            ProjectedCoverageDays = projectedCoverage
        };
    }

    private static bool TryExtractDecimalAtColumn(
        IReadOnlyList<Word> words,
        double columnRight,
        out decimal value)
    {
        value = 0;

        var valueWord = words
            .Where(word =>
                IsDecimal(word.Text) &&
                Math.Abs(word.BoundingBox.Right - columnRight) <=
                ColumnAlignmentTolerance)
            .OrderBy(word => Math.Abs(word.BoundingBox.Right - columnRight))
            .FirstOrDefault();

        if (valueWord is null)
            return false;

        value = ParseDecimal(valueWord.Text);
        return true;
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

    private sealed class PdfLine
    {
        public IReadOnlyList<Word> Words { get; init; } = Array.Empty<Word>();
        public string Text { get; init; } = string.Empty;
    }

    private sealed class AverageColumnPositions
    {
        public double FirstDailyColumnRight { get; init; }
        public double TotalOutputRight { get; init; }
        public double AverageOutputRight { get; init; }
        public double StockBalanceRight { get; init; }
        public double ProjectedCoverageRight { get; init; }
    }
}
