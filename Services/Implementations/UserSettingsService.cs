using System.Text.Json;
using RepyPharma.Models;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Implementatios;

public class UserSettingsService : IUserSettingsService
{
    private readonly string _usersFilePath;
    private readonly IAuthService _authService;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public UserSettingsService(IWebHostEnvironment env, IAuthService authService)
    {
        _authService = authService;
        _usersFilePath = Path.Combine(env.ContentRootPath, "storage", "users.json");
    }

    public event Action? OnChange;

    public async Task<UserProfileSettings?> GetCurrentProfileAsync()
    {
        var currentUser = _authService.CurrentUser;
        if (currentUser is null)
            return null;

        var user = await FindUserAsync(currentUser.Username);
        if (user is null)
            return null;

        return new UserProfileSettings
        {
            Username = user.Username,
            Name = user.Name,
            Email = user.Email,
            AvatarDataUrl = user.AvatarDataUrl,
            IsAdmin = user.IsAdmin,
            PasswordChangeRequested = user.PasswordChangeRequested
        };
    }

    public async Task UpdateCurrentProfileAsync(string name, string avatarDataUrl)
    {
        var currentUser = _authService.CurrentUser;
        if (currentUser is null)
            throw new InvalidOperationException("Usuário não autenticado.");

        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Informe o nome do usuário.");

        var users = await ReadUsersAsync();
        var user = users.FirstOrDefault(u => IsSameUser(u.Username, currentUser.Username));
        if (user is null)
            throw new InvalidOperationException("Usuário não encontrado.");

        user.Name = name.Trim();
        user.AvatarDataUrl = avatarDataUrl;

        await WriteUsersAsync(users);
        await _authService.UpdateCurrentUserProfileAsync(user.Name, user.AvatarDataUrl);
        NotifyStateChanged();
    }

    public async Task RequestPasswordChangeAsync()
    {
        var currentUser = _authService.CurrentUser;
        if (currentUser is null)
            throw new InvalidOperationException("Usuário não autenticado.");

        var users = await ReadUsersAsync();
        var user = users.FirstOrDefault(u => IsSameUser(u.Username, currentUser.Username));
        if (user is null)
            throw new InvalidOperationException("Usuário não encontrado.");

        user.PasswordChangeRequested = true;
        user.PasswordChangeRequestedAt = DateTime.UtcNow;

        await WriteUsersAsync(users);
        NotifyStateChanged();
    }

    public async Task<int> GetPendingPasswordRequestCountAsync()
    {
        var users = await ReadUsersAsync();
        return users.Count(u => u.PasswordChangeRequested);
    }

    public async Task<List<UserPasswordChangeRequest>> GetPendingPasswordRequestsAsync()
    {
        var users = await ReadUsersAsync();
        return users
            .Where(u => u.PasswordChangeRequested)
            .OrderBy(u => u.PasswordChangeRequestedAt ?? DateTime.MaxValue)
            .Select(u => new UserPasswordChangeRequest
            {
                Username = u.Username,
                Name = string.IsNullOrWhiteSpace(u.Name) ? u.Username : u.Name,
                Email = u.Email,
                RequestedAt = u.PasswordChangeRequestedAt
            })
            .ToList();
    }

    public async Task ChangeUserPasswordAsync(string username, string newPassword)
    {
        if (!_authService.IsAdmin)
            throw new InvalidOperationException("Somente administradores podem alterar senhas.");

        if (string.IsNullOrWhiteSpace(username))
            throw new InvalidOperationException("Usuário inválido.");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            throw new InvalidOperationException("Informe uma senha com pelo menos 6 caracteres.");

        var users = await ReadUsersAsync();
        var user = users.FirstOrDefault(u => IsSameUser(u.Username, username));
        if (user is null)
            throw new InvalidOperationException("Usuário não encontrado.");

        user.Password = newPassword;
        user.PasswordChangeRequested = false;
        user.PasswordChangeRequestedAt = null;

        await WriteUsersAsync(users);
        NotifyStateChanged();
    }

    private async Task<AppUser?> FindUserAsync(string username)
    {
        var users = await ReadUsersAsync();
        return users.FirstOrDefault(u => IsSameUser(u.Username, username));
    }

    private async Task<List<AppUser>> ReadUsersAsync()
    {
        if (!File.Exists(_usersFilePath))
            return new List<AppUser>();

        var json = await File.ReadAllTextAsync(_usersFilePath);
        if (string.IsNullOrWhiteSpace(json))
            return new List<AppUser>();

        return JsonSerializer.Deserialize<List<AppUser>>(json, _jsonOptions) ?? new List<AppUser>();
    }

    private async Task WriteUsersAsync(List<AppUser> users)
    {
        var json = JsonSerializer.Serialize(users, _jsonOptions);
        await File.WriteAllTextAsync(_usersFilePath, json);
    }

    private static bool IsSameUser(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private void NotifyStateChanged() => OnChange?.Invoke();
}
