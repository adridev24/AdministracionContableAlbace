using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BudgetControl.Api.Data
{
    // Scaffolding must never start Program/DbInitializer or connect to a configured database.
    public sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql().Options);
    }
}
