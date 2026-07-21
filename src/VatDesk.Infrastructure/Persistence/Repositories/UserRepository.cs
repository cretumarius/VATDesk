using Microsoft.EntityFrameworkCore;
using VatDesk.Infrastructure.Persistence.Entities;

namespace VatDesk.Infrastructure.Persistence.Repositories;

public class UserRepository(VatDeskDbContext dbContext)
{
    public Task<UserEntity?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
}
