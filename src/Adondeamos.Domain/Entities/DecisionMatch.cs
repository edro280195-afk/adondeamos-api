namespace Adondeamos.Domain.Entities;

/// <summary>
/// Lugar donde todos los participantes coincidieron (el plan que se hizo).
/// Tabla <c>decision_matches</c> (PK compuesta session_id + place_id).
/// </summary>
public class DecisionMatch
{
    public Guid SessionId { get; set; }
    public Guid PlaceId { get; set; }
    public DateTime MatchedAt { get; set; }
}
