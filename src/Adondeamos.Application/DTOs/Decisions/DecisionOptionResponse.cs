using Adondeamos.Application.DTOs.Places;

namespace Adondeamos.Application.DTOs.Decisions;

/// <summary>Lugar candidato dentro de la sesión, con sus votos. <c>IsMatch</c> = todos votaron sí.</summary>
public sealed record DecisionOptionResponse(
    Guid Id,
    PlaceResponse Place,
    IReadOnlyList<VoteResponse> Votes,
    bool IsMatch);
