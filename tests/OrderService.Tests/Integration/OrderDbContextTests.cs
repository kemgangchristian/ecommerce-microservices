using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderService.Data;
using OrderService.Models;
using Testcontainers.PostgreSql;
using Xunit;

namespace OrderService.Tests.Integration;

/// <summary>
/// Teste OrderDbContext contre une vraie instance PostgreSQL, démarrée dans
/// un conteneur Docker éphémère. Contrairement aux tests unitaires (EF Core
/// In-Memory), ceci applique réellement les migrations et vérifie le
/// comportement SQL effectif : cascade delete, précision decimal(18,2),
/// contraintes.
/// </summary>
public class OrderDbContextTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("OrderServiceTestsDb")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        // Applique les vraies migrations EF Core générées à l'étape 14,
        // exactement comme au démarrage de l'application.
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgresContainer.DisposeAsync();

    /// <summary>
    /// Crée un nouveau OrderDbContext à chaque appel. Utiliser une instance
    /// fraîche pour relire des données évite de lire depuis le cache de
    /// suivi d'EF Core et force une vraie requête SQL contre la base.
    /// </summary>
    private OrderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        return new OrderDbContext(options);
    }

    [Fact]
    public async Task AddOrder_WithItems_PersistsOrderAndItemsTogether()
    {
        var order = new Order
        {
            CustomerEmail = "client@example.com",
            Items = { new OrderItem { ProductId = "product-1", ProductName = "Clavier", Quantity = 2, UnitPrice = 79.99m } }
        };

        await using (var writeContext = CreateDbContext())
        {
            writeContext.Orders.Add(order);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext();
        var reloaded = await readContext.Orders.Include(o => o.Items).FirstAsync(o => o.Id == order.Id);

        Assert.Equal("client@example.com", reloaded.CustomerEmail);
        Assert.Single(reloaded.Items);
        Assert.Equal(79.99m, reloaded.Items[0].UnitPrice);
    }

    [Fact]
    public async Task DeleteOrder_CascadeDeletesItems()
    {
        var order = new Order
        {
            CustomerEmail = "client@example.com",
            Items = { new OrderItem { ProductId = "product-1", ProductName = "Clavier", Quantity = 1, UnitPrice = 79.99m } }
        };

        await using (var writeContext = CreateDbContext())
        {
            writeContext.Orders.Add(order);
            await writeContext.SaveChangesAsync();
        }

        await using (var deleteContext = CreateDbContext())
        {
            var toDelete = await deleteContext.Orders.FirstAsync(o => o.Id == order.Id);
            deleteContext.Orders.Remove(toDelete);
            await deleteContext.SaveChangesAsync();
        }

        // Vérifie que OnDelete(DeleteBehavior.Cascade) (étape 12) est bien
        // traduit en contrainte FK réelle par la migration, et appliqué par
        // PostgreSQL lui-même — pas juste simulé par EF Core en mémoire.
        await using var readContext = CreateDbContext();
        var remainingItems = await readContext.OrderItems.Where(i => i.OrderId == order.Id).ToListAsync();
        Assert.Empty(remainingItems);
    }

    [Fact]
    public async Task Status_IsPersistedAsReadableString()
    {
        var order = new Order { CustomerEmail = "client@example.com", Status = OrderStatus.Confirmed };

        await using (var writeContext = CreateDbContext())
        {
            writeContext.Orders.Add(order);
            await writeContext.SaveChangesAsync();
        }

        // Requête SQL brute (hors EF Core) pour vérifier ce qui est
        // réellement stocké : "Confirmed" en texte lisible, pas un entier —
        // comme configuré via HasConversion<string>() à l'étape 12.
        await using var connection = new NpgsqlConnection(_postgresContainer.GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT \"Status\" FROM \"Orders\" WHERE \"Id\" = @id";
        command.Parameters.AddWithValue("id", order.Id);
        var status = (string)(await command.ExecuteScalarAsync())!;

        Assert.Equal("Confirmed", status);
    }
}