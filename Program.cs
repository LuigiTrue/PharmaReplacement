using RepyPharma.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using ApexCharts;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using RepyPharma.Data;
using RepyPharma.Services.Implementatios;
using RepyPharma.Services.Import;
using RepyPharma.Services.Inventory;
using RepyPharma.Services.Replenishment;
using RepyPharma.Infrastructure.Json;
using RepyPharma.Infrastructure.Repositories;
using RepyPharma.Infrastructure.Repositories.Interfaces;
using RepyPharma.Services.Import.Interfaces;
using RepyPharma.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();
builder.Services.AddApexCharts();
builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});


//Services
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductStockService, ProductStockService>();
builder.Services.AddScoped<IStockJsonService, StockJsonService>();
builder.Services.AddScoped<IMinimumStockService, MinimumStockService>();
builder.Services.AddScoped<IReplenishmentService, ReplenishmentService>();
builder.Services.AddScoped<IReplenishmentDashboardService, ReplenishmentDashboardService>();
builder.Services.AddScoped<IReplacementSettingsService, ReplacementSettingsService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();
builder.Services.AddScoped<IJsonImportService, JsonImportService>();

//Repositories
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<IBatchRepository, BatchRepository>();
builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<IStockBalanceRepository, StockBalanceRepository>();
builder.Services.AddScoped<IDailyConsumptionRepository, DailyConsumptionRepository>();
builder.Services.AddScoped<IReplenishmentRuleRepository, ReplenishmentRuleRepository>();

//Abstractions 
builder.Services.AddScoped<IGridColumnService, GridColumnService>();
builder.Services.AddScoped<PdfStorageService>();
builder.Services.AddScoped<PdfValidationService>();
builder.Services.AddScoped<PdfParserService>();
builder.Services.AddScoped<LayoutState>();
builder.Services.AddScoped<ThemeState>();
builder.Services.AddSingleton<ReplenishmentDataState>();
builder.Services.AddScoped<ReportHtmlService>();

builder.Services.AddFluentUIComponents(options =>
{
    options.ValidateClassNames = false;
});

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

if (app.Environment.IsDevelopment())
{
    app.MapPost("/dev/import-stock", async (IJsonImportService importService) =>
    {
        var result = await importService.ImportStockAsync("storage/estoque.json");
        return Results.Ok(result);
    });
}

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
