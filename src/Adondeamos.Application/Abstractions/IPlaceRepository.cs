using Adondeamos.Domain.Entities;

namespace Adondeamos.Application.Abstractions;

public interface IPlaceRepository
{
    void Add(Place place);

    Task<Place?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Busca el lugar por su google_place_id (índice único parcial: nunca se duplica).</summary>
    Task<Place?> GetByGooglePlaceIdAsync(string googlePlaceId, CancellationToken cancellationToken = default);
}
