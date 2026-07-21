using VatDesk.Core.Models;

namespace VatDesk.Infrastructure.Persistence.Entities;

public class UserEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;

    // Not in architecture.md's original users schema (id, email, password_hash, role,
    // created_at) — added this session for the JWT "display name" claim and the header/
    // account-menu UI, which the design shows as a human name distinct from the email.
    // Additive, non-breaking; documented as a surfaced decision in this session's summary.
    public string DisplayName { get; set; } = null!;

    public UserRole Role { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
