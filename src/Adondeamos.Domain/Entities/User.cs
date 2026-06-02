namespace Adondeamos.Domain.Entities;

/// <summary>Usuario de la aplicación. Tabla <c>users</c>.</summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>Nombre visible.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Correo de acceso (único sin importar mayúsculas).</summary>
    public string Email { get; set; } = null!;

    /// <summary>Hash de la contraseña. Nulo si entra solo con login social.</summary>
    public string? PasswordHash { get; set; }

    /// <summary>URL de la foto de perfil (el archivo vive en R2/S3, aquí solo la URL).</summary>
    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
