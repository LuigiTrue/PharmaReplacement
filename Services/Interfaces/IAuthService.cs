using RepyPharma.Models;

namespace RepyPharma.Services.Interfaces;

public interface IAuthService
{
    event Action? OnChange;
    bool IsInitialized { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    AuthUserSession? CurrentUser { get; }
    bool IsInRole(string role);

    Task InitializeAsync();
    Task<AuthLoginResult> LoginAsync(string username, string password, bool rememberMe);
    Task UpdateCurrentUserProfileAsync(string name, string avatarDataUrl);
    Task LogoutAsync();
}
