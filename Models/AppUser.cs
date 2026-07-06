namespace RepyPharma.Models;

public class AuthUserSession
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public string AvatarDataUrl { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AuthLoginResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public AuthUserSession? User { get; set; }

    public static AuthLoginResult Success(AuthUserSession user) => new()
    {
        Succeeded = true,
        User = user
    };

    public static AuthLoginResult Failure(string message) => new()
    {
        Succeeded = false,
        ErrorMessage = message
    };
}
