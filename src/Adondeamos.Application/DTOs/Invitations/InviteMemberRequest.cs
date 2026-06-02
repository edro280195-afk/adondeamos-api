namespace Adondeamos.Application.DTOs.Invitations;

/// <summary>Invita a un usuario a un grupo: por correo o por id de usuario (uno de los dos).</summary>
public sealed record InviteMemberRequest(string? Email, Guid? UserId);
