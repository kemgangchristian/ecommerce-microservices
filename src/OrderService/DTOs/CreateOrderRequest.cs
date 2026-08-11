namespace OrderService.DTOs;

/// <summary>DTO d'entrée pour un article lors de la création d'une commande.</summary>
public record CreateOrderItemRequest(string ProductId, string ProductName, int Quantity, decimal UnitPrice);

/// <summary>DTO d'entrée pour la création d'une commande complète.</summary>
public record CreateOrderRequest(string CustomerEmail, List<CreateOrderItemRequest> Items);