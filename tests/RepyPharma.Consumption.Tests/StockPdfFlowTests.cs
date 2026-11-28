using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RepyPharma.Components.Pages.DataUpdates;
using RepyPharma.Infrastructure.Json;
using RepyPharma.Services.Import;
using RepyPharma.Shared.Components;
using Xunit;

namespace RepyPharma.Consumption.Tests;

public class StockPdfFlowTests : IClassFixture<ConsumptionDatabase>
{
    private readonly ConsumptionDatabase _database;
    public StockPdfFlowTests(ConsumptionDatabase database) => _database = database;

    [Fact]
    public async Task StockTabAdvertisesPdfInsteadOfCsvOrSimulation()
    {
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync<StockImport>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(StockImport.Today)] = new DateOnly(2026, 11, 24), [nameof(StockImport.CanImport)] = true
            }));
            return System.Net.WebUtility.HtmlDecode(root.ToHtmlString());
        });
        Assert.Contains("Anexar PDF do estoque", html);
        Assert.Contains("Relatório de Conferência dos Lotes", html);
        Assert.Contains("Arquivo .pdf", html);
        Assert.Contains("10 MB", html);
        Assert.DoesNotContain("CSV", html);
        Assert.DoesNotContain("simulado", html);
    }

    [Fact]
    public async Task InvalidPdfNeverSavesStockOrReportsSuccess()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "Não é um PDF de estoque");
            var component = new InputFileComponent();
            Set(component, "Parser", new PdfParserService());
            Set(component, "Storage", new PdfStorageService(new NoDatabaseFactory(), new StockDataState()));
            var imported = false;
            Set(component, "OnImported", EventCallback.Factory.Create<string>(this, _ => imported = true));
            await Import(component, path, "estoque.pdf");
            Assert.False(imported);
            Assert.True((bool)Get(component, "_statusIsError")!);
        }
        finally { File.Delete(path); }
    }

    [PostgresFact]
    public async Task ExistingPdfFlowPersistsStockAndNotifiesOnlyAfterCommit()
    {
        await _database.ClearAsync();
        var path = StockPdfFixture.Create();
        try
        {
            var parser = new PdfParserService();
            var validation = parser.ValidateStockMirrorPdf(path);
            Assert.True(validation.IsValid, validation.Message);
            var state = new StockDataState();
            var stockChanged = false;
            state.OnChange += () => stockChanged = true;
            var component = new InputFileComponent();
            Set(component, "Parser", parser);
            Set(component, "Storage", new PdfStorageService(_database, state));
            string? imported = null;
            Set(component, "OnImported", EventCallback.Factory.Create<string>(this, async name =>
            {
                // O callback da tela só pode disparar quando o saldo já está disponível em outra conexão.
                await using var context = _database.CreateDbContext();
                Assert.Equal(10.5m, (await context.StockBalances.SingleAsync()).Quantity);
                imported = name;
            }));
            await Import(component, path, "Relatorio_Original.PDF");
            Assert.True(stockChanged);
            Assert.Equal("Relatorio_Original.PDF", imported);
            Assert.False((bool)Get(component, "_statusIsError")!);
            await using var check = _database.CreateDbContext();
            Assert.Equal("138", (await check.Items.SingleAsync()).Code);
            Assert.Equal("RESMA", (await check.Items.SingleAsync()).Unit);
            Assert.Equal("L001", (await check.Batches.SingleAsync()).BatchNumber);
            Assert.Equal("997", (await check.Locations.SingleAsync()).Code);
            Assert.Empty(await check.DailyConsumptions.ToListAsync());
        }
        finally { File.Delete(path); }
    }

    [PostgresFact]
    public async Task SaveFailureDoesNotReportSuccessfulPdfImport()
    {
        await _database.ClearAsync();
        await using (var context = _database.CreateDbContext())
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE stock_balances ADD CONSTRAINT fail_stock_import CHECK (false) NOT VALID");
        var path = StockPdfFixture.Create();
        try
        {
            var component = new InputFileComponent();
            Set(component, "Parser", new PdfParserService());
            Set(component, "Storage", new PdfStorageService(_database, new StockDataState()));
            var imported = false;
            Set(component, "OnImported", EventCallback.Factory.Create<string>(this, _ => imported = true));
            await Assert.ThrowsAsync<DbUpdateException>(() => Import(component, path, "estoque.pdf"));
            Assert.False(imported);
            await using var context = _database.CreateDbContext();
            Assert.Empty(await context.Items.ToListAsync());
            Assert.Empty(await context.StockBalances.ToListAsync());
        }
        finally
        {
            File.Delete(path);
            await using var context = _database.CreateDbContext();
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE stock_balances DROP CONSTRAINT fail_stock_import");
        }
    }

    private static Task Import(InputFileComponent component, string path, string name)
        => (Task)typeof(InputFileComponent).GetMethod("ImportStockMirrorAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(component, [path, name])!;
    private static object? Get(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.SetValue(target, value);
}

internal static class StockPdfFixture
{
    // Documento mínimo com o contrato visual já lido pelo parser existente, sem biblioteca adicional.
    public static string Create()
    {
        var lines = new (int X, int Y, string Text)[]
        {
            (20,810,"SOULMV - Sistema de Gerenciamento de Estoque"),
            (20,795,"Relatório de Conferência dos Lotes"), (20,780,"Produto"),
            (20,700,"138"), (60,700,"PAPEL A4"), (160,700,"RESMA"),
            (220,700,"10,500"), (260,700,"L001"), (315,700,"31/12/2027"),
            (360,700,"997"), (470,700,"10,500")
        };
        var content = "BT\n/F1 8 Tf\n" + string.Join("\n", lines.Select(l => $"1 0 0 1 {l.X} {l.Y} Tm ({l.Text}) Tj")) + "\nET\n";
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            $"<< /Length {Encoding.Latin1.GetByteCount(content)} >>\nstream\n{content}endstream"
        };
        using var stream = new MemoryStream();
        void Write(string value) => stream.Write(Encoding.Latin1.GetBytes(value));
        Write("%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Length; i++) { offsets.Add(stream.Position); Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n"); }
        var xref = stream.Position;
        Write($"xref\n0 {offsets.Count}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) Write($"{offset:0000000000} 00000 n \n");
        Write($"trailer\n<< /Size {offsets.Count} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        var path = Path.Combine(Path.GetTempPath(), "repypharma-stock-" + Guid.NewGuid().ToString("N") + ".pdf");
        File.WriteAllBytes(path, stream.ToArray());
        return path;
    }
}
