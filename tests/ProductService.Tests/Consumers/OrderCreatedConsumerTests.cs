using ECommerce.Contracts.Events;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ProductService.Consumers;
using ProductService.Data;
using Xunit;

namespace ProductService.Tests.Consumers;

public class OrderCreatedConsumerTests
{
    [Fact]
    public async Task Consume_AllItemsHaveSufficientStock_PublishesStockReserved()
    {
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock.Setup(r => r.TryDecrementStockAsync("product-1", 2)).ReturnsAsync(true);
        repositoryMock.Setup(r => r.TryDecrementStockAsync("product-2", 3)).ReturnsAsync(true);

        await using var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton(repositoryMock.Object)
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<OrderCreatedConsumer>();
                x.SetTestTimeouts(testTimeout: TimeSpan.FromSeconds(30), testInactivityTimeout: TimeSpan.FromSeconds(10));
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            // ... corps du test inchangé (harness.Bus.Publish, Assert, etc.)
        }
        finally { await harness.Stop(); }
    }

    [Fact]
    public async Task Consume_SecondItemInsufficientStock_RollsBackFirstItem_PublishesStockReservationFailed()
    {
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock.Setup(r => r.TryDecrementStockAsync("product-1", 2)).ReturnsAsync(true);
        repositoryMock.Setup(r => r.TryDecrementStockAsync("product-2", 100)).ReturnsAsync(false);

        await using var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton(repositoryMock.Object)
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<OrderCreatedConsumer>();
                x.SetTestTimeouts(testTimeout: TimeSpan.FromSeconds(30), testInactivityTimeout: TimeSpan.FromSeconds(10));
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            var orderId = Guid.NewGuid();
            var orderCreated = new OrderCreated(orderId, DateTime.UtcNow, new[]
            {
                new OrderItemDto("product-1", "Clavier", 2, 79.99m),
                new OrderItemDto("product-2", "Souris", 100, 39.99m)
            });

            await harness.Bus.Publish(orderCreated);

            Assert.True(await harness.Consumed.Any<OrderCreated>());

            repositoryMock.Verify(r => r.IncrementStockAsync("product-1", 2), Times.Once);
            repositoryMock.Verify(r => r.IncrementStockAsync("product-2", It.IsAny<int>()), Times.Never);

            Assert.True(await harness.Published.Any<StockReservationFailed>(x => x.Context.Message.OrderId == orderId));
            Assert.False(await harness.Published.Any<StockReserved>());
        }
        finally { await harness.Stop(); }
    }
}