namespace Adondeamos.Application.DTOs.Auth;

/// <summary>Token de acceso emitido tras registro o login, junto con el perfil del usuario.</summary>
public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, UserResponse User);
