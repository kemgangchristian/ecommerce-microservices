namespace ECommerce.Contracts.Events;

/// <summary>
/// Représente un article d'une commande au sein de l'événement <see cref="OrderCreated"/>.
/// Contient une copie des informations produit au moment de la commande (nom, prix)
/// afin de ne pas dépendre de l'état courant du catalogue.
/// </summary>
/// <param name="ProductId">Identifiant du produit commandé.</param>
/// <param name="ProductName">Nom du produit au moment de la commande.</param>
/// <param name="Quantity">Quantité commandée.</param>
/// <param name="UnitPrice">Prix unitaire au moment de la commande.</param>
public record OrderItemDto(string ProductId, string ProductName, int Quantity, decimal UnitPrice);
