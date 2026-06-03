using Adondeamos.Application.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adondeamos.Infrastructure.Storage;

/// <summary>
/// Almacenamiento de fotos en el sistema de archivos local. SOLO para desarrollo.
/// Sirve los archivos estáticamente vía UseStaticFiles en wwwroot/uploads.
/// En producción usa S3PhotoStorage.
/// </summary>
public sealed class LocalPhotoStorage : IPhotoStorage
{
    private readonly StorageOptions _options;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<LocalPhotoStorage> _logger;

    public LocalPhotoStorage(
        IOptions<StorageOptions> options,
        IWebHostEnvironment env,
        ILogger<LocalPhotoStorage> logger)
    {
        _options = options.Value;
        _env = env;
        _logger = logger;
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        // Construye la ruta absoluta del archivo.
        var basePath = Path.IsPathRooted(_options.Local.BasePath)
            ? _options.Local.BasePath
            : Path.Combine(_env.ContentRootPath, _options.Local.BasePath);

        var filePath = Path.Combine(basePath, key.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetDirectoryName(filePath)!;

        Directory.CreateDirectory(directory);

        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fileStream, cancellationToken);

        // URL pública: {host}{PublicUrlPath}/{key}
        var publicUrl = $"{_options.Local.PublicUrlPath}/{key.Replace(Path.DirectorySeparatorChar, '/')}";

        _logger.LogInformation("[LocalStorage] Foto guardada en {Path}. URL relativa: {Url}", filePath, publicUrl);

        return publicUrl;
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var basePath = Path.IsPathRooted(_options.Local.BasePath)
            ? _options.Local.BasePath
            : Path.Combine(_env.ContentRootPath, _options.Local.BasePath);

        var filePath = Path.Combine(basePath, key.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("[LocalStorage] Foto eliminada: {Path}", filePath);
        }

        return Task.CompletedTask;
    }

    public string? TryGetKeyFromUrl(string url)
    {
        var prefix = _options.Local.PublicUrlPath.TrimEnd('/') + '/';
        if (!url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return url[prefix.Length..];
    }
}
