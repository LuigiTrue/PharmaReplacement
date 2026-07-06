using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RepyPharma.Domain.Entities;
using RepyPharma.Infrastructure.Identity;
using RepyPharma.Models;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Implementatios;

public class UserSettingsService : IUserSettingsService
{
    private readonly IAuthService _authService;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserSettingsService(
        IAuthService authService,
        UserManager<ApplicationUser> userManager)
    {
        _authService = authService;
        _userManager = userManager;
    }

    public event Action? OnChange;

    public async Task<UserProfileSettings?> GetCurrentProfileAsync()
    {
        var user = await GetCurrentApplicationUserAsync();
        if (user is null)
            return null;

        return new UserProfileSettings
        {
            Username = user.UserName ?? string.Empty,
            Name = user.Name,
            Email = user.Email ?? string.Empty,
            AvatarDataUrl = user.AvatarDataUrl,
            IsAdmin = await _userManager.IsInRoleAsync(user, AuthRoles.Admin),
            PasswordChangeRequested = user.PasswordChangeRequested
        };
    }

    public async Task UpdateCurrentProfileAsync(string name, string avatarDataUrl)
    {
        var user = await GetCurrentApplicationUserAsync()
            ?? throw new InvalidOperationException("Usuário não autenticado.");

        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Informe o nome do usuário.");

        user.Name = name.Trim();
        user.AvatarDataUrl = avatarDataUrl;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(GetIdentityErrorMessage("Falha ao atualizar o perfil", result));

        await _authService.UpdateCurrentUserProfileAsync(user.Name, user.AvatarDataUrl);
        NotifyStateChanged();
    }

    public async Task RequestPasswordChangeAsync()
    {
        var user = await GetCurrentApplicationUserAsync()
            ?? throw new InvalidOperationException("Usuário não autenticado.");

        user.PasswordChangeRequested = true;
        user.PasswordChangeRequestedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(GetIdentityErrorMessage("Falha ao registrar solicitação de senha", result));

        NotifyStateChanged();
    }

    public async Task<int> GetPendingPasswordRequestCountAsync()
    {
        return await _userManager.Users.CountAsync(user => user.PasswordChangeRequested);
    }

    public async Task<List<UserPasswordChangeRequest>> GetPendingPasswordRequestsAsync()
    {
        return await _userManager.Users
            .Where(user => user.PasswordChangeRequested)
            .OrderBy(user => user.PasswordChangeRequestedAt ?? DateTime.MaxValue)
            .Select(user => new UserPasswordChangeRequest
            {
                Username = user.UserName ?? string.Empty,
                Name = string.IsNullOrWhiteSpace(user.Name) ? user.UserName ?? string.Empty : user.Name,
                Email = user.Email ?? string.Empty,
                RequestedAt = user.PasswordChangeRequestedAt
            })
            .ToListAsync();
    }

    public async Task ChangeUserPasswordAsync(string username, string newPassword)
    {
        if (!_authService.IsAdmin)
            throw new InvalidOperationException("Somente administradores podem alterar senhas.");

        if (string.IsNullOrWhiteSpace(username))
            throw new InvalidOperationException("Usuário inválido.");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            throw new InvalidOperationException("Informe uma senha com pelo menos 8 caracteres.");

        var user = await _userManager.FindByNameAsync(username)
            ?? await _userManager.FindByEmailAsync(username);

        if (user is null)
            throw new InvalidOperationException("Usuário não encontrado.");

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(GetIdentityErrorMessage("Falha ao alterar a senha", result));

        user.PasswordChangeRequested = false;
        user.PasswordChangeRequestedAt = null;

        result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(GetIdentityErrorMessage("Falha ao atualizar solicitação de senha", result));

        NotifyStateChanged();
    }

    private async Task<ApplicationUser?> GetCurrentApplicationUserAsync()
    {
        var currentUser = _authService.CurrentUser;
        if (currentUser is null)
            return null;

        return await _userManager.FindByNameAsync(currentUser.Username)
            ?? await _userManager.FindByEmailAsync(currentUser.Email);
    }

    private static string GetIdentityErrorMessage(string message, IdentityResult result)
    {
        return $"{message}: {string.Join("; ", result.Errors.Select(error => error.Description))}";
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
