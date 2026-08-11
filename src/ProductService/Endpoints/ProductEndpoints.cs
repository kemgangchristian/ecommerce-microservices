using ProductService.Data;
using ProductService.Models;

namespace ProductService.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        group.MapGet("/", GetAllProducts);
        group.MapGet("/{id}", GetProductById);
        group.MapPost("/", CreateProduct);
        group.MapPut("/{id}", UpdateProduct);
        group.MapDelete("/{id}", DeleteProduct);
    }

    // Méthodes publiques et statiques : testables directement par un appel
    // de méthode classique dans les tests unitaires, sans démarrer de serveur.
    public static async Task<IResult> GetAllProducts(IProductRepository repository) =>
        Results.Ok(await repository.GetAllAsync());

    public static async Task<IResult> GetProductById(string id, IProductRepository repository)
    {
        var product = await repository.GetByIdAsync(id);
        return product is not null ? Results.Ok(product) : Results.NotFound();
    }

    public static async Task<IResult> CreateProduct(Product product, IProductRepository repository)
    {
        await repository.CreateAsync(product);
        return Results.Created($"/api/products/{product.Id}", product);
    }

    public static async Task<IResult> UpdateProduct(string id, Product input, IProductRepository repository)
    {
        var updated = await repository.UpdateAsync(id, input);
        return updated ? Results.NoContent() : Results.NotFound();
    }

    public static async Task<IResult> DeleteProduct(string id, IProductRepository repository)
    {
        var deleted = await repository.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}