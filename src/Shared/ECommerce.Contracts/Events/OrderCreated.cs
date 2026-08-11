namespace ECommerce.Contracts.Events;

/// <summary>
/// Événement publié lorsqu'une nouvelle commande est créée.
/// Contrat partagé entre les microservices (ex. OrderService en producteur,
/// ProductService ou d'autres consommateurs) pour la communication asynchrone
/// (message broker / bus d'événements).
/// </summary>
/// <param name="OrderId">Identifiant unique de la commande créée.</param>
/// <param name="CreatedAt">Date et heure de création de la commande.</param>
/// <param name="Items">Liste des articles composant la commande.</param>
public record OrderCreated(Guid OrderId, DateTime CreatedAt, IReadOnlyList<OrderItemDto> Items);
