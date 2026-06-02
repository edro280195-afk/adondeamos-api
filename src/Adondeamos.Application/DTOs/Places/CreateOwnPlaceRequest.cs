namespace Adondeamos.Application.DTOs.Places;

/// <summary>Crea un lugar propio (origin='own') cuando no está en Google.</summary>
public sealed record CreateOwnPlaceRequest(string Name, decimal Latitude, decimal Longitude, string? City);
