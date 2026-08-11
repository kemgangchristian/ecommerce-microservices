var builder = WebApplication.CreateBuilder(args);

// YARP charge sa table de routage depuis la section "ReverseProxy" de
// appsettings.json (routes + clusters). Le fichier est surveillé : modifier
// une adresse de destination ne nécessite pas de redémarrer le service.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapReverseProxy();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ApiGateway" }));

app.Run();