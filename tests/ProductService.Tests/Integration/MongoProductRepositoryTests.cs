using Microsoft.Extensions.Options;
using ProductService.Data;
using ProductService.Models;
using Testcontainers.MongoDb;
using Xunit;

namespace ProductService.Tests.Integration;

/// <summary>
/// Teste MongoProductRepository contre une vraie instance MongoDB, démarrée
/// dans un conteneur Docker éphémère le temps du test (via Testcontainers).
/// Contrairement aux tests unitaires (qui mockent IProductRepository), ceci
/// vérifie que le vrai driver Mongo — filtres, updates BSON, décrémentation
/// atomique du stock — se comporte réellement comme prévu.
/// </summary>
public class MongoProductRepositoryTests : IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer = new MongoDbBuilder("mongo:7")
    .Build();
    
    private MongoProductRepository _repository = null!;

    // IAsyncLifetime : xUnit appelle InitializeAsync avant le premier test
    // de la classe, et DisposeAsync après le dernier. Le conteneur ne
    // démarre donc qu'une fois par classe de test, pas par test individuel.
    public async Task InitializeAsync()
    {
        await _mongoContainer.StartAsync();

        var settings = Options.Create(new MongoDbSettings
        {
            ConnectionString = _mongoContainer.GetConnectionString(),
            DatabaseName = "ProductServiceTestsDb"
        });

        _repository = new MongoProductRepository(new MongoDbContext(settings));
    }

    public async Task DisposeAsync() => await _mongoContainer.DisposeAsync();

    [Fact]
    public async Task CreateAsync_ThenGetByIdAsync_ReturnsSameProduct()
    {
        var product = new Product { Name = "Clavier", Description = "RGB", Price = 79.99m, StockQuantity = 10 };

        await _repository.CreateAsync(product);
        var retrieved = await _repository.GetByIdAsync(product.Id);

        Assert.NotNull(retrieved);
        Assert.Equal("Clavier", retrieved!.Name);
        Assert.Equal(79.99m, retrieved.Price);
    }

    [Fact]
    public async Task TryDecrementStockAsync_SufficientStock_DecrementsAndReturnsTrue()
    {
        var product = new Product { Name = "Souris", Price = 39.99m, StockQuantity = 10 };
        await _repository.CreateAsync(product);

        var success = await _repository.TryDecrementStockAsync(product.Id, 3);

        Assert.True(success);
        var updated = await _repository.GetByIdAsync(product.Id);
        Assert.Equal(7, updated!.StockQuantity);
    }

    [Fact]
    public async Task TryDecrementStockAsync_InsufficientStock_ReturnsFalseAndDoesNotChangeStock()
    {
        // Vérifie le comportement précis du filtre Gte + Inc (étape 10) :
        // impossible à valider avec un mock, c'est le driver Mongo réel qui
        // doit refuser d'appliquer l'update.
        var product = new Product { Name = "Ecran", Price = 199.99m, StockQuantity = 2 };
        await _repository.CreateAsync(product);

        var success = await _repository.TryDecrementStockAsync(product.Id, 5);

        Assert.False(success);
        var unchanged = await _repository.GetByIdAsync(product.Id);
        Assert.Equal(2, unchanged!.StockQuantity);
    }

    [Fact]
    public async Task DeleteAsync_ExistingProduct_RemovesIt()
    {
        var product = new Product { Name = "Tapis de souris", Price = 9.99m, StockQuantity = 50 };
        await _repository.CreateAsync(product);

        var deleted = await _repository.DeleteAsync(product.Id);

        Assert.True(deleted);
        Assert.Null(await _repository.GetByIdAsync(product.Id));
    }
}