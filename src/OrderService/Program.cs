using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Endpoints;
using OrderService.Consumers;

var builder = WebApplication.CreateBuilder(args);

// --- Documentation API ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Base de données (PostgreSQL via Npgsql) ---
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("OrderDb")
        ?? throw new InvalidOperationException("Connection string 'OrderDb' introuvable dans la configuration.")));

// --- Messagerie asynchrone ---
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<StockReservedConsumer>();
    x.AddConsumer<StockReservationFailedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });

        cfg.ReceiveEndpoint("order-service-stock-reserved", e =>
        {
            e.ConfigureConsumer<StockReservedConsumer>(context);
        });

        cfg.ReceiveEndpoint("order-service-stock-reservation-failed", e =>
        {
            e.ConfigureConsumer<StockReservationFailedConsumer>(context);
        });
    });
});

var app = builder.Build();

// --- Application automatique des migrations au démarrage ---
// Pratique en développement. En production, on préfère généralement
// exécuter les migrations via un pipeline CI/CD dédié plutôt qu'au
// démarrage de l'app, pour éviter des migrations concurrentes si plusieurs
// instances du service démarrent en même temps.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapOrderEndpoints();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "OrderService" }));

app.Run();