using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using RepyPharma.Data;
using RepyPharma.Domain.Entities;
using RepyPharma.Models.Analytics;
using RepyPharma.Services.Import;
using Xunit;
using static RepyPharma.Consumption.Tests.ConsumptionImportTests;

namespace RepyPharma.Consumption.Tests;

public class ConsumptionPersistenceTests : IClassFixture<ConsumptionDatabase>
{
    private readonly ConsumptionDatabase _database;
    public ConsumptionPersistenceTests(ConsumptionDatabase database) => _database = database;
    private ConsumptionImportService Service(TimeProvider? clock = null) => new(_database,
        NullLogger<ConsumptionImportService>.Instance, clock ?? new FixedClock(Now), CombinedTestServices.Sector(), CombinedTestServices.Options());
    private async Task<ConsumptionImportResultDto> Import(string text, DateOnly? month = null, TimeProvider? clock = null)
    {
        using var stream = Csv(text);
        return await Service(clock).ImportDailyConsumptionAsync(stream, "consumo.csv", new() { ReferenceMonth = month ?? new(2026, 11, 1) });
    }
    private async Task<int> Seed(string code = "138", string unit = "RESMA")
    {
        await using var context = _database.CreateDbContext();
        var item = new Item { Code = code, Name = "Produto de teste", Unit = unit, ItemType = ItemType.Material, CreatedAt = DateTime.UtcNow };
        context.Items.Add(item);
        await context.SaveChangesAsync();
        return item.Id;
    }

    [PostgresFact]
    public async Task NegativeDailyAndMonthlyAmountsArePersistedWithNullPrice()
    {
        await _database.ClearAsync();
        var id = await Seed();
        var result = await Import(Report(15, quantity: -1.125m));
        Assert.Equal(ConsumptionOperationStatus.Completed, result.Status);
        await using var context = _database.CreateDbContext();
        var daily = await context.DailyConsumptions.ToListAsync();
        Assert.Equal(15, daily.Count);
        Assert.All(daily, d => { Assert.Equal(id, d.ItemId); Assert.Equal(-1.125m, d.Quantity); });
        var monthly = await context.MonthlyConsumptions.SingleAsync();
        Assert.Equal(-16.875m, monthly.TotalQuantity);
        Assert.Equal(-1.125m, monthly.AverageDailyConsumption);
        Assert.False(monthly.IsComplete);
        Assert.Equal(15, monthly.RecordedDays);
        Assert.Null(monthly.UnitPurchasePrice);
        Assert.NotNull(await Service().GetImportResultAsync(result.ImportId));
    }

    [PostgresFact]
    public async Task CurrentMonthUsesOnlyAvailableDaysAndPreservesZero()
    {
        await _database.ClearAsync();
        await Seed();
        Assert.Equal(ConsumptionOperationStatus.Completed, (await Import(Report(15))).Status);
        await using var context = _database.CreateDbContext();
        Assert.Equal(15, await context.DailyConsumptions.CountAsync());
        Assert.All(await context.DailyConsumptions.ToArrayAsync(), d => Assert.Equal(0, d.Quantity));
        var monthly = await context.MonthlyConsumptions.SingleAsync();
        Assert.False(monthly.IsComplete);
        Assert.Equal(15, monthly.RecordedDays);
        Assert.Equal(0, monthly.AverageDailyConsumption);
        Assert.Equal(new DateOnly(2026, 11, 15), (await context.ConsumptionImports.SingleAsync()).RecordedThrough);
    }

    [PostgresFact]
    public async Task ReimportReplacesValuesPreservesPriceAndDoesNotDuplicate()
    {
        await _database.ClearAsync();
        await Seed();
        Assert.Equal(ConsumptionOperationStatus.Completed, (await Import(Report(15, quantity: 2))).Status);
        await using (var context = _database.CreateDbContext())
        {
            (await context.MonthlyConsumptions.SingleAsync()).UnitPurchasePrice = 3.5m;
            await context.SaveChangesAsync();
        }
        Assert.Equal(ConsumptionOperationStatus.Completed, (await Import(Report(15, quantity: -1))).Status);
        await using var check = _database.CreateDbContext();
        Assert.Equal(15, await check.DailyConsumptions.CountAsync());
        Assert.Equal(-15, (await check.MonthlyConsumptions.SingleAsync()).TotalQuantity);
        Assert.Equal(3.5m, (await check.MonthlyConsumptions.SingleAsync()).UnitPurchasePrice);
        Assert.Equal(2, (await Service().GetImportsAsync()).Count);
    }

    [PostgresFact]
    public async Task NextDayUpdateAddsDayWithoutAddingOldAmountsAgain()
    {
        await _database.ClearAsync();
        await Seed();
        await Import(Report(15, quantity: 2));
        var clock = new FixedClock(Now.AddDays(1));
        Assert.Equal(ConsumptionOperationStatus.Completed, (await Import(Report(16, quantity: 1), clock: clock)).Status);
        await using var check = _database.CreateDbContext();
        Assert.Equal(16, await check.DailyConsumptions.CountAsync());
        Assert.Equal(16, (await check.MonthlyConsumptions.SingleAsync()).TotalQuantity);
    }

    [PostgresFact]
    public async Task UnknownProductRejectsEntireImportWithoutWritingKnownProduct()
    {
        await _database.ClearAsync();
        await Seed();
        var result = await Import(Report(15) + Report(15, code: "999"));
        Assert.Equal(ConsumptionOperationStatus.Failed, result.Status);
        Assert.Contains(result.Issues, i => i.Code == "UnknownItem" && i.ItemCode == "999");
        await using var context = _database.CreateDbContext();
        Assert.Empty(await context.DailyConsumptions.ToArrayAsync());
        Assert.Empty(await context.MonthlyConsumptions.ToArrayAsync());
        Assert.Empty(await context.ConsumptionImports.ToArrayAsync());
    }

    [PostgresFact]
    public async Task UnitMismatchAndIncompleteUpdatePreservePreviousData()
    {
        await _database.ClearAsync();
        await Seed();
        await Import(Report(15, quantity: 2));
        var wrongUnit = await Import(Report(15, unit: "CAIXA", quantity: 9));
        Assert.Equal(ConsumptionOperationStatus.Failed, wrongUnit.Status);
        Assert.Contains(wrongUnit.Issues, i => i.Code == "UnitMismatch");
        Assert.Equal(ConsumptionOperationStatus.Failed, (await Import(Report(14, quantity: 9))).Status);
        await using var context = _database.CreateDbContext();
        Assert.All(await context.DailyConsumptions.ToListAsync(), d => Assert.Equal(2, d.Quantity));
        Assert.Single(await context.ConsumptionImports.ToListAsync());
    }

    [PostgresFact]
    public async Task ConcurrentUpdatesRemainConsistent()
    {
        await _database.ClearAsync();
        await Seed();
        var results = await Task.WhenAll(Import(Report(15, quantity: 1)), Import(Report(15, quantity: -2)));
        Assert.All(results, r => Assert.Equal(ConsumptionOperationStatus.Completed, r.Status));
        await using var context = _database.CreateDbContext();
        var daily = await context.DailyConsumptions.ToArrayAsync();
        Assert.Equal(15, daily.Length);
        Assert.Single(daily.Select(d => d.Quantity).Distinct());
        Assert.Equal(daily.Sum(d => d.Quantity), (await context.MonthlyConsumptions.SingleAsync()).TotalQuantity);
    }

    [PostgresFact]
    public async Task FailureDuringSaveRollsBackAllRowsAndHistory()
    {
        await _database.ClearAsync();
        await Seed();
        await using (var context = _database.CreateDbContext())
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE consumption_imports ADD CONSTRAINT fail_import CHECK (false) NOT VALID");
        try
        {
            Assert.Equal(ConsumptionOperationStatus.Failed, (await Import(Report(15, quantity: -1))).Status);
            await using var context = _database.CreateDbContext();
            Assert.Empty(await context.DailyConsumptions.ToArrayAsync());
            Assert.Empty(await context.MonthlyConsumptions.ToArrayAsync());
        }
        finally
        {
            await using var context = _database.CreateDbContext();
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE consumption_imports DROP CONSTRAINT fail_import");
        }
    }
}

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CONSUMPTION_TEST_CONNECTION")))
            Skip = "Configure CONSUMPTION_TEST_CONNECTION para executar a integração em um banco temporário.";
    }
}

public sealed class PostgresTheoryAttribute : TheoryAttribute
{
    public PostgresTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CONSUMPTION_TEST_CONNECTION")))
            Skip = "Configure CONSUMPTION_TEST_CONNECTION para executar a integração em um banco temporário.";
    }
}

public sealed class ConsumptionDatabase : IDbContextFactory<AppDbContext>, IAsyncLifetime
{
    private string? _admin;
    private string? _name;
    private DbContextOptions<AppDbContext>? _options;
    public async Task InitializeAsync()
    {
        _admin = Environment.GetEnvironmentVariable("CONSUMPTION_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(_admin)) return;
        var builder = new NpgsqlConnectionStringBuilder(_admin);
        _name = "repypharma_consumption_test_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(builder.ConnectionString);
        await admin.OpenAsync();
        await using (var command = new NpgsqlCommand($"CREATE DATABASE \"{_name}\"", admin)) await command.ExecuteNonQueryAsync();
        builder.Database = _name;
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(builder.ConnectionString).Options;
        await using var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync();
    }
    public AppDbContext CreateDbContext() => new(_options ?? throw new InvalidOperationException("Banco temporário não inicializado."));
    public async Task ClearAsync()
    {
        await using var context = CreateDbContext();
        await context.Database.ExecuteSqlRawAsync("TRUNCATE daily_consumptions, monthly_consumptions, consumption_imports, items RESTART IDENTITY CASCADE");
    }
    public async Task DisposeAsync()
    {
        if (_name is null) return;
        NpgsqlConnection.ClearAllPools();
        await using var admin = new NpgsqlConnection(_admin);
        await admin.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE \"{_name}\" WITH (FORCE)", admin);
        await command.ExecuteNonQueryAsync();
    }
}
