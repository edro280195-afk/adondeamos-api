using Adondeamos.Domain.Enums;

namespace Adondeamos.Application.DTOs.Saves;

/// <summary>
/// Flujo de captura: guarda un lugar (ya resuelto vía /places/resolve o /places) para el usuario.
/// SourceNetwork por defecto 'manual' y Visibility por defecto 'private' si no se envían.
/// </summary>
public sealed record CreateSaveRequest(
    Guid PlaceId,
    SocialNetwork? SourceNetwork,
    string? SourceUrl,
    string? ThumbnailUrl,
    string? Note,
    ContentVisibility? Visibility);
