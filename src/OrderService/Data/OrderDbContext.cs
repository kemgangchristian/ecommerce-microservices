using Microsoft.EntityFrameworkCore;
using OrderService.Models;

namespace OrderService.Data;

/// <summary>Contexte EF Core d'OrderService, backé par PostgreSQL (Npgsql).</summary>
public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.CustomerEmail).IsRequired();

            // Stocke l'enum comme texte lisible ("Pending", "Confirmed", ...)
            // plutôt que comme entier : plus lisible directement en base et
            // plus robuste si l'ordre des valeurs de l'enum change un jour.
            entity.Property(o => o.Status).HasConversion<string>();

            // Relation 1-N Order -> OrderItem, suppression en cascade :
            // supprimer une commande supprime automatiquement ses lignes.
            entity.HasMany(o => o.Items)
                  .WithOne()
                  .HasForeignKey(i => i.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Total est calculé en mémoire (voir Order.Total) : on indique
            // explicitement à EF Core de ne pas essayer de le mapper à une
            // colonne.
            entity.Ignore(o => o.Total);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(i => i.Id);

            // decimal(18,2) : type monétaire standard côté PostgreSQL,
            // évite les erreurs d'arrondi propres aux types flottants
            // (float/double) quand on manipule de l'argent.
            entity.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
        });
    }
}