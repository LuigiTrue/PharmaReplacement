using RepyPharma.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using ApexCharts;
using RepyPharma.Services.Implementatios;
using RepyPharma.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();
builder.Services.AddApexCharts();


//Services
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductStockService, ProductStockService>();
builder.Services.AddScoped<IStockJsonService, StockJsonService>();
builder.Services.AddScoped<IMinimumStockService, MinimumStockService>();
builder.Services.AddScoped<IReplenishmentService, ReplenishmentService>();

//Abstractions 
builder.Services.AddScoped<IGridColumnService, GridColumnService>();
builder.Services.AddScoped<PdfStorageService>();
builder.Services.AddScoped<PdfValidationService>();
builder.Services.AddScoped<PdfParserService>();
builder.Services.AddScoped<LayoutState>();
builder.Services.AddScoped<ThemeState>();
builder.Services.AddScoped<ReportHtmlService>();


var app = builder.Build();

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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
