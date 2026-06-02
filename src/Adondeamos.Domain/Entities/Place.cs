using Adondeamos.Domain.Enums;

namespace Adondeamos.Domain.Entities;

/// <summary>
/// Lugar (la bisagra de la app). Tabla <c>places</c>.
/// Si <see cref="Origin"/> es <see cref="PlaceOrigin.Google"/>, lo único que se persiste de
/// Google es <see cref="GooglePlaceId"/>. Nunca se guardan nombre/dirección/horarios de Google.
/// </summary>
public class Place
{
    public Guid Id { get; set; }

    public PlaceOrigin Origin { get; set; }

    /// <summary>Único dato de Google que se guarda. Nulo si el lugar es propio.</summary>
    public string? GooglePlaceId { get; set; }

    /// <summary>Obligatorio para lugares propios; nulo para lugares de Google.</summary>
    public string? Name { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? City { get; set; }

    /// <summary>Quién lo dio de alta (relevante en lugares propios).</summary>
    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
