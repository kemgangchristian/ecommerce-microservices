namespace OrderService.Models;

/// <summary>
/// Ligne d'une commande. <see cref="ProductId"/> est volontairement un
/// <c>string</c> et non un <c>Guid</c> : il référence un document
/// ProductService (MongoDB), dont l'identifiant natif est un ObjectId
/// représenté en chaîne. Aucune clé étrangère de base de données n'est
/// possible ici — Product vit dans un service et une base entièrement
/// différents — c'est une référence purement logique, résolue par
/// l'appelant au moment de la création de la commande.
/// </summary>
public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}