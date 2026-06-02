using Adondeamos.Domain.Enums;

namespace Adondeamos.Application.DTOs.Saves;

/// <summary>
/// Cambios sobre un guardado (solo se aplican los campos enviados).
/// <c>Visited=true</c> marca como visitado (status='visited' y fija visited_at);
/// <c>Visited=false</c> lo regresa a pendiente.
/// </summary>
public sealed record UpdateSaveRequest(string? Note, ContentVisibility? Visibility, bool? Visited);
