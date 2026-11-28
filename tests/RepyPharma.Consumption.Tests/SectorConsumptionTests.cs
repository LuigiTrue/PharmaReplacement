using RepyPharma.Models.Analytics;
using Xunit;
using static RepyPharma.Consumption.Tests.ConsumptionImportTests;

namespace RepyPharma.Consumption.Tests;

public class SectorConsumptionTests
{
    [Fact]
    public async Task CopiesRawPriceRegardlessOfQuantitiesTotalsUnitsAndDates()
    {
        using var input = Csv(CombinedConsumptionTests.Sector("inválido", "valor inválido", "data inválida", "CAIXA", price: "5,2775"));
        var result = await CombinedTestServices.Sector().ProcessSectorConsumptionCsvAsync(input, "s.csv");
        Assert.Equal(5.2775m, Assert.Single(result.Products).UnitPurchasePrice);
        Assert.Equal(5.2775m, Assert.Single(result.Records).UnitValue);
        Assert.DoesNotContain(result.Issues, i => i.Severity >= AnalysisSeverity.Error);
    }

    [Fact]
    public async Task RepeatedPricesAcrossSectorsAndRepeatedHeadersAreAccepted()
    {
        var first = CombinedConsumptionTests.Sector(price: "2,1234");
        var second = CombinedConsumptionTests.Sector("-700", "900000", "10/2025", "FRASCO", price: "2,1234")
            .Replace("Setor:,997,", "Setor:,998,");
        using var input = Csv(first + second + "Obs: observação,,,,,,\n");
        var result = await CombinedTestServices.Sector().ProcessSectorConsumptionCsvAsync(input, "s.csv");
        Assert.Equal(2, result.Records.Count);
        Assert.Equal(2.1234m, Assert.Single(result.Products).UnitPurchasePrice);
        Assert.DoesNotContain(result.Issues, i => i.Severity >= AnalysisSeverity.Error);
    }

    [Fact]
    public async Task ZeroPricesAreIgnoredWhenThereIsOneDistinctPositivePrice()
    {
        using var input = Csv(CombinedConsumptionTests.Sector(price: "0") + CombinedConsumptionTests.Sector(price: "2,0001"));
        var report = await CombinedTestServices.Sector().ProcessSectorConsumptionCsvAsync(input, "s.csv");
        Assert.Empty(report.Issues);
        Assert.Equal(2.0001m, Assert.Single(report.Products).UnitPurchasePrice);
    }

    [Theory]
    [InlineData("", "InvalidUnitPrice")]
    [InlineData("erro", "InvalidUnitPrice")]
    [InlineData("-2", "InvalidUnitPrice")]
    [InlineData("1000000000000", "UnitPricePrecision")]
    [InlineData("0,0000001", "UnitPricePrecision")]
    public async Task InvalidPricesAreNeverConvertedRoundedOrMadeAbsolute(string price, string errorCode)
    {
        using var input = Csv(CombinedConsumptionTests.Sector(price: price));
        var report = await CombinedTestServices.Sector().ProcessSectorConsumptionCsvAsync(input, "s.csv");
        Assert.Contains(report.Issues, i => i.Code == errorCode);
        Assert.Null(Assert.Single(report.Products).UnitPurchasePrice);
    }

    [Fact]
    public async Task OnlyZeroPricesProduceAProcessingException()
    {
        using var input = Csv("Produto,Vl Unitário\n138,0\n");
        var report = await CombinedTestServices.Sector().ProcessSectorConsumptionCsvAsync(input, "s.csv");
        Assert.Null(Assert.Single(report.Products).UnitPurchasePrice);
        Assert.Contains(report.Issues,i=>i.Code=="NoValidUnitPrice" && i.ItemCode=="138");
    }

    [Fact]
    public async Task OnlyProductAndPriceColumnsAreRequiredWithDynamicColumnOrder()
    {
        using var input = Csv("Vl Unitário,,Produto\n\"0,5701\",,\"138 - PAPEL, A4\"\nProduto,Vl Unitário\n139,\"1,123456\"\n");
        var report = await CombinedTestServices.Sector().ProcessSectorConsumptionCsvAsync(input, "s.csv");
        Assert.Equal(2, report.Products.Count);
        Assert.Equal(0.5701m, report.Products.Single(p => p.ProductCode == "138").UnitPurchasePrice);
        Assert.Equal(1.123456m, report.Products.Single(p => p.ProductCode == "139").UnitPurchasePrice);
        Assert.Empty(report.Issues);
    }

    [Fact]
    public async Task OtherFieldsMayContainHeaderAndSubtotalWordsWithoutHidingProducts()
    {
        using var input=Csv("Produto,Descrição,Vl Unitário\n138,SETOR DE OBSERVACAO,1\n139,Produto,2\n140,Total geral,3\n");
        var report=await CombinedTestServices.Sector().ProcessSectorConsumptionCsvAsync(input,"s.csv");
        Assert.Equal(3,report.Products.Count);Assert.Empty(report.Issues);
    }

    [Fact]
    public async Task MissingSubtotalsAndWrongGrandTotalDoNotAffectPrice()
    {
        using var input = Csv("Produto,Vl Unitário\n138,2\n,,,,Total Geral:,,\"valor inválido\"\n");
        var report = await CombinedTestServices.Sector().ProcessSectorConsumptionCsvAsync(input, "s.csv");
        Assert.Equal(2, Assert.Single(report.Products).UnitPurchasePrice);
        Assert.Empty(report.Issues);
    }

    [Fact]
    public async Task DistinctPositivePricesUseAllPositivePriceRowsAndExcludeZeroPriceMovements()
    {
        var text=CombinedConsumptionTests.Sector("1","10",price:"10")
            +CombinedConsumptionTests.Sector("1","10",price:"10")
            +CombinedConsumptionTests.Sector("3","60",price:"20")
            +CombinedConsumptionTests.Sector("1000","99999",price:"0");
        using var input=Csv(text);
        var report=await CombinedTestServices.Sector().ProcessSectorConsumptionCsvAsync(input,"s.csv");
        Assert.Empty(report.Issues);Assert.Equal(16m,Assert.Single(report.Products).UnitPurchasePrice);
    }

    [Fact]
    public async Task FinancialOperandsKeepTheirDecimalPrecisionUntilTheFinalDivision()
    {
        using var input=Csv(CombinedConsumptionTests.Sector("0,1","0,0000004",price:"0,000004")
            +CombinedConsumptionTests.Sector("0,1","0,0000004",price:"0,000005"));
        var report=await CombinedTestServices.Sector().ProcessSectorConsumptionCsvAsync(input,"s.csv");
        Assert.Empty(report.Issues);Assert.Equal(0.000004m,Assert.Single(report.Products).UnitPurchasePrice);
    }

    [Theory]
    [InlineData("1","1","2","4","1,666667")]
    [InlineData("-1","-1","-2","-4","1,666667")]
    [InlineData("2","2","-1","-2","0")]
    public async Task WeightedCalculationPreservesReturnSigns(string q1,string v1,string q2,string v2,string expectedText)
    {
        using var input=Csv(CombinedConsumptionTests.Sector(q1,v1,price:"1")+CombinedConsumptionTests.Sector(q2,v2,price:"2"));
        var report=await CombinedTestServices.Sector().ProcessSectorConsumptionCsvAsync(input,"s.csv");
        var expected=decimal.Parse(expectedText,System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
        if(expected>0){Assert.Empty(report.Issues);Assert.Equal(expected,Assert.Single(report.Products).UnitPurchasePrice);}
        else{Assert.Null(Assert.Single(report.Products).UnitPurchasePrice);Assert.Contains(report.Issues,i=>i.Code=="InvalidWeightedPrice");}
    }

    [Theory]
    [InlineData("0","1","0","2","ZeroWeightedQuantity")]
    [InlineData("1","1","-1","-2","ZeroWeightedQuantity")]
    [InlineData("inválido","1","1","2","InvalidWeightedPriceData")]
    [InlineData("1","inválido","1","2","InvalidWeightedPriceData")]
    [InlineData("1","-1","1","-2","InvalidWeightedPrice")]
    [InlineData("1","0,00000001","1","0,00000001","UnitPricePrecision")]
    [InlineData("1","9999999999999999999999999999","1","9999999999999999999999999999","InvalidWeightedPrice")]
    [InlineData("1","79228162514264337593543950335","1","1","UnitPriceArithmeticFailure")]
    public async Task InfeasibleWeightedCalculationProducesAProcessingException(string q1,string v1,string q2,string v2,string expectedCode)
    {
        using var input=Csv(CombinedConsumptionTests.Sector(q1,v1,price:"1")+CombinedConsumptionTests.Sector(q2,v2,price:"2"));
        var logger=new CapturingLogger();
        var report=await new RepyPharma.Services.Import.SectorConsumptionImportService(logger).ProcessSectorConsumptionCsvAsync(input,"s.csv");
        Assert.Contains(report.Issues,i=>i.Code==expectedCode && i.ItemCode=="138" && i.Severity==AnalysisSeverity.Error);
        Assert.Null(Assert.Single(report.Products).UnitPurchasePrice);
        Assert.Single(logger.Exceptions);
    }

    [Fact]
    public async Task MovementColumnsAreRequiredOnlyForMultiplePositivePrices()
    {
        using var input=Csv("Produto,Vl Unitário\n138,1\n138,2\n");
        var report=await CombinedTestServices.Sector().ProcessSectorConsumptionCsvAsync(input,"s.csv");
        Assert.Null(Assert.Single(report.Products).UnitPurchasePrice);
        Assert.Contains(report.Issues,i=>i.Code=="InvalidWeightedPriceData");
    }

    private sealed class CapturingLogger : Microsoft.Extensions.Logging.ILogger<RepyPharma.Services.Import.SectorConsumptionImportService>
    {
        public List<Exception> Exceptions {get;}=[];
        public IDisposable? BeginScope<TState>(TState state) where TState:notnull=>null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel level)=>true;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel level,Microsoft.Extensions.Logging.EventId id,TState state,Exception? exception,Func<TState,Exception?,string> formatter)
        {if(exception is not null)Exceptions.Add(exception);}
    }

    [Theory]
    [InlineData("Produto,Quantidade\n138,1\n")]
    [InlineData("")]
    [InlineData("Produto,Vl Unitário\n138,\"aspas abertas\n")]
    public async Task InvalidCsvStructureStillBlocksImport(string text)
    {
        using var input = Csv(text);
        var report = await CombinedTestServices.Sector().ProcessSectorConsumptionCsvAsync(input, "s.csv");
        Assert.Contains(report.Issues, i => i.Severity >= AnalysisSeverity.Error);
    }
}
