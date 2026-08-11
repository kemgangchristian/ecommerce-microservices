namespace OrderService.Models;

/// <summary>
/// Représente une commande passée par un client, persistée dans PostgreSQL
/// via Entity Framework Core. Le choix d'une base relationnelle se justifie
/// ici par la nécessité de garantir l'intégrité référentielle avec ses
/// <see cref="OrderItem"/> et l'atomicité des écritures : une commande et
/// ses lignes sont enregistrées ensemble, dans une seule transaction.
/// </summary>
public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CustomerEmail { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public List<OrderItem> Items { get; set; } = new();

    /// <summary>
    /// Calculé à la volée à partir des lignes, jamais persisté en base
    /// (voir <see cref="Data.OrderDbContext.OnModelCreating"/> où cette
    /// propriété est explicitement ignorée par EF Core).
    /// </summary>
    public decimal Total => Items.Sum(i => i.UnitPrice * i.Quantity);

    /// <summary>Renseigné uniquement si Status == Cancelled, explique pourquoi.</summary>
    public string? CancellationReason { get; set; }
}

/// <summary>Cycle de vie d'une commande.</summary>
public enum OrderStatus
{
    Pending,
    Confirmed,
    Cancelled
}