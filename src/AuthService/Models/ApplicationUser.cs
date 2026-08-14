using Microsoft.AspNetCore.Identity;

namespace AuthService.Models;

/// <summary>
/// Utilisateur de l'application. Étend IdentityUser, qui fournit déjà
/// UserName, Email, PasswordHash (hashé par ASP.NET Core Identity, jamais
/// géré à la main), etc.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}