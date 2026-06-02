namespace Adondeamos.Domain.Entities;

/// <summary>Item de una lista: vincula un guardado con una lista. Tabla <c>list_items</c> (PK compuesta).</summary>
public class ListItem
{
    public Guid ListId { get; set; }
    public Guid SaveId { get; set; }

    /// <summary>Quién lo agregó (relevante en listas de grupo).</summary>
    public Guid? AddedBy { get; set; }

    /// <summary>Orden manual dentro de la lista.</summary>
    public int Position { get; set; }

    public DateTime AddedAt { get; set; }

    public List List { get; set; } = null!;
    public Save Save { get; set; } = null!;
}
