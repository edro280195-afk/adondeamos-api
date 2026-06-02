using Adondeamos.Domain.Entities;

namespace Adondeamos.Application.Abstractions;

public interface IInvitationRepository
{
    void Add(GroupInvitation invitation);

    /// <summary>Carga la invitación con su grupo (rastreada, para responder o actualizar su estado).</summary>
    Task<GroupInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Invitaciones pendientes dirigidas a un usuario (incluye el grupo).</summary>
    Task<List<GroupInvitation>> GetPendingForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> HasPendingAsync(Guid groupId, Guid invitedUserId, CancellationToken cancellationToken = default);
}
