using System.ComponentModel.DataAnnotations.Schema;

namespace TheIgKnightWorker.Infrastructure.DbModels;

[Table("User")]
internal class DbUser
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string PasswordHash { get; set; } = string.Empty!;
    public string PasswordSalt { get; set; } = string.Empty!;
    public string Role { get; set; } = "User";
    public string Features { get; set; } = string.Empty;
    public string Privileges { get; set; } = string.Empty;
    public bool IsExternal { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public bool IsPhoneConfirmed { get; set; }
    public bool IsActive { get; set; }
    public string UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
