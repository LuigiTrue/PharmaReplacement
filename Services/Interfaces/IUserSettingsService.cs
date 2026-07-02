using RepyPharma.Models;

namespace RepyPharma.Services.Interfaces;

public interface IUserSettingsService
{
    event Action? OnChange;

    Task<UserProfileSettings?> GetCurrentProfileAsync();
    Task UpdateCurrentProfileAsync(string name, string avatarDataUrl);
    Task RequestPasswordChangeAsync();
    Task<int> GetPendingPasswordRequestCountAsync();
    Task<List<UserPasswordChangeRequest>> GetPendingPasswordRequestsAsync();
    Task ChangeUserPasswordAsync(string username, string newPassword);
}
