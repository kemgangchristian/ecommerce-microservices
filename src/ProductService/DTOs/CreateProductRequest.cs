namespace ProductService.DTOs;

/// <summary>
/// DTO d'entrée pour la création (POST) et le remplacement complet (PUT)
/// d'un produit. Tous les champs sont obligatoires — contrairement à
/// UpdateProductRequest, prévu pour les mises à jour partielles (PATCH).
/// </summary>
public record CreateProductRequest(string Name, string Description, decimal Price, int StockQuantity);