namespace Adondeamos.Domain.Entities;

/// <summary>
/// Sesión para decidir a dónde ir. Tabla <c>decision_sessions</c>.
/// Si <see cref="GroupId"/> es nulo, es en solitario; si tiene valor, es un match de grupo.
/// </summary>
public class DecisionSession
{
    public Guid Id { get; set; }

    /// <summary>Nulo = en solitario; con valor = sesión de grupo.</summary>
    public Guid? GroupId { get; set; }

    public Guid CreatedBy { get; set; }

    /// <summary>Contexto usado (clima, fecha, presupuesto).</summary>
    public string? Context { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<DecisionOption> Options { get; set; } = [];
}
