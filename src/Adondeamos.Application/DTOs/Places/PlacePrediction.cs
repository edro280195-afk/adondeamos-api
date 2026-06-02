namespace Adondeamos.Application.DTOs.Places;

/// <summary>Predicción de Autocomplete de Google: lo necesario para que el usuario elija.</summary>
public sealed record PlacePrediction(string PlaceId, string Description, string? MainText, string? SecondaryText);
