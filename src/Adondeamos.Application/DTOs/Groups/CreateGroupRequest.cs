namespace Adondeamos.Application.DTOs.Groups;

/// <summary>Datos para crear un grupo. El creador queda como owner.</summary>
public sealed record CreateGroupRequest(string Name);
