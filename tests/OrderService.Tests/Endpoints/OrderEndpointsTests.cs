using ECommerce.Contracts.Events;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Endpoints;
using OrderService.Models;
using Xunit;

namespace OrderService.Tests.Endpoints;

public class OrderEndpointsTests
{
    /// <summary>
    /// Crée un OrderDbContext isolé, backé par le provider EF Core In-Memory
    /// plutôt qu'une vraie base PostgreSQL. Chaque test utilise un nom de
    /// base unique (Guid) pour ne jamais partager d'état entre tests.
    /// </summary>
    private static OrderDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrderDbContext(options);
    }

    [Fact]
    public async Task CreateOrder_EmptyItems_ReturnsBadRequest_AndDoesNotPublish()
    {
        using var db = CreateInMemoryDbContext();
        var publishEndpointMock = new Mock<IPublishEndpoint>();
        var request = new CreateOrderRequest("client@example.com", new List<CreateOrderItemRequest>());

        var result = await OrderEndpoints.CreateOrder(request, db, publishEndpointMock.Object);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);

        // Aucun événement ne doit être publié si la commande est invalide.
        publishEndpointMock.Verify(
            p => p.Publish(It.IsAny<OrderCreated>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateOrder_ValidRequest_PersistsOrderAndPublishesEvent()
    {
        using var db = CreateInMemoryDbContext();
        var publishEndpointMock = new Mock<IPublishEndpoint>();

        var request = new CreateOrderRequest(
            "client@example.com",
            new List<CreateOrderItemRequest> { new("product-1", "Clavier", 2, 79.99m) });

        var result = await OrderEndpoints.CreateOrder(request, db, publishEndpointMock.Object);

        // La commande doit être persistée en base...
        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status201Created, statusResult.StatusCode);
        Assert.Single(db.Orders);
        Assert.Single(db.Orders.Include(o => o.Items).Single().Items);

        // ...et l'événement OrderCreated doit être publié avec le bon
        // contenu, une seule fois — c'est le comportement clé de
        // l'architecture qu'on vérifie ici.
        publishEndpointMock.Verify(p => p.Publish(
            It.Is<OrderCreated>(e =>
                e.Items.Count == 1 &&
                e.Items[0].ProductId == "product-1" &&
                e.Items[0].Quantity == 2),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetOrderById_ExistingId_ReturnsOkWithOrder()
    {
        using var db = CreateInMemoryDbContext();
        var order = new Order { CustomerEmail = "client@example.com" };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var result = await OrderEndpoints.GetOrderById(order.Id, db);

        var valueResult = Assert.IsAssignableFrom<IValueHttpResult<Order>>(result);
        Assert.Equal("client@example.com", valueResult.Value!.CustomerEmail);
    }

    [Fact]
    public async Task GetOrderById_UnknownId_ReturnsNotFound()
    {
        using var db = CreateInMemoryDbContext();

        var result = await OrderEndpoints.GetOrderById(Guid.NewGuid(), db);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsAllPersistedOrders()
    {
        using var db = CreateInMemoryDbContext();
        db.Orders.AddRange(
            new Order { CustomerEmail = "a@example.com" },
            new Order { CustomerEmail = "b@example.com" });
        await db.SaveChangesAsync();

        var result = await OrderEndpoints.GetAllOrders(db);

        var valueResult = Assert.IsAssignableFrom<IValueHttpResult<List<Order>>>(result);
        Assert.Equal(2, valueResult.Value!.Count);
    }
}