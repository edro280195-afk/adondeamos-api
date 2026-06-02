namespace Adondeamos.Domain.Entities;

/// <summary>Grupo opcional (pareja, familia, amigos) para compartir. Tabla <c>groups</c>.</summary>
public class Group
{
    public Guid Id { get; set; }

    /// <summary>Nombre del grupo, ej. "Eduardo y esposa".</summary>
    public string Name { get; set; } = null!;

    /// <summary>Usuario que creó el grupo. Queda nulo si ese usuario se borra.</summary>
    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Miembros del grupo.</summary>
    public ICollection<GroupMember> Members { get; set; } = [];
}
