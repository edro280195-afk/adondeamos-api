namespace Adondeamos.Application.DTOs.Decisions;

/// <summary>Voto de un participante sobre una opción.</summary>
public sealed record VoteResponse(Guid UserId, bool IsYes, DateTime CreatedAt);
