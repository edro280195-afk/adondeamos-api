namespace Adondeamos.Application.DTOs.Decisions;

/// <summary>
/// Estado de una sesión de decisión: participantes, opciones con sus votos y los lugares que
/// ya hicieron match (todos los participantes votaron sí), registrados en decision_matches.
/// </summary>
public sealed record DecisionDetailResponse(
    Guid Id,
    Guid? GroupId,
    Guid CreatedBy,
    string? Context,
    DateTime CreatedAt,
    IReadOnlyList<Guid> Participants,
    IReadOnlyList<DecisionOptionResponse> Options,
    IReadOnlyList<Guid> MatchedPlaceIds);
