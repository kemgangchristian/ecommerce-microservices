using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ProductService.Models;

namespace ProductService.Data;

/// <summary>
/// Fournit l'accès à la base MongoDB du service Produits.
/// Encapsule le client Mongo et expose les collections utilisées par l'application.
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    /// <summary>
    /// Initialise le contexte à partir des paramètres de connexion Mongo (chaîne de connexion et nom de base)
    /// injectés via le pattern Options (<see cref="MongoDbSettings"/>).
    /// </summary>
    /// <param name="settings">Paramètres de connexion à la base MongoDB.</param>
    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    /// <summary>
    /// Collection MongoDB "Products" contenant les produits du catalogue.
    /// </summary>
    public IMongoCollection<Product> Products => _database.GetCollection<Product>("Products");
}
