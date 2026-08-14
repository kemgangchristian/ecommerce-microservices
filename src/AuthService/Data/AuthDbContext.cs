using AuthService.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data;

/// <summary>
/// Contexte EF Core d'AuthService, backé par PostgreSQL. Hérite
/// d'IdentityDbContext, qui fournit déjà les tables standard Identity
/// (AspNetUsers, AspNetRoles, AspNetUserRoles, etc.) — pas besoin de les
/// définir nous-mêmes.
/// </summary>
public class AuthDbContext : IdentityDbContext<ApplicationUser>
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }
}