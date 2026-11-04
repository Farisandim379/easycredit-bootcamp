using EasyCreditApi.Domain.Entities;
using EasyCreditApi.Infrastructure.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EasyCreditApi.Infrastructure;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetService<ILogger<AppDbContext>>();

            // Pastikan tabel PostgreSQL terbentuk
            await context.Database.EnsureCreatedAsync();

            // Seed user ADMIN: admin@easycredit.test / admin123
            if (!await context.Users.AnyAsync(u => u.Email == "admin@easycredit.test"))
            {
                context.Users.Add(new User
                {
                    Name = "Administrator",
                    Email = "admin@easycredit.test",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    Role = "ADMIN",
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Seed user STAFF: staff@easycredit.test / staff123
            if (!await context.Users.AnyAsync(u => u.Email == "staff@easycredit.test"))
            {
                context.Users.Add(new User
                {
                    Name = "Staff User",
                    Email = "staff@easycredit.test",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("staff123"),
                    Role = "STAFF",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
            logger?.LogInformation("DatabaseSeeder: Demo users seeded successfully.");
        }
        catch (Exception ex)
        {
            // Tidak menghentikan aplikasi jika database offline saat build / unit test
            Console.WriteLine($"[DatabaseSeeder Warning] Could not seed database: {ex.Message}");
        }
    }
}
