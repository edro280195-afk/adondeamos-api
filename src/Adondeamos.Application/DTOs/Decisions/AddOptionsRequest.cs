namespace Adondeamos.Application.DTOs.Decisions;

/// <summary>
/// Agrega lugares candidatos a la sesión: por <c>PlaceIds</c> explícitos y/o auto-llenando
/// desde los guardados pendientes de los participantes (<c>AutoFillFromSaves</c>).
/// </summary>
public sealed record AddOptionsRequest(IReadOnlyList<Guid>? PlaceIds, bool AutoFillFromSaves);
