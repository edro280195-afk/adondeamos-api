namespace Adondeamos.Infrastructure.Storage;

/// <summary>Opciones de almacenamiento de fotos. Sección "Storage".</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public S3StorageOptions S3 { get; set; } = new();
    public LocalStorageOptions Local { get; set; } = new();
}

/// <summary>
/// Configuración para almacenamiento S3-compatible.
/// Válido para Cloudflare R2, AWS S3, MinIO y cualquier implementación compatible.
/// </summary>
public sealed class S3StorageOptions
{
    /// <summary>
    /// URL del endpoint S3. Para Cloudflare R2:
    /// https://{account-id}.r2.cloudflarestorage.com
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    public string Bucket { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;

    /// <summary>
    /// URL base pública para los archivos. Puede ser el dominio público del bucket
    /// (ej. https://pub-xxxx.r2.dev) o un dominio personalizado (ej. https://cdn.adondeamos.app).
    /// </summary>
    public string PublicUrlBase { get; set; } = string.Empty;

    /// <summary>True si todas las credenciales necesarias están presentes.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint)
        && !string.IsNullOrWhiteSpace(Bucket)
        && !string.IsNullOrWhiteSpace(AccessKeyId)
        && !string.IsNullOrWhiteSpace(SecretAccessKey)
        && !string.IsNullOrWhiteSpace(PublicUrlBase);
}

/// <summary>Configuración para almacenamiento local (solo dev).</summary>
public sealed class LocalStorageOptions
{
    /// <summary>Carpeta donde se guardan los archivos. Por defecto: wwwroot/uploads.</summary>
    public string BasePath { get; set; } = "wwwroot/uploads";

    /// <summary>URL base pública servida por UseStaticFiles. Por defecto: /uploads.</summary>
    public string PublicUrlPath { get; set; } = "/uploads";
}
