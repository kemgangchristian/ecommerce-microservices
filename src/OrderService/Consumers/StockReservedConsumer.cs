using ECommerce.Contracts.Events;
using MassTransit;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Consumers;

/// <summary>
/// Consomme StockReserved, publié par ProductService quand le stock a été
/// réservé avec succès pour tous les articles. Confirme la commande.
/// </summary>
public class StockReservedConsumer : IConsumer<StockReserved>
{
    private readonly OrderDbContext _db;
    private readonly ILogger<StockReservedConsumer> _logger;

    public StockReservedConsumer(OrderDbContext db, ILogger<StockReservedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockReserved> context)
    {
        var order = await _db.Orders.FindAsync(context.Message.OrderId);
        if (order is null)
        {
            _logger.LogWarning("Commande {OrderId} introuvable lors de la confirmation du stock", context.Message.OrderId);
            return;
        }

        order.Status = OrderStatus.Confirmed;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Commande {OrderId} confirmee (stock reserve)", order.Id);
    }
}