using MassTransit;
using ProductService.Consumers;
using ProductService.Data;
using ProductService.Endpoints;
using ProductService.Models;

var builder = WebApplication.CreateBuilder(args);

// --- Documentation API (Swagger UI, disponible en développement) ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Configuration MongoDB ---
// Lie la section "MongoDb" de appsettings.json à MongoDbSettings, injectée
// ensuite via IOptions<MongoDbSettings> dans le constructeur de MongoDbContext.
// AddSingleton : un seul MongoClient/MongoDbContext pour toute la durée de
// vie de l'application (le driver Mongo gère lui-même un pool de connexions
// en interne, inutile d'en recréer un par requête).
// IProductRepository masque le driver Mongo derrière une interface : les
// endpoints et le consumer en dépendent, jamais de Mongo directement — ce
// qui les rend testables unitairement avec un simple mock.
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDb"));
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddSingleton<IProductRepository, MongoProductRepository>();

// --- Messagerie asynchrone (MassTransit + RabbitMQ) ---
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });

        cfg.ReceiveEndpoint("product-service-order-created", e =>
        {
            e.ConfigureConsumer<OrderCreatedConsumer>(context);
        });
    });
});

var app = builder.Build();

// --- Données de test ---
// Contrairement à EF Core, MongoDB ne nécessite aucune migration : on
// insère simplement un jeu de données si la collection est vide, pour
// pouvoir tester l'API dès le premier démarrage.
await SeedDataAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapProductEndpoints();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ProductService" }));

app.Run();

/// <summary>
/// Insère un jeu de données minimal au premier démarrage si la collection
/// "Products" est vide. Idempotent : ne duplique rien aux démarrages suivants.
/// </summary>
static async Task SeedDataAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

    var alreadySeeded = await repository.AnyAsync();
    if (alreadySeeded) return;

    await repository.InsertManyAsync(new[]
    {
        new Product { Name = "Clavier mecanique", Description = "Clavier RGB switches rouges", Price = 79.99m, StockQuantity = 50 },
        new Product { Name = "Souris sans fil", Description = "Souris ergonomique 2.4GHz", Price = 39.99m, StockQuantity = 100 }
    });
}