using ECommerce.Contracts.Events;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OrderService.Consumers;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Endpoints;
using OrderService.Models;
using ProductService.Consumers;
using ProductService.Data;
using ProductService.Models;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Test bout-en-bout : fait tourner côte à côte le vrai code de publication
/// d'OrderService et le vrai code de consommation de ProductService,
/// connectés à un RabbitMQ réel (conteneur Docker), avec leurs vraies bases
/// respectives (PostgreSQL, MongoDB). Aucun mock ni test harness en mémoire
/// ici : c'est la preuve que l'architecture événementielle fonctionne
/// réellement de bout en bout, pas seulement que chaque service se comporte
/// bien isolément.
/// </summary>
public class OrderToProductEndToEndTests : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management")
        .Build();

    private readonly MongoDbContainer _mongoContainer = new MongoDbBuilder()
        .WithImage("mongo:7")
        .Build();

    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("OrderServiceTestsDb")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private ServiceProvider _orderServiceProvider = null!;
    private ServiceProvider _productServiceProvider = null!;
    private MongoProductRepository _productRepository = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _rabbitMqContainer.StartAsync(),
            _mongoContainer.StartAsync(),
            _postgresContainer.StartAsync());

        var rabbitMqUri = new Uri(_rabbitMqContainer.GetConnectionString());

        // --- "OrderService" : reproduit exactement son Program.cs réel ---
        // (DbContext PostgreSQL + MassTransit publish-only, aucun consumer).
        _orderServiceProvider = new ServiceCollection()
            .AddLogging()
            .AddDbContext<OrderDbContext>(o => o.UseNpgsql(_postgresContainer.GetConnectionString()))
            .AddMassTransit(x =>
            {
                x.AddConsumer<StockReservedConsumer>();
                x.AddConsumer<StockReservationFailedConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(rabbitMqUri);

                    cfg.ReceiveEndpoint("order-service-stock-reserved", e =>
                    {
                        e.ConfigureConsumer<StockReservedConsumer>(context);
                    });

                    cfg.ReceiveEndpoint("order-service-stock-reservation-failed", e =>
                    {
                        e.ConfigureConsumer<StockReservationFailedConsumer>(context);
                    });
                });
            })
            .BuildServiceProvider(true);

        using (var scope = _orderServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            await db.Database.MigrateAsync();
        }

        // --- "ProductService" : le vrai OrderCreatedConsumer, branché sur
        // un vrai MongoProductRepository (vraie base MongoDB, pas de mock) ---
        var mongoSettings = Options.Create(new MongoDbSettings
        {
            ConnectionString = _mongoContainer.GetConnectionString(),
            DatabaseName = "ProductServiceTestsDb"
        });
        _productRepository = new MongoProductRepository(new MongoDbContext(mongoSettings));

        _productServiceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IProductRepository>(_productRepository)
            .AddMassTransit(x =>
            {
                x.AddConsumer<OrderCreatedConsumer>();
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(rabbitMqUri);
                    cfg.ReceiveEndpoint("product-service-order-created", e =>
                    {
                        e.ConfigureConsumer<OrderCreatedConsumer>(context);
                    });
                });
            })
            .BuildServiceProvider(true);

        // AddMassTransit enregistre un IHostedService, normalement démarré
        // par le host ASP.NET Core (app.Run()). Ici il n'y a pas de host,
        // donc on le démarre nous-mêmes pour les deux "applications".
        await StartHostedServicesAsync(_orderServiceProvider);
        await StartHostedServicesAsync(_productServiceProvider);
    }

    private static async Task StartHostedServicesAsync(IServiceProvider provider)
    {
        foreach (var hostedService in provider.GetServices<IHostedService>())
            await hostedService.StartAsync(CancellationToken.None);
    }

    private static async Task StopHostedServicesAsync(IServiceProvider provider)
    {
        foreach (var hostedService in provider.GetServices<IHostedService>())
            await hostedService.StopAsync(CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        await StopHostedServicesAsync(_orderServiceProvider);
        await StopHostedServicesAsync(_productServiceProvider);

        await _orderServiceProvider.DisposeAsync();
        await _productServiceProvider.DisposeAsync();

        await Task.WhenAll(
            _rabbitMqContainer.DisposeAsync().AsTask(),
            _mongoContainer.DisposeAsync().AsTask(),
            _postgresContainer.DisposeAsync().AsTask());
    }

    [Fact]
    public async Task CreateOrder_PublishesEvent_ConsumedByRealProductService_DecrementsStockAndConfirmsOrder()
    {
        var product = new Product { Name = "Clavier", Price = 79.99m, StockQuantity = 10 };
        await _productRepository.CreateAsync(product);

        Guid orderId;
        using (var orderScope = _orderServiceProvider.CreateScope())
        {
            var orderDb = orderScope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var publishEndpoint = orderScope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            var request = new CreateOrderRequest(
                "client@example.com",
                new List<CreateOrderItemRequest> { new(product.Id, product.Name, 3, product.Price) });

            var result = await OrderEndpoints.CreateOrder(request, orderDb, publishEndpoint);
            var valueResult = Assert.IsAssignableFrom<IValueHttpResult<Order>>(result);
            orderId = valueResult.Value!.Id;
        }

        // Le stock doit être décrémenté dans le vrai MongoDB...
        Product? updatedProduct = null;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            updatedProduct = await _productRepository.GetByIdAsync(product.Id);
            if (updatedProduct?.StockQuantity == 7) break;
            await Task.Delay(500);
        }
        Assert.NotNull(updatedProduct);
        Assert.Equal(7, updatedProduct!.StockQuantity);

        // ...ET la commande doit passer à Confirmed dans le vrai PostgreSQL :
        // preuve que le round-trip StockReserved a bien été consommé par
        // OrderService, pas juste que ProductService a publié quelque chose.
        Order? finalOrder = null;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            using var readScope = _orderServiceProvider.CreateScope();
            var readDb = readScope.ServiceProvider.GetRequiredService<OrderDbContext>();
            finalOrder = await readDb.Orders.FindAsync(orderId);
            if (finalOrder?.Status == OrderStatus.Confirmed) break;
            await Task.Delay(500);
        }
        Assert.NotNull(finalOrder);
        Assert.Equal(OrderStatus.Confirmed, finalOrder!.Status);
    }

    [Fact]
    public async Task CreateOrder_InsufficientStock_CancelsOrder_AndLeavesStockUnchanged()
    {
        var product = new Product { Name = "Ecran", Price = 199.99m, StockQuantity = 2 };
        await _productRepository.CreateAsync(product);

        Guid orderId;
        using (var orderScope = _orderServiceProvider.CreateScope())
        {
            var orderDb = orderScope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var publishEndpoint = orderScope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            var request = new CreateOrderRequest(
                "client@example.com",
                new List<CreateOrderItemRequest> { new(product.Id, product.Name, 10, product.Price) });

            var result = await OrderEndpoints.CreateOrder(request, orderDb, publishEndpoint);
            var valueResult = Assert.IsAssignableFrom<IValueHttpResult<Order>>(result);
            orderId = valueResult.Value!.Id;
        }

        Order? finalOrder = null;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            using var readScope = _orderServiceProvider.CreateScope();
            var readDb = readScope.ServiceProvider.GetRequiredService<OrderDbContext>();
            finalOrder = await readDb.Orders.FindAsync(orderId);
            if (finalOrder?.Status == OrderStatus.Cancelled) break;
            await Task.Delay(500);
        }

        Assert.NotNull(finalOrder);
        Assert.Equal(OrderStatus.Cancelled, finalOrder!.Status);
        Assert.NotNull(finalOrder.CancellationReason);

        // Un seul article, jamais réservé avec succès : rien à compenser, le
        // stock ne doit pas avoir bougé.
        var unchangedProduct = await _productRepository.GetByIdAsync(product.Id);
        Assert.Equal(2, unchangedProduct!.StockQuantity);
    }
}