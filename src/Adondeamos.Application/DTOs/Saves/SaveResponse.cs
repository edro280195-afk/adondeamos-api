using Adondeamos.Application.DTOs.Places;
using Adondeamos.Domain.Entities;
using Adondeamos.Domain.Enums;

namespace Adondeamos.Application.DTOs.Saves;

/// <summary>Guardado del usuario, con el lugar al que apunta.</summary>
public sealed record SaveResponse(
    Guid Id,
    PlaceResponse Place,
    SocialNetwork SourceNetwork,
    string? SourceUrl,
    string? ThumbnailUrl,
    string? Note,
    ContentVisibility Visibility,
    SaveStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? VisitedAt)
{
    public static SaveResponse FromEntity(Save save) =>
        new(
            save.Id,
            PlaceResponse.FromEntity(save.Place),
            save.SourceNetwork,
            save.SourceUrl,
            save.ThumbnailUrl,
            save.Note,
            save.Visibility,
            save.Status,
            save.CreatedAt,
            save.UpdatedAt,
            save.VisitedAt);
}
