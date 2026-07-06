using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using RepyPharma.Domain.Entities;
using RepyPharma.Infrastructure.Identity;
using RepyPharma.Models;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Implementatios;

public class AuthService : IAuthService, IDisposable
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthService(
        AuthenticationStateProvider authenticationStateProvider,
        UserManager<ApplicationUser> userManager)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _userManager = userManager;
        _authenticationStateProvider.AuthenticationStateChanged += HandleAuthenticationStateChanged;
    }

    public event Action? OnChange;

    public bool IsInitialized { get; private set; }
    public AuthUserSession? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser is not null;
    public bool IsAdmin => IsInRole(AuthRoles.Admin);

    public async Task InitializeAsync()
    {
        if (IsInitialized)
            return;

        var authenticationState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        await SetCurrentUserAsync(authenticationState);
        IsInitialized = true;
        NotifyStateChanged();
    }

    public async Task<AuthLoginResult> LoginAsync(string username, string password, bool rememberMe)
    {
        var user = await _userManager.FindByNameAsync(username.Trim())
            ?? await _userManager.FindByEmailAsync(username.Trim());

        if (user is null || !user.IsActive)
            return AuthLoginResult.Failure("Usuário ou senha inválidos.");

        var passwordIsValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordIsValid)
            return AuthLoginResult.Failure("Usuário ou senha inválidos.");

        var session = await CreateSessionAsync(user);
        return AuthLoginResult.Success(session);
    }

    public Task UpdateCurrentUserProfileAsync(string name, string avatarDataUrl)
    {
        if (CurrentUser is null)
            return Task.CompletedTask;

        CurrentUser.Name = name;
        CurrentUser.AvatarDataUrl = avatarDataUrl;
        NotifyStateChanged();
        return Task.CompletedTask;
    }

    public Task LogoutAsync()
    {
        CurrentUser = null;
        IsInitialized = true;
        NotifyStateChanged();
        return Task.CompletedTask;
    }

    public bool IsInRole(string role)
    {
        return CurrentUser?.Roles.Any(currentRole =>
            string.Equals(currentRole, role, StringComparison.OrdinalIgnoreCase)) == true;
    }

    private void HandleAuthenticationStateChanged(Task<AuthenticationState> task)
    {
        _ = RefreshAuthenticationStateAsync(task);
    }

    private async Task RefreshAuthenticationStateAsync(Task<AuthenticationState> task)
    {
        var authenticationState = await task;
        await SetCurrentUserAsync(authenticationState);
        IsInitialized = true;
        NotifyStateChanged();
    }

    private async Task SetCurrentUserAsync(AuthenticationState authenticationState)
    {
        var principal = authenticationState.User;
        if (principal.Identity?.IsAuthenticated != true)
        {
            CurrentUser = null;
            return;
        }

        var user = await _userManager.GetUserAsync(principal);
        CurrentUser = user is null || !user.IsActive
            ? null
            : await CreateSessionAsync(user);
    }

    private async Task<AuthUserSession> CreateSessionAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return new AuthUserSession
        {
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Name = string.IsNullOrWhiteSpace(user.Name) ? user.UserName ?? string.Empty : user.Name,
            IsAdmin = roles.Contains(AuthRoles.Admin),
            AvatarDataUrl = user.AvatarDataUrl,
            Roles = roles.ToList(),
            CreatedAt = DateTime.UtcNow
        };
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    public void Dispose()
    {
        _authenticationStateProvider.AuthenticationStateChanged -= HandleAuthenticationStateChanged;
    }
}
