using Adondeamos.Application.Abstractions;
using Adondeamos.Application.Common.Exceptions;
using Adondeamos.Application.DTOs.Decisions;
using Adondeamos.Application.DTOs.Places;
using Adondeamos.Domain.Entities;
using FluentValidation;

namespace Adondeamos.Application.Services;

/// <summary>
/// Decidir a dónde ir (solo o en grupo). Una opción hace match cuando TODOS los participantes
/// votaron sí; al detectarlo se registra en decision_matches.
/// </summary>
public sealed class DecisionService
{
    private readonly IDecisionRepository _decisions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateDecisionRequest> _createValidator;
    private readonly IValidator<AddOptionsRequest> _addOptionsValidator;

    public DecisionService(
        IDecisionRepository decisions,
        IUnitOfWork unitOfWork,
        IValidator<CreateDecisionRequest> createValidator,
        IValidator<AddOptionsRequest> addOptionsValidator)
    {
        _decisions = decisions;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _addOptionsValidator = addOptionsValidator;
    }

    public async Task<DecisionDetailResponse> CreateDecisionAsync(Guid userId, CreateDecisionRequest request, CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (request.GroupId is Guid groupId)
        {
            var members = await _decisions.GetGroupMemberUserIdsAsync(groupId, cancellationToken);
            if (!members.Contains(userId))
            {
                throw new ForbiddenException("No perteneces al grupo indicado.");
            }
        }

        var session = new DecisionSession
        {
            GroupId = request.GroupId,
            CreatedBy = userId,
            Context = string.IsNullOrWhiteSpace(request.Context) ? null : request.Context.Trim()
        };

        _decisions.AddSession(session);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await BuildDetailAsync(session.Id, userId, cancellationToken);
    }

    public async Task<DecisionDetailResponse> AddOptionsAsync(Guid userId, Guid sessionId, AddOptionsRequest request, CancellationToken cancellationToken = default)
    {
        await _addOptionsValidator.ValidateAndThrowAsync(request, cancellationToken);

        var session = await _decisions.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new NotFoundException("Sesión de decisión no encontrada.");

        var participants = await GetParticipantsAsync(session, cancellationToken);
        EnsureParticipant(participants, userId);

        var candidates = new HashSet<Guid>(request.PlaceIds ?? []);
        if (request.AutoFillFromSaves)
        {
            var pending = await _decisions.GetPendingPlaceIdsForUsersAsync(participants, cancellationToken);
            candidates.UnionWith(pending);
        }

        if (candidates.Count > 0)
        {
            // Verifica que todos los lugares existan (evita violar la llave foránea).
            var existing = await _decisions.FilterExistingPlaceIdsAsync(candidates, cancellationToken);
            if (existing.Count != candidates.Count)
            {
                throw new NotFoundException("Uno o más lugares no existen.");
            }

            var alreadyOptions = (await _decisions.GetOptionPlaceIdsAsync(sessionId, cancellationToken)).ToHashSet();
            var toAdd = candidates.Where(placeId => !alreadyOptions.Contains(placeId)).ToList();

            foreach (var placeId in toAdd)
            {
                _decisions.AddOption(new DecisionOption { SessionId = sessionId, PlaceId = placeId });
            }

            if (toAdd.Count > 0)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        return await BuildDetailAsync(sessionId, userId, cancellationToken);
    }

    public async Task<DecisionDetailResponse> CastVoteAsync(Guid userId, Guid sessionId, Guid optionId, CastVoteRequest request, CancellationToken cancellationToken = default)
    {
        var session = await _decisions.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new NotFoundException("Sesión de decisión no encontrada.");

        var participants = await GetParticipantsAsync(session, cancellationToken);
        EnsureParticipant(participants, userId);

        var option = await _decisions.GetOptionAsync(sessionId, optionId, cancellationToken)
            ?? throw new NotFoundException("Opción no encontrada en esta sesión.");

        var existingVote = await _decisions.GetVoteAsync(option.Id, userId, cancellationToken);
        if (existingVote is null)
        {
            _decisions.AddVote(new Vote { OptionId = option.Id, UserId = userId, IsYes = request.IsYes });
        }
        else
        {
            existingVote.IsYes = request.IsYes;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await BuildDetailAsync(sessionId, userId, cancellationToken);
    }

    public Task<DecisionDetailResponse> GetDecisionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default) =>
        BuildDetailAsync(sessionId, userId, cancellationToken);

    private async Task<DecisionDetailResponse> BuildDetailAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        var session = await _decisions.GetDetailAsync(sessionId, cancellationToken)
            ?? throw new NotFoundException("Sesión de decisión no encontrada.");

        var participants = await GetParticipantsAsync(session, cancellationToken);
        EnsureParticipant(participants, userId);

        var matchedPlaceIds = await RecomputeAndPersistMatchesAsync(session, participants, cancellationToken);

        var options = session.Options
            .Select(option => new DecisionOptionResponse(
                option.Id,
                PlaceResponse.FromEntity(option.Place),
                option.Votes
                    .OrderBy(v => v.CreatedAt)
                    .Select(v => new VoteResponse(v.UserId, v.IsYes, v.CreatedAt))
                    .ToList(),
                matchedPlaceIds.Contains(option.PlaceId)))
            .ToList();

        return new DecisionDetailResponse(
            session.Id,
            session.GroupId,
            session.CreatedBy,
            session.Context,
            session.CreatedAt,
            participants.ToList(),
            options,
            matchedPlaceIds.ToList());
    }

    /// <summary>Participantes: los miembros del grupo (si es de grupo) o solo el creador (en solitario).</summary>
    private async Task<IReadOnlyCollection<Guid>> GetParticipantsAsync(DecisionSession session, CancellationToken cancellationToken)
    {
        if (session.GroupId is Guid groupId)
        {
            return await _decisions.GetGroupMemberUserIdsAsync(groupId, cancellationToken);
        }

        return [session.CreatedBy];
    }

    private static void EnsureParticipant(IReadOnlyCollection<Guid> participants, Guid userId)
    {
        if (!participants.Contains(userId))
        {
            throw new ForbiddenException("No participas en esta decisión.");
        }
    }

    /// <summary>
    /// Calcula los matches (opciones donde todos los participantes votaron sí) y registra en
    /// decision_matches los que aún no estuvieran. Devuelve el conjunto de lugares con match.
    /// </summary>
    private async Task<HashSet<Guid>> RecomputeAndPersistMatchesAsync(DecisionSession session, IReadOnlyCollection<Guid> participants, CancellationToken cancellationToken)
    {
        var liveMatched = new HashSet<Guid>();
        foreach (var option in session.Options)
        {
            var allVotedYes = participants.Count > 0
                && participants.All(participant => option.Votes.Any(vote => vote.UserId == participant && vote.IsYes));

            if (allVotedYes)
            {
                liveMatched.Add(option.PlaceId);
            }
        }

        var persisted = (await _decisions.GetMatchedPlaceIdsAsync(session.Id, cancellationToken)).ToHashSet();
        var toInsert = liveMatched.Where(placeId => !persisted.Contains(placeId)).ToList();

        foreach (var placeId in toInsert)
        {
            _decisions.AddMatch(new DecisionMatch { SessionId = session.Id, PlaceId = placeId });
        }

        if (toInsert.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            persisted.UnionWith(toInsert);
        }

        return persisted;
    }
}
