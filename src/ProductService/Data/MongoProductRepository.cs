using MongoDB.Driver;
using ProductService.Models;

namespace ProductService.Data;

/// <summary>Implémentation MongoDB de <see cref="IProductRepository"/>.</summary>
public class MongoProductRepository : IProductRepository
{
    private readonly MongoDbContext _db;

    public MongoProductRepository(MongoDbContext db) => _db = db;

    public async Task<List<Product>> GetAllAsync() =>
        await _db.Products.Find(_ => true).ToListAsync();

    public async Task<Product?> GetByIdAsync(string id) =>
        await _db.Products.Find(p => p.Id == id).FirstOrDefaultAsync();

    public Task CreateAsync(Product product) =>
        _db.Products.InsertOneAsync(product);

    public async Task<bool> UpdateAsync(string id, Product input)
    {
        var update = Builders<Product>.Update
            .Set(p => p.Name, input.Name)
            .Set(p => p.Description, input.Description)
            .Set(p => p.Price, input.Price)
            .Set(p => p.StockQuantity, input.StockQuantity);

        var result = await _db.Products.UpdateOneAsync(p => p.Id == id, update);
        return result.MatchedCount > 0;
    }

    public async Task<bool> PatchAsync(string id, string? name, string? description, decimal? price, int? stockQuantity)
    {
        var updates = new List<UpdateDefinition<Product>>();

        if (name is not null)
            updates.Add(Builders<Product>.Update.Set(p => p.Name, name));
        if (description is not null)
            updates.Add(Builders<Product>.Update.Set(p => p.Description, description));
        if (price is not null)
            updates.Add(Builders<Product>.Update.Set(p => p.Price, price.Value));
        if (stockQuantity is not null)
            updates.Add(Builders<Product>.Update.Set(p => p.StockQuantity, stockQuantity.Value));

        if (updates.Count == 0)
            return await GetByIdAsync(id) is not null;

        var combinedUpdate = Builders<Product>.Update.Combine(updates);
        var result = await _db.Products.UpdateOneAsync(p => p.Id == id, combinedUpdate);
        return result.MatchedCount > 0;
    }
    
    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _db.Products.DeleteOneAsync(p => p.Id == id);
        return result.DeletedCount > 0;
    }

    public async Task IncrementStockAsync(string productId, int quantity)
    {
        var update = Builders<Product>.Update.Inc(p => p.StockQuantity, quantity);
        await _db.Products.UpdateOneAsync(p => p.Id == productId, update);
    }
    
    public async Task<bool> TryDecrementStockAsync(string productId, int quantity)
    {
        // Filtre combinant identité + stock suffisant : garantit l'atomicité
        // sans lecture préalable ni verrou explicite (voir étape 10).
        var filter = Builders<Product>.Filter.And(
            Builders<Product>.Filter.Eq(p => p.Id, productId),
            Builders<Product>.Filter.Gte(p => p.StockQuantity, quantity));

        var update = Builders<Product>.Update.Inc(p => p.StockQuantity, -quantity);

        var result = await _db.Products.UpdateOneAsync(filter, update);
        return result.MatchedCount > 0;
    }

    public async Task<bool> AnyAsync() =>
        await _db.Products.Find(_ => true).AnyAsync();

    public Task InsertManyAsync(IEnumerable<Product> products) =>
        _db.Products.InsertManyAsync(products);
}