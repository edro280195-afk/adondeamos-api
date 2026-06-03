namespace Adondeamos.Application.Abstractions;

/// <summary>
/// Almacenamiento de fotos. Las implementaciones soportan almacenamiento local (dev)
/// o S3-compatible (Cloudflare R2, AWS S3, MinIO) según la configuración.
/// </summary>
public interface IPhotoStorage
{
    /// <summary>
    /// Sube una foto y regresa la URL pública. El key es el path dentro del bucket/carpeta
    /// (ej. "saves/{saveId}/cover.jpg"). El stream no se cierra aquí.
    /// </summary>
    Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Elimina una foto. No lanza error si ya no existe.</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extrae la clave (key) a partir de una URL pública generada por esta implementación.
    /// Devuelve null si la URL no corresponde a este almacenamiento.
    /// </summary>
    string? TryGetKeyFromUrl(string url);
}
