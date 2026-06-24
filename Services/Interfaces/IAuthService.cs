using RepyPharma.Models;

namespace RepyPharma.Services.Interfaces;

public interface IAuthService
{
    event Action? OnChange;
    bool IsInitialized { get; }
    bool IsAuthenticated { get; }
    AuthUserSession? CurrentUser { get; }

    Task InitializeAsync();
    Task<AuthLoginResult> LoginAsync(string username, string password, bool rememberMe);
    Task LogoutAsync();
}
