using Adondeamos.Domain.Enums;

namespace Adondeamos.Domain.Entities;

/// <summary>Guardado de un lugar. Pertenece SIEMPRE a un usuario. Tabla <c>saves</c>.</summary>
public class Save
{
    public Guid Id { get; set; }

    /// <summary>Dueño del guardado.</summary>
    public Guid UserId { get; set; }

    public Guid PlaceId { get; set; }

    public SocialNetwork SourceNetwork { get; set; }

    /// <summary>Enlace del post/video que vio el usuario.</summary>
    public string? SourceUrl { get; set; }

    /// <summary>URL de la miniatura (el archivo vive en R2/S3, aquí solo la URL).</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Nota personal ("ir un viernes", "pedir los de pastor").</summary>
    public string? Note { get; set; }

    public ContentVisibility Visibility { get; set; }
    public SaveStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Cuándo se marcó como visitado.</summary>
    public DateTime? VisitedAt { get; set; }

    public Place Place { get; set; } = null!;
}
