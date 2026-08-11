namespace ECommerce.Contracts.Events;

/// <summary>
/// Publié par ProductService quand le stock a été réservé (décrémenté) avec
/// succès pour TOUS les articles d'une commande.
/// </summary>
public record StockReserved(Guid OrderId);