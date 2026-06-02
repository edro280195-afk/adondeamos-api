using Adondeamos.Domain.Entities;

namespace Adondeamos.Application.DTOs.Auth;

/// <summary>Perfil público del usuario (nunca expone el hash de la contraseña).</summary>
public sealed record UserResponse(Guid Id, string Name, string Email, string? AvatarUrl, DateTime CreatedAt)
{
    public static UserResponse FromEntity(User user) =>
        new(user.Id, user.Name, user.Email, user.AvatarUrl, user.CreatedAt);
}
