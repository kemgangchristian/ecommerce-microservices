using ProductService.Data;
using ProductService.DTOs;
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
        group.MapPut("/{id}", ReplaceProduct);
        group.MapPatch("/{id}", PatchProduct);
        group.MapDelete("/{id}", DeleteProduct);
    }

    public static async Task<IResult> GetAllProducts(IProductRepository repository) =>
        Results.Ok(await repository.GetAllAsync());

    public static async Task<IResult> GetProductById(string id, IProductRepository repository)
    {
        var product = await repository.GetByIdAsync(id);
        return product is not null ? Results.Ok(product) : Results.NotFound();
    }

    public static async Task<IResult> CreateProduct(CreateProductRequest request, IProductRepository repository)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity
        };

        await repository.CreateAsync(product);
        return Results.Created($"/api/products/{product.Id}", product);
    }

    // PUT : remplacement complet, réutilise la même forme que la création.
    public static async Task<IResult> ReplaceProduct(string id, CreateProductRequest request, IProductRepository repository)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity
        };

        var updated = await repository.UpdateAsync(id, product);
        return updated ? Results.NoContent() : Results.NotFound();
    }

    // PATCH : mise à jour partielle, seuls les champs fournis sont modifiés.
    public static async Task<IResult> PatchProduct(string id, UpdateProductRequest request, IProductRepository repository)
    {
        var updated = await repository.PatchAsync(id, request.Name, request.Description, request.Price, request.StockQuantity);
        return updated ? Results.NoContent() : Results.NotFound();
    }

    public static async Task<IResult> DeleteProduct(string id, IProductRepository repository)
    {
        var deleted = await repository.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}