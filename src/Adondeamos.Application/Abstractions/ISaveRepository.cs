using Adondeamos.Domain.Entities;
using Adondeamos.Domain.Enums;

namespace Adondeamos.Application.Abstractions;

public interface ISaveRepository
{
    void Add(Save save);

    void Remove(Save save);

    Task<Save?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Carga el guardado con su lugar (para responder o actualizar).</summary>
    Task<Save?> GetByIdWithPlaceAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsForUserAndPlaceAsync(Guid userId, Guid placeId, CancellationToken cancellationToken = default);

    /// <summary>Guardados del usuario, con filtros opcionales por estado y por lista.</summary>
    Task<List<Save>> GetForUserAsync(Guid userId, SaveStatus? status, Guid? listId, CancellationToken cancellationToken = default);
}
