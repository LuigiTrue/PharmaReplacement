using Microsoft.AspNetCore.Identity;

namespace RepyPharma.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string Name { get; set; } = string.Empty;
    public string AvatarDataUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool PasswordChangeRequested { get; set; }
    public DateTime? PasswordChangeRequestedAt { get; set; }
}
