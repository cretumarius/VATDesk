using VatDesk.Core.Models;

namespace VatDesk.Infrastructure.Persistence.Entities;

public class UserEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public UserRole Role { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
