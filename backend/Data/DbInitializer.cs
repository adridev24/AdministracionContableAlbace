using Microsoft.Extensions.Configuration;
using BudgetControl.Api.Models;

namespace BudgetControl.Api.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context, IConfiguration configuration)
        {
            context.Database.EnsureCreated();

            if (!context.Roles.Any())
            {
                context.Roles.AddRange(
                    new Role { Name = "Admin" },
                    new Role { Name = "User" }
                );
            }

            if (!context.Users.Any())
            {
                var defaultPassword = configuration.GetValue<string>("DefaultAdminPassword") ?? "Admin123!";
                var admin = new User
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
                    FullName = "Administrador",
                    Email = "admin@example.com"
                };

                context.Users.Add(admin);
                context.SaveChanges();

                var adminRole = context.Roles.Single(r => r.Name == "Admin");
                context.UserRoles.Add(new UserRole
                {
                    UserId = admin.Id,
                    RoleId = adminRole.Id
                });
            }

            context.SaveChanges();
        }
    }
}
