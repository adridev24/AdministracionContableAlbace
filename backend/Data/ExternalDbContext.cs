using Microsoft.EntityFrameworkCore;
using BudgetControl.Api.Models;

namespace BudgetControl.Api.Data
{
    public class ExternalDbContext : DbContext
    {
        public ExternalDbContext(DbContextOptions<ExternalDbContext> options) : base(options) { }

        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<Obra> Obras { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Client>(entity =>
            {
                entity.ToTable("Clientes");
                entity.HasKey(e => e.IdCliente);
                entity.Property(e => e.IdCliente).HasColumnName("idCliente");
                entity.Property(e => e.NombreCliente).HasColumnName("nombrecliente");
                entity.Property(e => e.Domicilio).HasColumnName("domicilio");
                entity.Property(e => e.Telefonoc).HasColumnName("telefonoc");
            });

            modelBuilder.Entity<Obra>(entity =>
            {
                entity.ToTable("Obras");
                entity.HasKey(e => e.IdObra);
                entity.Property(e => e.IdObra).HasColumnName("idobra");
                entity.Property(e => e.NombreObra).HasColumnName("nombreobra");
                entity.Property(e => e.Descripcion).HasColumnName("descripcion");
                entity.Property(e => e.Finalizada).HasColumnName("finalizada");
                entity.Property(e => e.ClienteId).HasColumnName("clienteId");
                entity.HasOne(e => e.Cliente).WithMany(c => c.Obras).HasForeignKey(e => e.ClienteId);
            });
        }
    }
}
