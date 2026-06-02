namespace Adondeamos.Application.DTOs.Decisions;

/// <summary>Voto del usuario sobre una opción: true = sí, false = no.</summary>
public sealed record CastVoteRequest(bool IsYes);
