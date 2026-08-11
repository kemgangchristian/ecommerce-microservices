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
        repositoryMock
            .Setup(r => r.TryDecrementStockAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        await using var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton(repositoryMock.Object)
            .AddMassTransitTestHarness(x => x.AddConsumer<OrderCreatedConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            var orderId = Guid.NewGuid();
            var orderCreated = new OrderCreated(
                orderId,
                DateTime.UtcNow,
                new[]
                {
                    new OrderItemDto("product-1", "Clavier", 2, 79.99m),
                    new OrderItemDto("product-2", "Souris", 1, 39.99m)
                });

            await harness.Bus.Publish(orderCreated);

            Assert.True(await harness.Consumed.Any<OrderCreated>());

            // Le consumer doit avoir publié StockReserved en retour...
            Assert.True(await harness.Published.Any<StockReserved>(x => x.Context.Message.OrderId == orderId));
            // ...et jamais StockReservationFailed.
            Assert.False(await harness.Published.Any<StockReservationFailed>());

            repositoryMock.Verify(r => r.TryDecrementStockAsync("product-1", 2), Times.Once);
            repositoryMock.Verify(r => r.TryDecrementStockAsync("product-2", 1), Times.Once);
            repositoryMock.Verify(r => r.IncrementStockAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task Consume_SecondItemInsufficientStock_RollsBackFirstItem_PublishesStockReservationFailed()
    {
        var repositoryMock = new Mock<IProductRepository>();

        // Le premier article réussit, le second échoue (stock insuffisant).
        repositoryMock.Setup(r => r.TryDecrementStockAsync("product-1", 2)).ReturnsAsync(true);
        repositoryMock.Setup(r => r.TryDecrementStockAsync("product-2", 100)).ReturnsAsync(false);

        await using var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton(repositoryMock.Object)
            .AddMassTransitTestHarness(x => x.AddConsumer<OrderCreatedConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            var orderId = Guid.NewGuid();
            var orderCreated = new OrderCreated(
                orderId,
                DateTime.UtcNow,
                new[]
                {
                    new OrderItemDto("product-1", "Clavier", 2, 79.99m),
                    new OrderItemDto("product-2", "Souris", 100, 39.99m)
                });

            await harness.Bus.Publish(orderCreated);

            Assert.True(await harness.Consumed.Any<OrderCreated>());

            // Compensation : le premier article (réussi) doit être annulé...
            repositoryMock.Verify(r => r.IncrementStockAsync("product-1", 2), Times.Once);
            // ...et le second (jamais réservé) ne doit surtout pas l'être.
            repositoryMock.Verify(r => r.IncrementStockAsync("product-2", It.IsAny<int>()), Times.Never);

            Assert.True(await harness.Published.Any<StockReservationFailed>(x => x.Context.Message.OrderId == orderId));
            Assert.False(await harness.Published.Any<StockReserved>());
        }
        finally
        {
            await harness.Stop();
        }
    }
}