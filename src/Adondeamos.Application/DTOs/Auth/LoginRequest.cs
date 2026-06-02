namespace Adondeamos.Application.DTOs.Auth;

/// <summary>Credenciales para iniciar sesión.</summary>
public sealed record LoginRequest(string Username, string Password);
