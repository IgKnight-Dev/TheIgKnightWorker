namespace TheIgKnightWorker.Domain.Entities;

public class User
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string PasswordHash { get; set; } = string.Empty!;
    public string PasswordSalt { get; set; } = string.Empty!;
    public string Role { get; set; } = "User";
    public List<string> Features { get; set; } = new List<string>();
    public List<string> Privileges { get; set; } = new List<string>();
    public bool External { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public bool IsPhoneConfirmed { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
