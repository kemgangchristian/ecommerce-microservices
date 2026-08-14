using AuthService.DTOs;
using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (
            RegisterRequest request,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName
            };
            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return Results.BadRequest(result.Errors.Select(e => e.Description));

            await userManager.AddToRoleAsync(user, "Customer");

            return Results.Created($"/api/auth/users/{user.Id}",
                new RegisterResponse(user.Id, user.Email!, user.FullName));
        });

        group.MapPost("/login", async (
            LoginRequest request,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtTokenService jwtTokenService) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
                return Results.Unauthorized();

            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
                return Results.Unauthorized();

            var roles = await userManager.GetRolesAsync(user);
            var (token, expiresAt) = jwtTokenService.GenerateToken(user, roles);

            return Results.Ok(new AuthResponse(token, expiresAt, user.Email!, user.FullName));
        });

        group.MapGet("/me", (System.Security.Claims.ClaimsPrincipal user) =>
        {
            var email = user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                        ?? user.FindFirst("email")?.Value;
            return Results.Ok(new { email, claims = user.Claims.Select(c => new { c.Type, c.Value }) });
        }).RequireAuthorization();

        group.MapPost("/users/{id}/roles", async (
            string id,
            AssignRoleRequest request,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager) =>
        {
            if (!await roleManager.RoleExistsAsync(request.Role))
                return Results.BadRequest($"Le role '{request.Role}' n'existe pas.");

            var user = await userManager.FindByIdAsync(id);
            if (user is null) return Results.NotFound();

            var currentRoles = await userManager.GetRolesAsync(user);
            if (currentRoles.Count > 0)
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                    return Results.BadRequest(removeResult.Errors.Select(e => e.Description));
            }

            var addResult = await userManager.AddToRoleAsync(user, request.Role);
            if (!addResult.Succeeded)
                return Results.BadRequest(addResult.Errors.Select(e => e.Description));

            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}