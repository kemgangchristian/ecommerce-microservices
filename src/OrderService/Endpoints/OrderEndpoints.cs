using ECommerce.Contracts.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Models;

namespace OrderService.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");

        group.MapGet("/", GetAllOrders);
        group.MapGet("/{id:guid}", GetOrderById);
        group.MapPost("/", CreateOrder);
    }

    public static async Task<IResult> GetAllOrders(OrderDbContext db) =>
        Results.Ok(await db.Orders.Include(o => o.Items).AsNoTracking().ToListAsync());

    public static async Task<IResult> GetOrderById(Guid id, OrderDbContext db)
    {
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        return order is not null ? Results.Ok(order) : Results.NotFound();
    }

    public static async Task<IResult> CreateOrder(CreateOrderRequest request, OrderDbContext db, IPublishEndpoint publishEndpoint)
    {
        if (request.Items.Count == 0)
            return Results.BadRequest("La commande doit contenir au moins un article.");

        var order = new Order
        {
            CustomerEmail = request.CustomerEmail,
            Status = OrderStatus.Pending,
            Items = request.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        await publishEndpoint.Publish(new OrderCreated(
            order.Id,
            order.CreatedAt,
            order.Items
                .Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice))
                .ToList()
        ));

        return Results.Created($"/api/orders/{order.Id}", order);
    }
}