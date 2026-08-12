using Microsoft.AspNetCore.Http;
using Moq;
using ProductService.Data;
using ProductService.DTOs;
using ProductService.Endpoints;
using ProductService.Models;
using Xunit;

namespace ProductService.Tests.Endpoints;

public class ProductEndpointsTests
{
    [Fact]
    public async Task GetAllProducts_ReturnsOkWithProductList()
    {
        var products = new List<Product>
        {
            new() { Name = "Clavier", Price = 79.99m, StockQuantity = 10 },
            new() { Name = "Souris", Price = 39.99m, StockQuantity = 20 }
        };
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(products);

        var result = await ProductEndpoints.GetAllProducts(repositoryMock.Object);

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
    public async Task CreateProduct_CallsRepositoryWithMappedProduct_ReturnsCreated()
    {
        var repositoryMock = new Mock<IProductRepository>();
        var request = new CreateProductRequest("Ecran", "27 pouces", 199.99m, 5);

        var result = await ProductEndpoints.CreateProduct(request, repositoryMock.Object);

        // Vérifie que le Product construit à partir du DTO contient bien
        // les valeurs fournies (l'Id est généré en interne, on ne le teste pas).
        repositoryMock.Verify(r => r.CreateAsync(It.Is<Product>(p =>
            p.Name == "Ecran" &&
            p.Description == "27 pouces" &&
            p.Price == 199.99m &&
            p.StockQuantity == 5)), Times.Once);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status201Created, statusResult.StatusCode);
    }

    [Fact]
    public async Task ReplaceProduct_ExistingId_ReturnsNoContent()
    {
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock.Setup(r => r.UpdateAsync("abc123", It.IsAny<Product>())).ReturnsAsync(true);

        var request = new CreateProductRequest("Clavier", "RGB", 79.99m, 10);
        var result = await ProductEndpoints.ReplaceProduct("abc123", request, repositoryMock.Object);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status204NoContent, statusResult.StatusCode);
    }

    [Fact]
    public async Task ReplaceProduct_UnknownId_ReturnsNotFound()
    {
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Product>())).ReturnsAsync(false);

        var request = new CreateProductRequest("Clavier", "RGB", 79.99m, 10);
        var result = await ProductEndpoints.ReplaceProduct("unknown", request, repositoryMock.Object);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, statusResult.StatusCode);
    }

    [Fact]
    public async Task PatchProduct_OnlyPriceProvided_CallsRepositoryWithOnlyPriceSet()
    {
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(r => r.PatchAsync("abc123", null, null, 89.99m, null))
            .ReturnsAsync(true);

        var request = new UpdateProductRequest(null, null, 89.99m, null);
        var result = await ProductEndpoints.PatchProduct("abc123", request, repositoryMock.Object);

        // Preuve du comportement partiel : seul le prix est transmis au
        // repository, les 3 autres paramètres restent null.
        repositoryMock.Verify(r => r.PatchAsync("abc123", null, null, 89.99m, null), Times.Once);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status204NoContent, statusResult.StatusCode);
    }

    [Fact]
    public async Task PatchProduct_UnknownId_ReturnsNotFound()
    {
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(r => r.PatchAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<decimal?>(), It.IsAny<int?>()))
            .ReturnsAsync(false);

        var request = new UpdateProductRequest("Nouveau nom", null, null, null);
        var result = await ProductEndpoints.PatchProduct("unknown", request, repositoryMock.Object);

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