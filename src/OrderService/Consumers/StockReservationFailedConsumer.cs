using ECommerce.Contracts.Events;
using MassTransit;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Consumers;

/// <summary>
/// Consomme StockReservationFailed, publié par ProductService quand le
/// stock n'a pas pu être réservé pour au moins un article. Annule la
/// commande et enregistre la raison.
/// </summary>
public class StockReservationFailedConsumer : IConsumer<StockReservationFailed>
{
    private readonly OrderDbContext _db;
    private readonly ILogger<StockReservationFailedConsumer> _logger;

    public StockReservationFailedConsumer(OrderDbContext db, ILogger<StockReservationFailedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockReservationFailed> context)
    {
        var message = context.Message;
        var order = await _db.Orders.FindAsync(message.OrderId);
        if (order is null)
        {
            _logger.LogWarning("Commande {OrderId} introuvable lors de l'annulation", message.OrderId);
            return;
        }

        order.Status = OrderStatus.Cancelled;
        order.CancellationReason = message.Reason;
        await _db.SaveChangesAsync();

        _logger.LogWarning("Commande {OrderId} annulee : {Reason}", order.Id, message.Reason);
    }
}