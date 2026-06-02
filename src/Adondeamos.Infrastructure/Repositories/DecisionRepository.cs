using Adondeamos.Application.Abstractions;
using Adondeamos.Domain.Entities;
using Adondeamos.Domain.Enums;
using Adondeamos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Adondeamos.Infrastructure.Repositories;

public sealed class DecisionRepository : IDecisionRepository
{
    private readonly AdondeamosDbContext _context;

    public DecisionRepository(AdondeamosDbContext context) => _context = context;

    public void AddSession(DecisionSession session) => _context.DecisionSessions.Add(session);

    public void AddOption(DecisionOption option) => _context.DecisionOptions.Add(option);

    public void AddVote(Vote vote) => _context.Votes.Add(vote);

    public void AddMatch(DecisionMatch match) => _context.DecisionMatches.Add(match);

    public Task<DecisionSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.DecisionSessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<DecisionSession?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.DecisionSessions
            .Include(s => s.Options).ThenInclude(o => o.Place)
            .Include(s => s.Options).ThenInclude(o => o.Votes)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<DecisionOption?> GetOptionAsync(Guid sessionId, Guid optionId, CancellationToken cancellationToken = default) =>
        _context.DecisionOptions
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.SessionId == sessionId && o.Id == optionId, cancellationToken);

    public Task<Vote?> GetVoteAsync(Guid optionId, Guid userId, CancellationToken cancellationToken = default) =>
        _context.Votes.FirstOrDefaultAsync(v => v.OptionId == optionId && v.UserId == userId, cancellationToken);

    public Task<List<Guid>> GetOptionPlaceIdsAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        _context.DecisionOptions
            .Where(o => o.SessionId == sessionId)
            .Select(o => o.PlaceId)
            .ToListAsync(cancellationToken);

    public Task<List<Guid>> GetMatchedPlaceIdsAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        _context.DecisionMatches
            .Where(m => m.SessionId == sessionId)
            .Select(m => m.PlaceId)
            .ToListAsync(cancellationToken);

    public Task<List<Guid>> GetGroupMemberUserIdsAsync(Guid groupId, CancellationToken cancellationToken = default) =>
        _context.GroupMembers
            .Where(m => m.GroupId == groupId)
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken);

    public Task<List<Guid>> GetPendingPlaceIdsForUsersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default) =>
        _context.Saves
            .Where(s => userIds.Contains(s.UserId) && s.Status == SaveStatus.Pending)
            .Select(s => s.PlaceId)
            .Distinct()
            .ToListAsync(cancellationToken);

    public Task<List<Guid>> FilterExistingPlaceIdsAsync(IReadOnlyCollection<Guid> placeIds, CancellationToken cancellationToken = default) =>
        _context.Places
            .Where(p => placeIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
}
