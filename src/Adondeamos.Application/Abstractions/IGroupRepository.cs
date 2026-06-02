using Adondeamos.Domain.Entities;

namespace Adondeamos.Application.Abstractions;

public interface IGroupRepository
{
    void Add(Group group);

    void AddMember(GroupMember member);

    /// <summary>Carga el grupo con sus miembros (incluye el usuario de cada miembro).</summary>
    Task<Group?> GetDetailAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>Pertenencias del usuario, incluyendo el grupo de cada una (para listar sus grupos).</summary>
    Task<List<GroupMember>> GetMembershipsForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> IsMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default);
}
