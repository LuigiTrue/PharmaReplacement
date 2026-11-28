using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RepyPharma.Data;
using RepyPharma.Models.Analytics;
using RepyPharma.Services.Import;
using Xunit;

namespace RepyPharma.Consumption.Tests;

public class ConsumptionImportTests
{
    internal static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);
    private readonly ConsumptionImportService _service = new(new NoDatabaseFactory(), NullLogger<ConsumptionImportService>.Instance, new FixedClock(Now), CombinedTestServices.Sector(), CombinedTestServices.Options());

    internal static MemoryStream Csv(string text)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return new(Encoding.GetEncoding(1252).GetBytes(text));
    }
    internal static string Report(int days, string code = "138", string unit = "RESMA", decimal quantity = 0)
        => Block(Enumerable.Range(1, Math.Min(14, days)), code, unit, quantity) +
            (days > 14 ? Block(Enumerable.Range(15, Math.Min(14, days - 14)), code, unit, quantity, true) : "") +
            (days > 28 ? Block(Enumerable.Range(29, days - 28), code, unit, quantity) : "");
    internal static string Block(IEnumerable<int> days, string code, string unit, decimal quantity, bool gaps = false)
    {
        var header = new List<string> { "Produto", "", "Unidade" };
        var row = new List<string> { code, "\"PAPEL, A4 \"\"BRANCO\"\"\"", unit };
        foreach (var day in days)
        {
            if (gaps) { header.Add(""); row.Add(""); }
            header.Add(day.ToString("00"));
            row.Add('"' + quantity.ToString(CultureInfo.GetCultureInfo("pt-BR")) + '"');
        }
        return string.Join(',', header) + "\n" + string.Join(',', row) + "\n";
    }
    private async Task<ConsumptionImportValidationDto> Validate(string report, DateOnly month)
    {
        using var stream = Csv(report);
        var result = await _service.ValidateImportAsync(stream, "consumo.csv", new() { ReferenceMonth = month });
        Assert.True(stream.CanRead);
        return result;
    }

    [Theory]
    [InlineData(2025, 2, 28)]
    [InlineData(2024, 2, 29)]
    [InlineData(2026, 4, 30)]
    [InlineData(2026, 8, 31)]
    public async Task PastMonthsRequireTheirExactCalendarDays(int year, int month, int days)
    {
        Assert.True((await Validate(Report(days, quantity: -1.125m), new(year, month, 1))).IsValid);
        var incomplete = await Validate(Report(days - 1), new(year, month, 1));
        Assert.False(incomplete.IsValid);
        Assert.Contains(incomplete.Issues, i => i.Code == "IncompleteHistory");
    }

    [Theory]
    [InlineData(15, true)]
    [InlineData(14, false)]
    [InlineData(16, false)]
    [InlineData(30, false)]
    public async Task CurrentMonthRequiresExactlyFirstThroughToday(int days, bool valid)
    {
        var result = await Validate(Report(days), new(2026, 11, 1));
        Assert.Equal(valid, result.IsValid);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(27)]
    public async Task PastMonthNeverAcceptsLessThan28Days(int days)
        => Assert.False((await Validate(Report(days), new(2026, 8, 1))).IsValid);

    [Fact]
    public async Task EveryProductMustCoverAllDaysEvenWhenHeadersCoverTheMonth()
    {
        var csv = Report(31) + Block([1, 2], "139", "RESMA", 0);
        var result = await Validate(csv, new(2026, 8, 1));
        Assert.Contains(result.Issues, i => i.Code == "IncompleteHistory" && i.ItemCode == "139");
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task MissingMiddleDayCannotBeReplacedWithAnotherDay()
    {
        var csv = Block(Enumerable.Range(1, 15).Where(d => d != 8).Append(16), "138", "RESMA", 0);
        Assert.False((await Validate(csv, new(2026, 11, 1))).IsValid);
    }

    [Fact]
    public async Task BlocksCanArriveInAnyOrder()
    {
        var csv = Block(Enumerable.Range(29, 3), "138", "RESMA", -1.25m) +
            Block(Enumerable.Range(15, 14), "138", "RESMA", 0, true) + Block(Enumerable.Range(1, 14), "138", "RESMA", 2.5m);
        Assert.True((await Validate(csv, new(2026, 8, 1))).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("inválido")]
    [InlineData("1.2")]
    public async Task InvalidAmountsRejectTheWholeReport(string amount)
    {
        var csv = Report(15).Replace("\"0\"", '"' + amount + '"');
        var result = await Validate(csv, new(2026, 11, 1));
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == "InvalidQuantity");
    }

    [Fact]
    public async Task DuplicateProductDaysRejectTheReport()
    {
        var result = await Validate(Report(15) + Block([1], "138", "RESMA", 0), new(2026, 11, 1));
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == "DuplicateDay");
    }

    [Fact]
    public async Task FutureMonthIsRejectedBeforeReading()
    {
        using var stream = Csv(Report(31));
        var result = await _service.ValidateImportAsync(stream, "consumo.csv", new() { ReferenceMonth = new(2026, 12, 1) });
        Assert.False(result.IsValid);
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public async Task IncompleteImportNeverOpensDatabase()
    {
        using var stream = Csv(Report(27));
        var result = await _service.ImportDailyConsumptionAsync(stream, "consumo.csv", new() { ReferenceMonth = new(2026, 8, 1) });
        Assert.Equal(ConsumptionOperationStatus.Failed, result.Status);
    }

    [Fact]
    public async Task CalendarUsesBrasiliaAtUtcMidnight()
    {
        var clock = new FixedClock(new(2026, 11, 16, 1, 0, 0, TimeSpan.Zero));
        var service = new ConsumptionImportService(new NoDatabaseFactory(), NullLogger<ConsumptionImportService>.Instance, clock, CombinedTestServices.Sector(), CombinedTestServices.Options());
        using var stream = Csv(Report(15));
        Assert.True((await service.ValidateImportAsync(stream, "consumo.csv", new() { ReferenceMonth = new(2026, 11, 1) })).IsValid);
    }

    [Theory]
    [InlineData("Produto,,Unidade,01\n138,\"unclosed,RESMA,0\n", "ReadFailure")]
    [InlineData("", "NoHeaders")]
    [InlineData("Produto,,Unidade,32\n138,PAPEL,RESMA,0\n", "InvalidHeaderDay")]
    public async Task InvalidStructuresAreRejected(string csv, string code)
    {
        var result = await Validate(csv, new(2026, 8, 1));
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == code);
    }

    [Theory]
    [InlineData("1,00049", false)]
    [InlineData("1,0005", true)]
    public async Task ReportedAverageComparisonUsesExactlyThreeDecimalPlaces(string average, bool mismatch)
    {
        var lines=Block(Enumerable.Range(1,31),"138","RESMA",1).TrimEnd().Split('\n');
        var report=lines[0]+",Média\n"+lines[1]+",\""+average+"\"\n";
        var validation=await Validate(report,new(2026,8,1));
        Assert.True(validation.IsValid);
        Assert.Equal(mismatch,validation.Issues.Any(i=>i.Code=="MonthlyAverageMismatch"));
    }

    [Fact]
    public async Task QuantitiesThatWouldBeRoundedByDatabaseAreRejected()
    {
        var result = await Validate(Report(15, quantity: 0.0001m), new(2026, 11, 1));
        Assert.Contains(result.Issues, i => i.Code == "QuantityPrecision");
        Assert.False(result.IsValid);
    }
}

internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
internal sealed class NoDatabaseFactory : IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext() => throw new Exception("A validação do CSV não deve abrir o banco.");
}
