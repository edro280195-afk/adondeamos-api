namespace Adondeamos.Application.DTOs.Groups;

/// <summary>Agrega un miembro al grupo: por correo o por id de usuario (uno de los dos).</summary>
public sealed record AddMemberRequest(string? Email, Guid? UserId);
