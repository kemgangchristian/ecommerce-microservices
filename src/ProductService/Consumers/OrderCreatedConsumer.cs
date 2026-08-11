using ECommerce.Contracts.Events;
using MassTransit;
using ProductService.Data;

namespace ProductService.Consumers;

/// <summary>
/// Consommateur MassTransit qui réagit à l'événement <see cref="OrderCreated"/>
/// et tente de réserver (décrémenter) le stock de tous les articles de la
/// commande. Si un seul article échoue, les réservations déjà effectuées sur
/// les autres articles de la MÊME commande sont annulées (compensation),
/// pour ne jamais laisser une réservation partielle en base. Le résultat
/// final est renvoyé à OrderService via <see cref="StockReserved"/> ou
/// <see cref="StockReservationFailed"/>, qui met à jour le statut de la
/// commande en conséquence.
/// </summary>
public class OrderCreatedConsumer : IConsumer<OrderCreated>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(IProductRepository repository, ILogger<OrderCreatedConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var orderCreated = context.Message;

        _logger.LogInformation(
            "Commande {OrderId} recue ({ItemCount} article(s)), tentative de reservation du stock...",
            orderCreated.OrderId,
            orderCreated.Items.Count);

        // On garde la trace des articles réservés avec succès, pour pouvoir
        // les annuler si un article suivant échoue.
        var reservedItems = new List<(string ProductId, int Quantity)>();
        string? failureReason = null;

        foreach (var item in orderCreated.Items)
        {
            var success = await _repository.TryDecrementStockAsync(item.ProductId, item.Quantity);

            if (success)
            {
                reservedItems.Add((item.ProductId, item.Quantity));
            }
            else
            {
                failureReason = $"Stock insuffisant pour le produit {item.ProductId} (quantite demandee : {item.Quantity})";
                break; // inutile de continuer, la commande entière va être annulée
            }
        }

        if (failureReason is null)
        {
            _logger.LogInformation("Stock reserve avec succes pour la commande {OrderId}", orderCreated.OrderId);

            // context.Publish (plutôt qu'un IPublishEndpoint injecté) relie
            // automatiquement ce message publié au message consommé
            // (conversation/correlation), utile pour le traçage de bout en
            // bout d'une saga.
            await context.Publish(new StockReserved(orderCreated.OrderId));
        }
        else
        {
            _logger.LogWarning(
                "Echec de reservation pour la commande {OrderId} : {Reason}. Annulation des {Count} decrement(s) deja applique(s).",
                orderCreated.OrderId, failureReason, reservedItems.Count);

            // Compensation : on annule les décréments déjà appliqués sur les
            // autres articles de cette même commande.
            foreach (var (productId, quantity) in reservedItems)
            {
                await _repository.IncrementStockAsync(productId, quantity);
            }

            await context.Publish(new StockReservationFailed(orderCreated.OrderId, failureReason));
        }
    }
}