using Adondeamos.Domain.Enums;

namespace Adondeamos.Domain.Entities;

/// <summary>
/// Lista de lugares, personal o de grupo. Tabla <c>lists</c>.
/// Si <see cref="GroupId"/> es nulo, es personal; si tiene valor, es la bóveda compartida del grupo.
/// </summary>
public class List
{
    public Guid Id { get; set; }

    /// <summary>Quién creó la lista.</summary>
    public Guid OwnerId { get; set; }

    /// <summary>Nulo = lista personal; con valor = lista de grupo.</summary>
    public Guid? GroupId { get; set; }

    /// <summary>Nombre, ej. "Quiero ir", "Citas", "Antojos".</summary>
    public string Name { get; set; } = null!;

    public ContentVisibility Visibility { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ListItem> Items { get; set; } = [];
}
