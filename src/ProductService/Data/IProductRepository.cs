using ProductService.Models;

namespace ProductService.Data;

/// <summary>
/// Abstraction au-dessus de la persistance des produits. Isoler l'accès aux
/// données derrière cette interface permet de mocker facilement le contrat
/// dans les tests unitaires des endpoints et du consumer, sans dépendre du
/// driver MongoDB directement.
/// </summary>
public interface IProductRepository
{
    Task<List<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(string id);
    Task CreateAsync(Product product);
    Task<bool> UpdateAsync(string id, Product input);
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Réincrémente le stock d'un produit. Utilisé en compensation lorsqu'une
    /// réservation de stock doit être annulée (un autre article de la même
    /// commande n'a pas pu être réservé).
    /// </summary>
    Task IncrementStockAsync(string productId, int quantity);

    /// <summary>
    /// Décrémente le stock de façon atomique si la quantité disponible est
    /// suffisante. Retourne false si le produit n'existe pas ou si le stock
    /// est insuffisant (dans ce cas, aucune écriture n'a lieu).
    /// </summary>
    Task<bool> TryDecrementStockAsync(string productId, int quantity);

    Task<bool> AnyAsync();
    Task InsertManyAsync(IEnumerable<Product> products);
}