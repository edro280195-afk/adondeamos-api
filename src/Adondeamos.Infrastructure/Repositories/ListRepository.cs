using Adondeamos.Application.Abstractions;
using Adondeamos.Domain.Entities;
using Adondeamos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Adondeamos.Infrastructure.Repositories;

public sealed class ListRepository : IListRepository
{
    private readonly AdondeamosDbContext _context;

    public ListRepository(AdondeamosDbContext context) => _context = context;

    public void Add(List list) => _context.Lists.Add(list);

    public void AddItem(ListItem item) => _context.ListItems.Add(item);

    public void RemoveItem(ListItem item) => _context.ListItems.Remove(item);

    public Task<List?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Lists.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public Task<List?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Lists
            .Include(l => l.Items.OrderBy(i => i.Position))
            .ThenInclude(i => i.Save)
            .ThenInclude(s => s.Place)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public Task<List<List>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.Lists
            .Where(l => l.OwnerId == userId
                || (l.GroupId != null && _context.GroupMembers.Any(m => m.GroupId == l.GroupId && m.UserId == userId)))
            .OrderByDescending(l => l.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public Task<ListItem?> GetItemAsync(Guid listId, Guid saveId, CancellationToken cancellationToken = default) =>
        _context.ListItems.FirstOrDefaultAsync(i => i.ListId == listId && i.SaveId == saveId, cancellationToken);

    public Task<bool> ItemExistsAsync(Guid listId, Guid saveId, CancellationToken cancellationToken = default) =>
        _context.ListItems.AnyAsync(i => i.ListId == listId && i.SaveId == saveId, cancellationToken);

    public async Task<int> GetNextPositionAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        var maxPosition = await _context.ListItems
            .Where(i => i.ListId == listId)
            .Select(i => (int?)i.Position)
            .MaxAsync(cancellationToken);

        return (maxPosition ?? -1) + 1;
    }
}
