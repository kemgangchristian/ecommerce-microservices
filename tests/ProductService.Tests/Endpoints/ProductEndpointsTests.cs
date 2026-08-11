using Microsoft.AspNetCore.Http;
using Moq;
using ProductService.Data;
using ProductService.Endpoints;
using ProductService.Models;
using Xunit;

namespace ProductService.Tests.Endpoints;

public class ProductEndpointsTests
{
    [Fact]
    public async Task GetAllProducts_ReturnsOkWithProductList()
    {
        // Arrange : on mocke IProductRepository, jamais Mongo directement.
        var products = new List<Product>
        {
            new() { Name = "Clavier", Price = 79.99m, StockQuantity = 10 },
            new() { Name = "Souris", Price = 39.99m, StockQuantity = 20 }
        };
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(products);

        // Act : on appelle directement la méthode statique, sans démarrer
        // de serveur HTTP — c'est ce que le passage aux méthodes nommées
        // (étape 18) rend possible.
        var result = await ProductEndpoints.GetAllProducts(repositoryMock.Object);

        // Assert : on inspecte le IResult via les interfaces standard des
        // "Typed Results" ASP.NET Core, plutôt que de dépendre d'un type
        // concret précis.
        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);

        var valueResult = Assert.IsAssignableFrom<IValueHttpResult<List<Product>>>(result);
        Assert.Equal(2, valueResult.Value!.Count);
    }

    [Fact]
    public async Task GetProductById_ExistingId_ReturnsOkWithProduct()
    {
        var product = new Product { Id = "abc123", Name = "Clavier", Price = 79.99m, StockQuantity = 10 };
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync("abc123")).ReturnsAsync(product);

        var result = await ProductEndpoints.GetProductById("abc123", repositoryMock.Object);

        var valueResult = Assert.IsAssignableFrom<IValueHttpResult<Product>>(result);
        Assert.Equal("Clavier", valueResult.Value!.Name);
    }

    [Fact]
    public async Task GetProductById_UnknownId_ReturnsNotFound()
    {
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((Product?)null);

        var result = await ProductEndpoints.GetProductById("unknown", repositoryMock.Object);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, statusResult.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_CallsRepositoryOnceAndReturnsCreated()
    {
        var repositoryMock = new Mock<IProductRepository>();
        var product = new Product { Name = "Ecran", Price = 199.99m, StockQuantity = 5 };

        var result = await ProductEndpoints.CreateProduct(product, repositoryMock.Object);

        // Vérifie que la méthode métier attendue a bien été appelée, une
        // seule fois, avec l'objet exact reçu — pas juste que "quelque chose"
        // a été appelé sur le mock.
        repositoryMock.Verify(r => r.CreateAsync(product), Times.Once);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status201Created, statusResult.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_ExistingId_ReturnsNoContent()
    {
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock.Setup(r => r.UpdateAsync("abc123", It.IsAny<Product>())).ReturnsAsync(true);

        var result = await ProductEndpoints.UpdateProduct("abc123", new Product(), repositoryMock.Object);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status204NoContent, statusResult.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_UnknownId_ReturnsNotFound()
    {
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Product>())).ReturnsAsync(false);

        var result = await ProductEndpoints.UpdateProduct("unknown", new Product(), repositoryMock.Object);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, statusResult.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_ExistingId_ReturnsNoContent()
    {
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock.Setup(r => r.DeleteAsync("abc123")).ReturnsAsync(true);

        var result = await ProductEndpoints.DeleteProduct("abc123", repositoryMock.Object);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status204NoContent, statusResult.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_UnknownId_ReturnsNotFound()
    {
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<string>())).ReturnsAsync(false);

        var result = await ProductEndpoints.DeleteProduct("unknown", repositoryMock.Object);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, statusResult.StatusCode);
    }
}