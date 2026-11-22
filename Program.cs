using RepyPharma.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RepyPharma.Data;
using RepyPharma.Domain.Entities;
using RepyPharma.Infrastructure.Identity;
using RepyPharma.Services.Implementatios;
using RepyPharma.Services.Import;
using RepyPharma.Infrastructure.Json;
using RepyPharma.Services.Import.Interfaces;
using RepyPharma.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    options.UseNpgsql(DatabaseConnectionString.GetRequired(builder.Configuration));
});
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;

    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;

    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login";
    options.LogoutPath = "/auth/logout";
    options.AccessDeniedPath = "/Home";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});
builder.Services.AddAuthorization();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Application services
builder.Services.AddScoped<IDanfeService, DanfeService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IQuotationService, QuotationService>();
builder.Services.AddScoped<IStockJsonService, StockJsonService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();
builder.Services.AddScoped<IJsonImportService, JsonImportService>();

// Stock import and UI state
builder.Services.AddScoped<PdfStorageService>();
builder.Services.AddScoped<PdfParserService>();
builder.Services.AddScoped<ThemeState>();
builder.Services.AddSingleton<StockDataState>();

builder.Services.AddFluentUIComponents(options =>
{
    options.ValidateClassNames = false;
});

var app = builder.Build();

app.UseForwardedHeaders();

using (var scope = app.Services.CreateScope())
{
    await IdentitySeedData.SeedAsync(scope.ServiceProvider, app.Configuration);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

if (app.Environment.IsDevelopment())
{
    app.MapPost("/dev/import-stock", async (IJsonImportService importService) =>
    {
        var result = await importService.ImportStockAsync("storage/estoque.json");
        return Results.Ok(result);
    });
}

app.MapPost("/auth/login", async (
    HttpContext httpContext,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var username = form["username"].ToString().Trim();
    var password = form["password"].ToString();
    var rememberMe = string.Equals(form["rememberMe"].ToString(), "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(form["rememberMe"].ToString(), "on", StringComparison.OrdinalIgnoreCase);
    var returnUrl = GetSafeReturnUrl(form["returnUrl"].ToString());

    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        return Results.Redirect(GetLoginUrl("invalid", returnUrl));

    var user = await userManager.FindByNameAsync(username)
        ?? await userManager.FindByEmailAsync(username);

    if (user is null)
        return Results.Redirect(GetLoginUrl("invalid", returnUrl));

    if (!user.IsActive)
        return Results.Redirect(GetLoginUrl("inactive", returnUrl));

    var result = await signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: true);

    if (result.IsLockedOut)
        return Results.Redirect(GetLoginUrl("locked", returnUrl));

    if (!result.Succeeded)
        return Results.Redirect(GetLoginUrl("invalid", returnUrl));

    return Results.Redirect(returnUrl);
}).DisableAntiforgery();

app.MapPost("/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/Login");
}).DisableAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static string GetSafeReturnUrl(string? returnUrl)
{
    if (string.IsNullOrWhiteSpace(returnUrl))
        return "/Home";

    if (Uri.TryCreate(returnUrl, UriKind.Absolute, out _))
        return "/Home";

    if (returnUrl.StartsWith("//", StringComparison.Ordinal))
        return "/Home";

    return returnUrl.StartsWith("/", StringComparison.Ordinal)
        ? returnUrl
        : $"/{returnUrl.TrimStart('/')}";
}

static string GetLoginUrl(string error, string returnUrl)
{
    return $"/Login?error={Uri.EscapeDataString(error)}&returnUrl={Uri.EscapeDataString(returnUrl)}";
}
