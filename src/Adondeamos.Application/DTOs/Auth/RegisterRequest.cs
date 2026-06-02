namespace Adondeamos.Application.DTOs.Auth;

/// <summary>Datos para registrar un usuario.</summary>
public sealed record RegisterRequest(string Name, string Email, string Password);
