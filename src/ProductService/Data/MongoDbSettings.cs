namespace ProductService.Data;

/// <summary>
/// Options de configuration pour la connexion à MongoDB.
/// Ces valeurs sont typiquement liées à la section "MongoDbSettings" de la configuration
/// (appsettings.json, variables d'environnement, secrets, etc.) via le pattern Options.
/// </summary>
public class MongoDbSettings
{
    /// <summary>
    /// Chaîne de connexion vers l'instance/cluster MongoDB.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Nom de la base de données MongoDB utilisée par le service.
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;
}
