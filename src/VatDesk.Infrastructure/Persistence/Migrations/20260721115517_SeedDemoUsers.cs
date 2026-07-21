using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Migrations;
using VatDesk.Infrastructure.Persistence.Entities;

#nullable disable

namespace VatDesk.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedDemoUsers : Migration
    {
        private static readonly Guid AdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid ViewerId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Hashed here (not stored as a literal) so no plaintext or precomputed hash ever
            // sits in source control — PasswordHasher salts each call, so this migration
            // produces a fresh hash the one time it actually runs against a database.
            var hasher = new PasswordHasher<UserEntity>();
            var adminHash = hasher.HashPassword(null, "Admin123!");
            var viewerHash = hasher.HashPassword(null, "Viewer123!");

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "email", "password_hash", "display_name", "role", "created_at" },
                values: new object[,]
                {
                    { AdminId, "admin@demo.hu", adminHash, "Nagy Katalin", "Admin", DateTimeOffset.UtcNow },
                    { ViewerId, "viewer@demo.hu", viewerHash, "Kovács Péter", "Viewer", DateTimeOffset.UtcNow },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "users", keyColumn: "id", keyValue: AdminId);
            migrationBuilder.DeleteData(table: "users", keyColumn: "id", keyValue: ViewerId);
        }
    }
}
