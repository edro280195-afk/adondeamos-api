namespace Adondeamos.Domain.Entities;

/// <summary>Lugar candidato dentro de una sesión de decisión. Tabla <c>decision_options</c>.</summary>
public class DecisionOption
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid PlaceId { get; set; }

    public DecisionSession Session { get; set; } = null!;
    public Place Place { get; set; } = null!;
    public ICollection<Vote> Votes { get; set; } = [];
}
