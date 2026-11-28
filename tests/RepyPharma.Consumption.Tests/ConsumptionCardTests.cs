using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using RepyPharma.Components.Pages.DataUpdates;
using RepyPharma.Models.Analytics;
using RepyPharma.Services.Import.Interfaces;
using RepyPharma.Services.Interfaces;
using Xunit;
using static RepyPharma.Components.Pages.DataUpdates.DataUpdates;

namespace RepyPharma.Consumption.Tests;

public class ConsumptionCardTests
{
    private static async Task<string> Render<T>(Dictionary<string, object?> parameters, ImportServiceStub? service = null) where T : IComponent
    {
        using var services = new ServiceCollection().AddLogging()
            .AddSingleton<IJSRuntime, NoBrowserJs>()
            .AddSingleton<IAuthService>(_ => DispatchProxy.Create<IAuthService, AuthorizedUser>())
            .AddScoped<RepyPharma.Services.Abstractions.LoadingOperationState>()
            .AddSingleton<IConsumptionImportService>(service ?? new())
            .AddSingleton<TimeProvider>(new FixedClock(ConsumptionImportTests.Now)).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync<T>(ParameterView.FromDictionary(parameters));
            return root.ToHtmlString();
        });
    }

    [Fact]
    public async Task EveryAvailableCardKeepsItsUploadPanelSeparate()
    {
        var html = await Render<ConsumptionImport>(new()
        {
            [nameof(ConsumptionImport.Today)] = new DateOnly(2026, 11, 15),
            [nameof(ConsumptionImport.FirstYear)] = 2025,
            [nameof(ConsumptionImport.CanImport)] = true
        });
        for (var month = 1; month <= 11; month++) Assert.Contains($"data-period=\"2026-{month:00}\"", html);
        Assert.DoesNotContain("data-period=\"2026-12\"", html);
        Assert.DoesNotContain("type=\"file\"", html);
        Assert.DoesNotContain("Processar relatórios", html);
        Assert.Contains("Anexar relatórios de novembro", System.Net.WebUtility.HtmlDecode(html));
    }

    [Fact]
    public async Task CompletedMonthHasUpdateActionAndCurrentMonthBecomesPendingNextDay()
    {
        var imported = new ImportRecord(Guid.NewGuid(), ImportKind.Consumption, new(2026, 11, 1), "novembro.csv",
            ConsumptionImportTests.Now, ConsumptionOperationStatus.Completed, "Importado", new(2026, 11, 15));
        var parameters = new Dictionary<string, object?>
        {
            [nameof(ConsumptionImport.Today)] = new DateOnly(2026, 11, 15),
            [nameof(ConsumptionImport.FirstYear)] = 2025,
            [nameof(ConsumptionImport.CanImport)] = true,
            [nameof(ConsumptionImport.Imports)] = new[] { imported }
        };
        Assert.Contains("Atualizar relatórios de novembro", System.Net.WebUtility.HtmlDecode(await Render<ConsumptionImport>(parameters)));
        parameters[nameof(ConsumptionImport.Today)] = new DateOnly(2026, 11, 16);
        var html = await Render<ConsumptionImport>(parameters);
        Assert.Contains("Anexar relatórios de novembro", System.Net.WebUtility.HtmlDecode(html));
        Assert.DoesNotContain("Atualizar relatórios de novembro", System.Net.WebUtility.HtmlDecode(html));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(8)]
    [InlineData(11)]
    public async Task AttachmentUsesCardPeriodAndSuccessfulDatabaseResult(int month)
    {
        var service = new ImportServiceStub();
        using var component = new CsvAttachment();
        Set(component, nameof(CsvAttachment.Kind), ImportKind.Consumption);
        Set(component, nameof(CsvAttachment.ReferenceDate), new DateOnly(2026, month, 1));
        Set(component, "ConsumptionService", service);
        Set(component, "Logger", NullLogger<CsvAttachment>.Instance);
        ConsumptionImportResultDto? completed = null;
        Set(component, nameof(CsvAttachment.OnImported), EventCallback.Factory.Create<ConsumptionImportResultDto>(this, r => completed = r));
        Call(component, "SelectFile", new InputFileChangeEventArgs([new BrowserCsv()]));
        await (Task)Call(component, "ConfirmAsync")!;
        Assert.Equal(new DateOnly(2026, month, 1), service.ReferenceMonth);
        Assert.NotNull(completed);
        Assert.Equal(ConsumptionOperationStatus.Completed, completed.Status);
    }

    [Fact]
    public async Task InvalidReportStaysOpenAndDisplaysErrorWithoutSuccessCallback()
    {
        var service = new ImportServiceStub { Reject = true };
        using var component = new CsvAttachment();
        Set(component, nameof(CsvAttachment.ReferenceDate), new DateOnly(2026, 8, 1));
        Set(component, "ConsumptionService", service);
        Set(component, "Logger", NullLogger<CsvAttachment>.Instance);
        var completed = false;
        Set(component, nameof(CsvAttachment.OnImported), EventCallback.Factory.Create<ConsumptionImportResultDto>(this, _ => completed = true));
        Call(component, "SelectFile", new InputFileChangeEventArgs([new BrowserCsv()]));
        await (Task)Call(component, "ConfirmAsync")!;
        Assert.False(completed);
        Assert.Contains("dias", Assert.IsType<string>(Get(component, "_error")));
        Assert.NotNull(Get(component, "_selected"));
    }

    [Fact]
    public async Task PageLoadsRealHistoryInsteadOfSeedingDemonstration()
    {
        var service = new ImportServiceStub();
        var html = await Render<DataUpdates>(new(), service);
        Assert.True(service.ReadHistory);
        Assert.DoesNotContain("Dados fictícios", html);
        Assert.DoesNotContain("consumo_2026_", html);
        Assert.Contains("data-period=\"2026-11\"", html);
        Assert.DoesNotContain("data-period=\"2026-12\"", html);
    }

    private static object? Call(object target, string name, params object[] args)
        => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args);
    private static object? Get(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target);
    private static void Set(object target, string name, object value)
    {
        var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property is not null) property.SetValue(target, value);
        else target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
    }
    public class AuthorizedUser : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.ReturnType == typeof(bool) ? true : null;
    }
    private sealed class NoBrowserJs : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new NotSupportedException();
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => throw new NotSupportedException();
    }
    private sealed class ImportServiceStub : IConsumptionImportService
    {
        public DateOnly? ReferenceMonth { get; private set; }
        public bool ReadHistory { get; private set; }
        public bool Reject { get; init; }
        public Task<ConsumptionImportResultDto> ImportDailyConsumptionAsync(Stream stream, string name, ConsumptionImportOptionsDto? options = null, CancellationToken cancellationToken = default)
        {
            ReferenceMonth = options!.ReferenceMonth;
            return Task.FromResult(new ConsumptionImportResultDto
            {
                ImportId = Guid.NewGuid(), SourceFileName = name,
                Status = Reject ? ConsumptionOperationStatus.Failed : ConsumptionOperationStatus.Completed,
                Issues = Reject ? [new() { Code = "IncompleteHistory", Severity = AnalysisSeverity.Error, Message = "Faltam dias no relatório." }] : []
            });
        }
        public Task<IReadOnlyList<ConsumptionImportResultDto>> GetImportsAsync(CancellationToken cancellationToken = default)
        { ReadHistory = true; return Task.FromResult<IReadOnlyList<ConsumptionImportResultDto>>([]); }
        public Task<ConsumptionReportsValidationDto> PrepareCombinedImportAsync(Stream? d, string dn, Stream? s, string sn, DateOnly m, IProgress<ConsumptionImportProgressDto>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CombinedConsumptionImportResultDto> ConfirmCombinedImportAsync(Guid id, IProgress<ConsumptionImportProgressDto>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void DiscardCombinedPreview(Guid id) { }
        public Task<ConsumptionImportResultDto?> GetImportResultAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConsumptionImportValidationDto> ValidateImportAsync(Stream stream, string name, ConsumptionImportOptionsDto? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
    private sealed class BrowserCsv : IBrowserFile
    {
        public string Name => "consumo.csv";
        public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;
        public long Size => 20;
        public string ContentType => "text/csv";
        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) => new MemoryStream(Encoding.Latin1.GetBytes("Produto,,Unidade,01\n"));
    }
}
