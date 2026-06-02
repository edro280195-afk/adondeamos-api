namespace Adondeamos.Domain.Entities;

/// <summary>Voto de un usuario sobre una opción. Tabla <c>votes</c>.</summary>
public class Vote
{
    public Guid Id { get; set; }
    public Guid OptionId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Swipe: true = sí, false = no.</summary>
    public bool IsYes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DecisionOption Option { get; set; } = null!;
}
