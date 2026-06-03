namespace Adondeamos.Application.DTOs.Saves;

/// <summary>Resultado de subir o reemplazar la foto de portada de un guardado.</summary>
public sealed record PhotoUploadResponse(Guid SaveId, string ThumbnailUrl);
