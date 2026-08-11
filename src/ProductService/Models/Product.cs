using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProductService.Models;

/// <summary>
/// Représente un produit du catalogue, tel que stocké dans la collection MongoDB "Products".
/// </summary>
public class Product
{
    /// <summary>
    /// Identifiant unique du produit (ObjectId MongoDB représenté sous forme de chaîne).
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>
    /// Nom du produit affiché aux utilisateurs.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description détaillée du produit.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Prix unitaire du produit.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Quantité disponible en stock.
    /// </summary>
    public int StockQuantity { get; set; }
}
