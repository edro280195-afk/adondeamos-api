using Adondeamos.Application.Abstractions;
using Adondeamos.Domain.Entities;
using Adondeamos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Adondeamos.Infrastructure.Repositories;

public sealed class GroupRepository : IGroupRepository
{
    private readonly AdondeamosDbContext _context;

    public GroupRepository(AdondeamosDbContext context) => _context = context;

    public void Add(Group group) => _context.Groups.Add(group);

    public void AddMember(GroupMember member) => _context.GroupMembers.Add(member);

    public Task<Group?> GetDetailAsync(Guid groupId, CancellationToken cancellationToken = default) =>
        _context.Groups
            .Include(g => g.Members)
            .ThenInclude(m => m.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

    public Task<List<GroupMember>> GetMembershipsForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.GroupMembers
            .Include(m => m.Group)
            .Where(m => m.UserId == userId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public Task<bool> IsMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default) =>
        _context.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == userId, cancellationToken);
}
