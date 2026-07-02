namespace RepyPharma.Models;

public class UserProfileSettings
{
    public string Username { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string AvatarDataUrl { get; set; } = "";
    public bool IsAdmin { get; set; }
    public bool PasswordChangeRequested { get; set; }
}

public class UserPasswordChangeRequest
{
    public string Username { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime? RequestedAt { get; set; }
}
