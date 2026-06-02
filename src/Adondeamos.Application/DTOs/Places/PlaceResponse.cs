using Adondeamos.Domain.Entities;
using Adondeamos.Domain.Enums;

namespace Adondeamos.Application.DTOs.Places;

/// <summary>
/// Lugar canónico en nuestra base. Para lugares de Google, lo único persistido es el
/// <see cref="GooglePlaceId"/> (nombre/coordenadas/ciudad quedan nulos).
/// </summary>
public sealed record PlaceResponse(
    Guid Id,
    PlaceOrigin Origin,
    string? GooglePlaceId,
    string? Name,
    decimal? Latitude,
    decimal? Longitude,
    string? City,
    DateTime CreatedAt)
{
    public static PlaceResponse FromEntity(Place place) =>
        new(place.Id, place.Origin, place.GooglePlaceId, place.Name, place.Latitude, place.Longitude, place.City, place.CreatedAt);
}
