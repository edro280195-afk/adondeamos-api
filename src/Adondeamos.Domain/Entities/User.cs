namespace Adondeamos.Domain.Entities;

/// <summary>Usuario de la aplicación. Tabla <c>users</c>.</summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>Nombre de usuario (único y en minúsculas).</summary>
    public string Username { get; set; } = null!;

    /// <summary>Nombre visible.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Correo electrónico. Identidad y recuperación; no es el campo de login.</summary>
    public string Email { get; set; } = null!;

    /// <summary>True cuando el usuario verificó su correo pulsando el link de confirmación.</summary>
    public bool EmailConfirmed { get; set; }

    /// <summary>Hash de la contraseña. Nulo si solo usa login social.</summary>
    public string? PasswordHash { get; set; }

    /// <summary>URL de la foto de perfil (el archivo vive en R2/S3, aquí solo la URL).</summary>
    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
