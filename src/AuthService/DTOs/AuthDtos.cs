namespace AuthService.DTOs;

public record RegisterRequest(string Email, string Password, string FullName);
public record RegisterResponse(string Id, string Email, string FullName);
public record LoginRequest(string Email, string Password);
public record AssignRoleRequest(string Role);
public record AuthResponse(string AccessToken, DateTime ExpiresAt, string Email, string FullName);