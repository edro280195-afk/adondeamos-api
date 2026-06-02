using Adondeamos.Application.Abstractions;
using Adondeamos.Domain.Entities;
using Adondeamos.Domain.Enums;
using Adondeamos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Adondeamos.Infrastructure.Repositories;

public sealed class SaveRepository : ISaveRepository
{
    private readonly AdondeamosDbContext _context;

    public SaveRepository(AdondeamosDbContext context) => _context = context;

    public void Add(Save save) => _context.Saves.Add(save);

    public void Remove(Save save) => _context.Saves.Remove(save);

    public Task<Save?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Saves.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<Save?> GetByIdWithPlaceAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Saves
            .Include(s => s.Place)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> ExistsForUserAndPlaceAsync(Guid userId, Guid placeId, CancellationToken cancellationToken = default) =>
        _context.Saves.AnyAsync(s => s.UserId == userId && s.PlaceId == placeId, cancellationToken);

    public Task<List<Save>> GetForUserAsync(Guid userId, SaveStatus? status, Guid? listId, CancellationToken cancellationToken = default)
    {
        var query = _context.Saves
            .Include(s => s.Place)
            .Where(s => s.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        if (listId.HasValue)
        {
            // Solo los guardados del usuario que están en esa lista.
            query = query.Where(s => _context.ListItems.Any(li => li.ListId == listId.Value && li.SaveId == s.Id));
        }

        return query
            .OrderByDescending(s => s.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
