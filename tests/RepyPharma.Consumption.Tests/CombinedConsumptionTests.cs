using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RepyPharma.Domain.Entities;
using RepyPharma.Models.Analytics;
using RepyPharma.Services.Import;
using Xunit;
using static RepyPharma.Consumption.Tests.ConsumptionImportTests;

namespace RepyPharma.Consumption.Tests;

public class CombinedConsumptionTests : IClassFixture<ConsumptionDatabase>
{
    private readonly ConsumptionDatabase _database;
    public CombinedConsumptionTests(ConsumptionDatabase database) => _database = database;
    private ConsumptionImportService Service(TimeProvider? clock = null) => new(_database, NullLogger<ConsumptionImportService>.Instance,
        clock ?? new FixedClock(Now), CombinedTestServices.Sector(), CombinedTestServices.Options());
    internal static string Sector(string quantity="31,00", string value="62,00", string month="08/2026", string unit="RESMA", string code="138", string price="2")
        => $"Setor:,997,FARMACIA,,,,\nEspécie:,1,MATERIAL,,,,\nData,,Produto,Unidade,Vl Unitário,Qtd Moviment.,Valor Total\n{month},,{code} - PAPEL A4,{unit},\"{price}\",\"{quantity}\",\"{value}\"\n,,,,Total da Espécie:,,\"{value}\"\n,,,,Total do Setor:,,\"{value}\"\n,,,,Total Geral:,,\"{value}\"\n";
    private async Task Seed(string code="138", string unit="RESMA")
    {
        await using var db=_database.CreateDbContext();
        db.Items.Add(new(){Code=code,Name="PAPEL A4",Unit=unit,ItemType=ItemType.Material,CreatedAt=DateTime.UtcNow});
        await db.SaveChangesAsync();
    }
    private static async Task<ConsumptionReportsValidationDto> Prepare(ConsumptionImportService service,string daily,string sector)
    {
        using var d=Csv(daily);using var s=Csv(sector);
        return await service.PrepareCombinedImportAsync(d,"diario.csv",s,"setorial.csv",new(2026,8,1));
    }

    [PostgresFact]
    public async Task PreviewIsReadOnlyAndConfirmationStoresReportedPriceAndBothNames()
    {
        await _database.ClearAsync();await Seed();var service=Service();
        var preview=await Prepare(service,Report(31,quantity:1),Sector());
        Assert.True(preview.CanConfirm,string.Join(";",preview.Issues.Select(i=>i.Message)));
        await using(var db=_database.CreateDbContext()) { Assert.Empty(await db.DailyConsumptions.ToListAsync());Assert.Empty(await db.ConsumptionImports.ToListAsync()); }
        var result=await service.ConfirmCombinedImportAsync(preview.ValidationId);
        Assert.Equal(ConsumptionOperationStatus.Completed,result.Status);Assert.Equal("setorial.csv",result.SectorSourceFileName);
        await using(var db=_database.CreateDbContext())
        {
            var monthly=await db.MonthlyConsumptions.SingleAsync();Assert.Equal(31,monthly.TotalQuantity);Assert.Equal(2,monthly.UnitPurchasePrice);
            Assert.True(monthly.IsComplete);Assert.Equal("diario.csv",(await db.ConsumptionImports.SingleAsync()).SourceFileName);
        }
        Assert.Equal(ConsumptionOperationStatus.Failed,(await service.ConfirmCombinedImportAsync(preview.ValidationId)).Status);
    }

    [PostgresFact]
    public async Task DifferencesBetweenReportsNeverBlockOrTransformTheReportedPrice()
    {
        await _database.ClearAsync();await Seed();var service=Service();
        var daily=Report(31);var first=daily.IndexOf("\"0\"",StringComparison.Ordinal);
        daily=daily[..first]+"\"0,660\""+daily[(first+3)..];
        var preview=await Prepare(service,daily,Sector("quantidade inválida","total inválido","10/2025","CAIXA",price:"3,987654"));
        Assert.True(preview.CanConfirm,string.Join(";",preview.Issues.Select(i=>i.Message)));
        Assert.Equal(1,preview.AssignedPriceCount);
        Assert.Equal(ConsumptionOperationStatus.Completed,(await service.ConfirmCombinedImportAsync(preview.ValidationId)).Status);
        await using var db=_database.CreateDbContext();var monthly=await db.MonthlyConsumptions.SingleAsync();
        Assert.Equal(0.660m,monthly.TotalQuantity);Assert.Equal(0.021m,monthly.AverageDailyConsumption);
        Assert.Equal(3.987654m,monthly.UnitPurchasePrice);
    }

    [PostgresFact]
    public async Task InvalidDailyUnitsStillBlockAgainstTheItemCatalog()
    {
        await _database.ClearAsync();await Seed();var service=Service();
        var preview=await Prepare(service,Report(31,unit:"CAIXA",quantity:1),Sector());
        Assert.False(preview.CanConfirm);Assert.Contains(preview.Issues,i=>i.Code=="RegisteredUnitMismatch");
    }

    [PostgresFact]
    public async Task NegativeAndZeroConsumptionsRetainTheReportedUnitPrice()
    {
        await _database.ClearAsync();await Seed();var service=Service();
        var negative=await Prepare(service,Report(31,quantity:-1),Sector("-31,00","-62,00"));
        Assert.True(negative.CanConfirm);Assert.Equal(0,negative.MissingPriceCount);
        await service.ConfirmCombinedImportAsync(negative.ValidationId);
        var zero=await Prepare(service,Report(31),Sector("0,00","0,00"));Assert.True(zero.CanConfirm);
        await service.ConfirmCombinedImportAsync(zero.ValidationId);
        await using var db=_database.CreateDbContext();var m=await db.MonthlyConsumptions.SingleAsync();Assert.Equal(0,m.TotalQuantity);Assert.Equal(2,m.UnitPurchasePrice);
        Assert.All(await db.DailyConsumptions.ToArrayAsync(),d=>Assert.Equal(0,d.Quantity));
    }

    [PostgresFact]
    public async Task UnknownDailyProductsAndOpenMonthlyPeriodsStillBlockPreview()
    {
        await _database.ClearAsync();var service=Service();
        var unknown=await Prepare(service,Report(31,quantity:1),Sector());Assert.False(unknown.CanConfirm);Assert.Equal(1,unknown.UnknownProductCount);
        await Seed();var wrong=await Prepare(service,Report(31,quantity:1),Sector(month:"09/2026"));Assert.True(wrong.CanConfirm);
        using var d=Csv(Report(15));using var s=Csv(Sector(month:"11/2026"));
        var open=await service.PrepareCombinedImportAsync(d,"d.csv",s,"s.csv",new(2026,11,1));Assert.False(open.CanConfirm);Assert.Contains(open.Issues,i=>i.Code=="OpenMonthlyPeriod");
    }

    [PostgresFact]
    public async Task MissingFileAndDiscardedPreviewNeverPersist()
    {
        await _database.ClearAsync();await Seed();var service=Service();using var d=Csv(Report(31));
        Assert.False((await service.PrepareCombinedImportAsync(d,"d.csv",null,"",new(2026,8,1))).CanConfirm);
        var preview=await Prepare(service,Report(31,quantity:1),Sector());service.DiscardCombinedPreview(preview.ValidationId);
        Assert.Equal(ConsumptionOperationStatus.Failed,(await service.ConfirmCombinedImportAsync(preview.ValidationId)).Status);
        await using var db=_database.CreateDbContext();Assert.Empty(await db.DailyConsumptions.ToArrayAsync());
    }

    [PostgresFact]
    public async Task ClosedMonthRequiresBothDocumentsAndCompleteCoverage()
    {
        await _database.ClearAsync();await Seed();var service=Service();
        using var daily=Csv(Report(31));
        var isolated=await service.ImportDailyConsumptionAsync(daily,"daily.csv",new(){ReferenceMonth=new(2026,8,1)});
        Assert.Contains(isolated.Issues,i=>i.Code=="BothReportsRequired");
        Assert.Equal(0,daily.Position);
        var incomplete=await Prepare(service,Report(27),Sector("0,00","0,00"));
        Assert.False(incomplete.CanConfirm);
        Assert.Contains(incomplete.Issues,i=>i.Code=="InvalidReportPeriod");
        await using var db=_database.CreateDbContext();Assert.Empty(await db.DailyConsumptions.ToArrayAsync());
    }

    [PostgresFact]
    public async Task IndependentCircuitsSerializeMonthlyConfirmationsWithoutDuplicates()
    {
        await _database.ClearAsync();await Seed();var first=Service();var second=Service();
        var firstPreview=await Prepare(first,Report(31,quantity:1),Sector());
        var secondPreview=await Prepare(second,Report(31,quantity:2),Sector("62,00","186,00",price:"3"));
        Assert.True(firstPreview.CanConfirm);Assert.True(secondPreview.CanConfirm);
        var results=await Task.WhenAll(first.ConfirmCombinedImportAsync(firstPreview.ValidationId),second.ConfirmCombinedImportAsync(secondPreview.ValidationId));
        Assert.All(results,r=>Assert.Equal(ConsumptionOperationStatus.Completed,r.Status));
        await using var db=_database.CreateDbContext();var rows=await db.DailyConsumptions.ToArrayAsync();
        Assert.Equal(31,rows.Length);Assert.Single(rows.Select(d=>d.Quantity).Distinct());
        var monthly=await db.MonthlyConsumptions.SingleAsync();
        Assert.Equal(rows.Sum(d=>d.Quantity),monthly.TotalQuantity);
        Assert.Equal(rows[0].Quantity==1 ? 2m : 3m,monthly.UnitPurchasePrice);
    }

    [PostgresTheory]
    [InlineData("0,075", "0,003")]
    [InlineData("-0,075", "-0,003")]
    public async Task AverageIsRoundedToThreeDecimalsInPreviewAndPersistence(string amount, string expectedText)
    {
        await _database.ClearAsync();await Seed();var service=Service();
        var text=Report(30);var first=text.IndexOf("\"0\"",StringComparison.Ordinal);
        text=text[..first]+"\""+amount+"\""+text[(first+3)..];
        using var daily=Csv(text);using var prices=Csv(Sector());
        var preview=await service.PrepareCombinedImportAsync(daily,"daily.csv",prices,"prices.csv",new(2026,4,1));
        var expected=decimal.Parse(expectedText,System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
        Assert.True(preview.CanConfirm);Assert.Equal(expected,Assert.Single(preview.Products).AverageDailyConsumption);
        await service.ConfirmCombinedImportAsync(preview.ValidationId);
        await using var db=_database.CreateDbContext();var monthly=await db.MonthlyConsumptions.SingleAsync();
        Assert.Equal(expected,monthly.AverageDailyConsumption);
        Assert.Equal(decimal.Parse(amount,System.Globalization.CultureInfo.GetCultureInfo("pt-BR")),monthly.TotalQuantity);
        Assert.Equal(30,monthly.RecordedDays);
    }

    [PostgresFact]
    public async Task ReimportIsIdempotentAndProductSetChangesAreBlockedInPreview()
    {
        await _database.ClearAsync();await Seed();var service=Service();
        for(var n=0;n<2;n++){var p=await Prepare(service,Report(31,quantity:1),Sector());Assert.True(p.CanConfirm);await service.ConfirmCombinedImportAsync(p.ValidationId);}
        await using(var db=_database.CreateDbContext())Assert.Equal(31,await db.DailyConsumptions.CountAsync());
        await Seed("139");var changed=await Prepare(service,Report(31,code:"139",quantity:1),Sector(code:"139"));
        Assert.False(changed.CanConfirm);Assert.Contains(changed.Issues,i=>i.Code=="ReimportProductSetChanged");
    }

    [PostgresFact]
    public async Task CatalogChangeBetweenPreviewAndConfirmationBlocksAllWrites()
    {
        await _database.ClearAsync();await Seed();var service=Service();var preview=await Prepare(service,Report(31,quantity:1),Sector());
        await using(var db=_database.CreateDbContext()){(await db.Items.SingleAsync()).Unit="CAIXA";await db.SaveChangesAsync();}
        Assert.Equal(ConsumptionOperationStatus.Failed,(await service.ConfirmCombinedImportAsync(preview.ValidationId)).Status);
        await using var check=_database.CreateDbContext();Assert.Empty(await check.DailyConsumptions.ToArrayAsync());
    }

    [PostgresFact]
    public async Task MonthlyFailureRollsBackDailyAndImportHistory()
    {
        await _database.ClearAsync();await Seed();var service=Service();var preview=await Prepare(service,Report(31,quantity:1),Sector());
        await using(var db=_database.CreateDbContext())await db.Database.ExecuteSqlRawAsync("ALTER TABLE monthly_consumptions ADD CONSTRAINT fail_combined CHECK (false) NOT VALID");
        try
        {
            Assert.Equal(ConsumptionOperationStatus.Failed,(await service.ConfirmCombinedImportAsync(preview.ValidationId)).Status);
            await using var db=_database.CreateDbContext();Assert.Empty(await db.DailyConsumptions.ToArrayAsync());Assert.Empty(await db.ConsumptionImports.ToArrayAsync());
        }
        finally { await using var db=_database.CreateDbContext();await db.Database.ExecuteSqlRawAsync("ALTER TABLE monthly_consumptions DROP CONSTRAINT fail_combined"); }
    }
    [PostgresFact]
    public async Task MultiplePositivePricesUseSectorWeightsWithoutComparingDailyQuantities()
    {
        await _database.ClearAsync();await Seed();var service=Service();
        var sector=Sector("10","10",price:"1")+Sector("20","40",price:"2")+Sector("999","9999",price:"0");
        var preview=await Prepare(service,Report(31,quantity:1),sector);
        Assert.True(preview.CanConfirm,string.Join(";",preview.Issues.Select(i=>i.Message)));
        Assert.Equal(1.666667m,Assert.Single(preview.Products).UnitPurchasePrice);
        await service.ConfirmCombinedImportAsync(preview.ValidationId);
        await using var db=_database.CreateDbContext();var monthly=await db.MonthlyConsumptions.SingleAsync();
        Assert.Equal(31,monthly.TotalQuantity);Assert.Equal(1,monthly.AverageDailyConsumption);Assert.Equal(1.666667m,monthly.UnitPurchasePrice);
    }

    [PostgresFact]
    public async Task InfeasibleCostBlocksConfirmationWithoutWrites()
    {
        await _database.ClearAsync();await Seed();var service=Service();
        var preview=await Prepare(service,Report(31,quantity:1),Sector("0","0",price:"1")+Sector("0","0",price:"2"));
        Assert.False(preview.CanConfirm);Assert.Contains(preview.Issues,i=>i.Code=="ZeroWeightedQuantity");
        Assert.Equal(ConsumptionOperationStatus.Failed,(await service.ConfirmCombinedImportAsync(preview.ValidationId)).Status);
        await using var db=_database.CreateDbContext();Assert.Empty(await db.DailyConsumptions.ToListAsync());Assert.Empty(await db.MonthlyConsumptions.ToListAsync());
    }

    [PostgresFact]
    public async Task MissingPriceIsAProcessingExceptionAndNeverCreatesItemsOrConsumption()
    {
        await _database.ClearAsync();await Seed();var service=Service();
        var preview=await Prepare(service,Report(31,quantity:1),Sector(code:"999",price:"7"));
        Assert.False(preview.CanConfirm);Assert.Equal(1,preview.MissingPriceCount);Assert.Equal(0,preview.UnknownProductCount);
        Assert.Contains(preview.Issues,i=>i.Code=="MissingUnitPrice");
        await using var db=_database.CreateDbContext();Assert.Single(await db.Items.ToListAsync());
        Assert.Empty(await db.MonthlyConsumptions.ToListAsync());Assert.Empty(await db.DailyConsumptions.ToListAsync());
    }

    [PostgresFact]
    public async Task ReimportUpdatesTheCostButInfeasibleNewCostPreservesThePreviousTransaction()
    {
        await _database.ClearAsync();await Seed();var service=Service();
        var first=await Prepare(service,Report(31,quantity:1),Sector(price:"1,123456"));
        await service.ConfirmCombinedImportAsync(first.ValidationId);
        var corrected=await Prepare(service,Report(31,quantity:1),Sector(price:"4,5678"));
        await service.ConfirmCombinedImportAsync(corrected.ValidationId);
        var noPrice=await Prepare(service,Report(31,quantity:9),Sector(price:"0"));
        Assert.False(noPrice.CanConfirm);Assert.Contains(noPrice.Issues,i=>i.Code=="NoValidUnitPrice");
        await using var db=_database.CreateDbContext();var monthly=await db.MonthlyConsumptions.SingleAsync();
        Assert.Equal(4.5678m,monthly.UnitPurchasePrice);Assert.Equal(31,monthly.TotalQuantity);Assert.Equal(31,await db.DailyConsumptions.CountAsync());
        Assert.Equal(2,await db.ConsumptionImports.CountAsync());
    }

    [PostgresFact]
    public async Task ExpiredPreviewAndChangedCoverageDoNotOverwriteData()
    {
        await _database.ClearAsync();await Seed();var clock=new MutableClock{Now=CombinedConsumptionTestsClock};var service=Service(clock);
        var preview=await Prepare(service,Report(31,quantity:1),Sector());clock.Now=clock.Now.AddHours(1);
        Assert.Equal(ConsumptionOperationStatus.Failed,(await service.ConfirmCombinedImportAsync(preview.ValidationId)).Status);
        await using(var db=_database.CreateDbContext())
        {
            var id=(await db.Items.SingleAsync()).Id;db.DailyConsumptions.Add(new(){ItemId=id,Date=new(2026,8,1),Quantity=1});await db.SaveChangesAsync();
        }
        var partial=await Prepare(service,Report(31,quantity:1),Sector());Assert.False(partial.CanConfirm);Assert.Contains(partial.Issues,i=>i.Code=="ReimportCoverageChanged");
    }
    [PostgresFact]
    public async Task SimultaneousServiceRequestsAreRejectedAndCancellationReleasesGate()
    {
        await _database.ClearAsync();await Seed();var delayed=new DelayedFactory(_database);
        var service=new ConsumptionImportService(delayed,NullLogger<ConsumptionImportService>.Instance,new FixedClock(Now),CombinedTestServices.Sector(),CombinedTestServices.Options());
        using var cancel=new CancellationTokenSource();using var d=Csv(Report(31,quantity:1));using var s=Csv(Sector());
        delayed.DelayNext=true;
        var first=service.PrepareCombinedImportAsync(d,"d.csv",s,"s.csv",new(2026,8,1),cancellationToken:cancel.Token);
        await delayed.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
        using var duplicateDaily=Csv(Report(31));using var duplicateSector=Csv(Sector());
        var duplicate=await service.PrepareCombinedImportAsync(duplicateDaily,"d.csv",duplicateSector,"s.csv",new(2026,8,1));
        Assert.Contains(duplicate.Issues,i=>i.Code=="OperationInProgress");Assert.Equal(0,duplicateDaily.Position);
        cancel.Cancel();await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>first);
        var valid=await Prepare(service,Report(31,quantity:1),Sector());Assert.True(valid.CanConfirm);
        delayed.Reset();delayed.DelayNext=true;
        var confirmation=service.ConfirmCombinedImportAsync(valid.ValidationId);await delayed.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.Contains((await service.ConfirmCombinedImportAsync(valid.ValidationId)).Issues,i=>i.Code=="OperationInProgress");
        delayed.Release.TrySetResult(true);Assert.Equal(ConsumptionOperationStatus.Completed,(await confirmation).Status);
        await using var db=_database.CreateDbContext();Assert.Single(await db.ConsumptionImports.ToListAsync());
    }

    [ReferenceReportsFact]
    public async Task RealReportsUseUniqueOrWeightedSectorCostsWithoutDailyReconciliation()
    {
        await _database.ClearAsync();
        var sectorPath=Environment.GetEnvironmentVariable("CONSUMPTION_SECTOR_REFERENCE")!;
        await using var sectorInput=File.OpenRead(sectorPath);
        var sector=await CombinedTestServices.Sector().ProcessSectorConsumptionCsvAsync(sectorInput,Path.GetFileName(sectorPath));
        foreach(var group in sector.Records.GroupBy(r=>r.ProductCode))
        {
            var rows=group.Where(r=>r.UnitValue>0).ToArray();
            var prices=rows.Select(r=>r.UnitValue!.Value).Distinct().ToArray();
            var product=sector.Products.Single(p=>p.ProductCode==group.Key);
            if(prices.Length==1)Assert.Equal(prices[0],product.UnitPurchasePrice);
            else if(prices.Length==0)Assert.Null(product.UnitPurchasePrice);
            else if(rows.All(r=>r.MovementQuantity.HasValue && r.MovementTotalValue.HasValue))
            {
                var quantity=rows.Sum(r=>r.MovementQuantity!.Value);
                var total=rows.Sum(r=>r.MovementTotalValue!.Value);
                var expected=quantity==0 ? (decimal?)null : decimal.Round(total/quantity,6,MidpointRounding.AwayFromZero);
                if(expected>0)Assert.Equal(expected,product.UnitPurchasePrice);else Assert.Null(product.UnitPurchasePrice);
            }
        }
        await using var daily=File.OpenRead(Environment.GetEnvironmentVariable("CONSUMPTION_DAILY_REFERENCE")!);
        var reference=new DateOnly(2026,10,1);
        // Catálogo temporário derivado exclusivamente do diário, incluindo suas unidades reais.
        using(var reader=CsvReportFormatForTest(daily))
        {
            var items=new Dictionary<string,Item>();
            while(await reader.ReadAsync())
            {
                if(reader.FieldCount<3)continue;
                var code=reader.GetString(0).Trim();
                if(code.Length>0 && code.All(char.IsAsciiDigit))
                    items.TryAdd(code,new(){Code=code,Name=reader.GetString(1),Unit=reader.GetString(2).Trim(),CreatedAt=DateTime.UtcNow,ItemType=ItemType.Material});
            }
            await using var db=_database.CreateDbContext();db.Items.AddRange(items.Values);await db.SaveChangesAsync();
        }
        daily.Position=0;await using var input=File.OpenRead(sectorPath);var service=Service();
        var result=await service.PrepareCombinedImportAsync(daily,Path.GetFileName(daily.Name),input,Path.GetFileName(input.Name),reference);
        Assert.DoesNotContain(result.Issues,i=>new[]{"QuantityMismatch","ReportsUnitMismatch","ReportPeriodMismatch","MissingDaily","MissingSector","FinancialTotalMismatch"}.Contains(i.Code));
        Assert.DoesNotContain(result.Issues,i=>i.Code=="ConflictingUnitPrice");
        Assert.Equal(!result.Issues.Any(i=>i.Severity>=AnalysisSeverity.Error),result.CanConfirm);
        Assert.All(result.Products,p=>Assert.Equal(decimal.Round(p.AverageDailyConsumption!.Value,3),p.AverageDailyConsumption));
        await using(var db=_database.CreateDbContext()){Assert.Empty(await db.DailyConsumptions.ToListAsync());Assert.Empty(await db.MonthlyConsumptions.ToListAsync());}
    }

    private static CsvHelper.CsvReader CsvReportFormatForTest(Stream stream)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        return CsvHelper.CsvReader.Create(stream,new CsvHelper.CsvReaderOptions{HasHeader=false,Separator=',',Encoding=System.Text.Encoding.GetEncoding(1252),FieldCountMismatch=CsvHelper.CsvFieldCountHandling.Allow},leaveOpen:true);
    }

    private sealed class DelayedFactory(Microsoft.EntityFrameworkCore.IDbContextFactory<RepyPharma.Data.AppDbContext> inner):Microsoft.EntityFrameworkCore.IDbContextFactory<RepyPharma.Data.AppDbContext>
    {
        public bool DelayNext;public TaskCompletionSource<bool> Entered=new(TaskCreationOptions.RunContinuationsAsynchronously),Release=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Reset(){Entered=new(TaskCreationOptions.RunContinuationsAsynchronously);Release=new(TaskCreationOptions.RunContinuationsAsynchronously);}
        public RepyPharma.Data.AppDbContext CreateDbContext()=>inner.CreateDbContext();
        public async Task<RepyPharma.Data.AppDbContext> CreateDbContextAsync(CancellationToken token=default)
        {if(DelayNext){DelayNext=false;Entered.TrySetResult(true);await Release.Task.WaitAsync(token);}return inner.CreateDbContext();}
    }
    private static DateTimeOffset CombinedConsumptionTestsClock => ConsumptionImportTests.Now;
    private sealed class MutableClock:TimeProvider {public DateTimeOffset Now{get;set;}public override DateTimeOffset GetUtcNow()=>Now;}

}

public sealed class ReferenceReportsFactAttribute:FactAttribute
{
    public ReferenceReportsFactAttribute()
    {
        if(new[]{"CONSUMPTION_TEST_CONNECTION","CONSUMPTION_DAILY_REFERENCE","CONSUMPTION_SECTOR_REFERENCE"}.Any(k=>string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(k))))
            Skip="Configure a conexão temporária e os dois CSVs de referência para esta validação.";
    }
}
