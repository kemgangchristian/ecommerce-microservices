namespace ECommerce.Contracts.Events;

/// <summary>
/// Publié par ProductService quand le stock n'a pas pu être réservé pour au
/// moins un article de la commande. Les décréments déjà appliqués sur les
/// autres articles de la même commande ont été annulés (compensation) avant
/// la publication de cet événement — aucune réservation partielle ne
/// subsiste en base au moment où OrderService reçoit ce message.
/// </summary>
public record StockReservationFailed(Guid OrderId, string Reason);