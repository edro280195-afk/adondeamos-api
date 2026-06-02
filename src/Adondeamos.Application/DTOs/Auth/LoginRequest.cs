namespace Adondeamos.Application.DTOs.Auth;

/// <summary>Credenciales para iniciar sesión.</summary>
public sealed record LoginRequest(string Email, string Password);
