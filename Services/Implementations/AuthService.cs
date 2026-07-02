using System.Text.Json;
using Microsoft.JSInterop;
using RepyPharma.Models;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Implementatios;

public class AuthService : IAuthService
{
    private const string StorageKey = "repypharma.auth.user";
    private readonly string _usersFilePath;
    private readonly IJSRuntime _jsRuntime;
    private readonly IWebHostEnvironment _env;
    private string? _currentStorageName;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public AuthService(IWebHostEnvironment env, IJSRuntime jsRuntime)
    {
        _env = env;
        _jsRuntime = jsRuntime;
        _usersFilePath = Path.Combine(env.ContentRootPath, "storage", "users.json");
    }

    public event Action? OnChange;

    public bool IsInitialized { get; private set; }
    public AuthUserSession? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser is not null;
    public bool IsAdmin =>
        CurrentUser?.IsAdmin == true ||
        string.Equals(CurrentUser?.Email, "admin@repypharma.local", StringComparison.OrdinalIgnoreCase);

    public async Task InitializeAsync()
    {
        if (IsInitialized)
            return;

        CurrentUser = await ReadStoredSessionAsync("sessionStorage");
        if (CurrentUser is not null)
        {
            _currentStorageName = "sessionStorage";
        }
        else
        {
            CurrentUser = await ReadStoredSessionAsync("localStorage");
            if (CurrentUser is not null)
                _currentStorageName = "localStorage";
        }

        IsInitialized = true;
        NotifyStateChanged();
    }

    public async Task<AuthLoginResult> LoginAsync(string username, string password, bool rememberMe)
    {
        await EnsureUsersFileAsync();

        var normalizedUsername = username.Trim();
        var users = await ReadUsersAsync();
        var user = users.FirstOrDefault(u =>
            u.Active
            && IsUserLoginMatch(u, normalizedUsername)
            && u.Password == password);

        if (user is null)
            return AuthLoginResult.Failure("Usuário ou senha inválidos.");

        CurrentUser = new AuthUserSession
        {
            Username = user.Username,
            Email = user.Email,
            Name = string.IsNullOrWhiteSpace(user.Name) ? user.Email : user.Name,
            IsAdmin = user.IsAdmin,
            AvatarDataUrl = user.AvatarDataUrl,
            CreatedAt = DateTime.UtcNow
        };

        await ClearBrowserStorageAsync();
        _currentStorageName = rememberMe ? "localStorage" : "sessionStorage";
        await WriteStoredSessionAsync(_currentStorageName, CurrentUser);

        IsInitialized = true;
        NotifyStateChanged();
        return AuthLoginResult.Success(CurrentUser);
    }

    public async Task UpdateCurrentUserProfileAsync(string name, string avatarDataUrl)
    {
        if (CurrentUser is null)
            return;

        CurrentUser.Name = name;
        CurrentUser.AvatarDataUrl = avatarDataUrl;

        await WriteStoredSessionAsync(_currentStorageName ?? "sessionStorage", CurrentUser);
        NotifyStateChanged();
    }

    public async Task LogoutAsync()
    {
        CurrentUser = null;
        _currentStorageName = null;
        IsInitialized = true;
        await ClearBrowserStorageAsync();
        NotifyStateChanged();
    }

    private static bool IsUserLoginMatch(AppUser user, string username)
    {
        return string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase)
            || string.Equals(user.Email, username, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<List<AppUser>> ReadUsersAsync()
    {
        await EnsureUsersFileAsync();

        var json = await File.ReadAllTextAsync(_usersFilePath);
        if (string.IsNullOrWhiteSpace(json))
            return new List<AppUser>();

        return JsonSerializer.Deserialize<List<AppUser>>(json, _jsonOptions) ?? new List<AppUser>();
    }

    private async Task EnsureUsersFileAsync()
    {
        var directory = Path.GetDirectoryName(_usersFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        if (File.Exists(_usersFilePath))
            return;

        var adminPassword = Environment.GetEnvironmentVariable("REPY_ADMIN_PASSWORD");
        if (string.IsNullOrWhiteSpace(adminPassword) && !_env.IsDevelopment())
            throw new InvalidOperationException("Configure a variável de ambiente REPY_ADMIN_PASSWORD para criar o administrador inicial.");

        var users = new List<AppUser>
        {
            new()
            {
                Username = Environment.GetEnvironmentVariable("REPY_ADMIN_USERNAME") ?? "admin",
                Email = Environment.GetEnvironmentVariable("REPY_ADMIN_EMAIL") ?? "admin@repypharma.local",
                Name = Environment.GetEnvironmentVariable("REPY_ADMIN_NAME") ?? "Administrador",
                Password = adminPassword ?? "admin123",
                Active = true,
                IsAdmin = true
            }
        };

        var json = JsonSerializer.Serialize(users, _jsonOptions);
        await File.WriteAllTextAsync(_usersFilePath, json);
    }

    private async Task<AuthUserSession?> ReadStoredSessionAsync(string storageName)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("repyPharmaAuthStorage.get", storageName, StorageKey);
            return string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<AuthUserSession>(json, _jsonOptions);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (JSException)
        {
            return null;
        }
    }

    private async Task WriteStoredSessionAsync(string storageName, AuthUserSession session)
    {
        var json = JsonSerializer.Serialize(session, _jsonOptions);
        await _jsRuntime.InvokeVoidAsync("repyPharmaAuthStorage.set", storageName, StorageKey, json);
    }

    private async Task ClearBrowserStorageAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("repyPharmaAuthStorage.clear", StorageKey);
        }
        catch (InvalidOperationException)
        {
        }
        catch (JSException)
        {
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
