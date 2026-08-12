using MassTransit;
using ProductService.Consumers;
using ProductService.Data;
using ProductService.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// --- Documentation API (Swagger UI, disponible en développement) ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Configuration MongoDB ---
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

// Aucune donnée n'est insérée automatiquement au démarrage : la base
// démarre vide, seule l'API (POST /api/products) permet d'y ajouter des
// produits.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapProductEndpoints();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ProductService" }));

app.Run();