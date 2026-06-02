using Adondeamos.Application.Abstractions;
using Adondeamos.Domain.Entities;
using Adondeamos.Domain.Enums;
using Adondeamos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Adondeamos.Infrastructure.Repositories;

public sealed class InvitationRepository : IInvitationRepository
{
    private readonly AdondeamosDbContext _context;

    public InvitationRepository(AdondeamosDbContext context) => _context = context;

    public void Add(GroupInvitation invitation) => _context.GroupInvitations.Add(invitation);

    public Task<GroupInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GroupInvitations
            .Include(i => i.Group)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<List<GroupInvitation>> GetPendingForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.GroupInvitations
            .Include(i => i.Group)
            .Where(i => i.InvitedUser == userId && i.Status == InvitationStatus.Pending)
            .OrderByDescending(i => i.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public Task<bool> HasPendingAsync(Guid groupId, Guid invitedUserId, CancellationToken cancellationToken = default) =>
        _context.GroupInvitations.AnyAsync(
            i => i.GroupId == groupId && i.InvitedUser == invitedUserId && i.Status == InvitationStatus.Pending,
            cancellationToken);
}
