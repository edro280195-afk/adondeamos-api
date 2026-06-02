namespace Adondeamos.Application.DTOs.Decisions;

/// <summary>Inicia una sesión para decidir. Sin <c>GroupId</c> es en solitario; con valor es de grupo.</summary>
public sealed record CreateDecisionRequest(Guid? GroupId, string? Context);
