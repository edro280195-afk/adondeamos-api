namespace Adondeamos.Application.DTOs.Auth;

/// <summary>Campos editables del perfil. Solo se actualizan los que vengan con valor.</summary>
public sealed record UpdateMeRequest(string? Name, string? AvatarUrl);
