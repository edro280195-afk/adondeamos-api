using Adondeamos.Domain.Entities;

namespace Adondeamos.Application.Abstractions;

public interface IDecisionRepository
{
    void AddSession(DecisionSession session);
    void AddOption(DecisionOption option);
    void AddVote(Vote vote);
    void AddMatch(DecisionMatch match);

    Task<DecisionSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Carga la sesión con sus opciones (lugar incluido) y los votos de cada opción.</summary>
    Task<DecisionSession?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DecisionOption?> GetOptionAsync(Guid sessionId, Guid optionId, CancellationToken cancellationToken = default);

    Task<Vote?> GetVoteAsync(Guid optionId, Guid userId, CancellationToken cancellationToken = default);

    Task<List<Guid>> GetOptionPlaceIdsAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<List<Guid>> GetMatchedPlaceIdsAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<List<Guid>> GetGroupMemberUserIdsAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>Place_ids de los guardados pendientes de un conjunto de usuarios (para auto-llenar opciones).</summary>
    Task<List<Guid>> GetPendingPlaceIdsForUsersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default);

    /// <summary>De una lista de place_ids, devuelve los que sí existen.</summary>
    Task<List<Guid>> FilterExistingPlaceIdsAsync(IReadOnlyCollection<Guid> placeIds, CancellationToken cancellationToken = default);
}
