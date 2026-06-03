using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Adondeamos.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adondeamos.Infrastructure.Storage;

/// <summary>
/// Almacenamiento de fotos compatible con S3 (Cloudflare R2, AWS S3, MinIO).
/// Para Cloudflare R2:
///   Storage:S3:Endpoint   = https://{account-id}.r2.cloudflarestorage.com
///   Storage:S3:Bucket     = nombre-del-bucket
///   Storage:S3:AccessKeyId     = clave desde R2 › Manage API tokens
///   Storage:S3:SecretAccessKey = secreto desde R2 › Manage API tokens
///   Storage:S3:PublicUrlBase   = https://pub-xxxx.r2.dev  (o tu dominio personalizado)
/// </summary>
public sealed class S3PhotoStorage : IPhotoStorage
{
    private readonly S3StorageOptions _s3;
    private readonly IAmazonS3 _client;
    private readonly ILogger<S3PhotoStorage> _logger;

    public S3PhotoStorage(IOptions<StorageOptions> options, ILogger<S3PhotoStorage> logger)
    {
        _s3 = options.Value.S3;
        _logger = logger;

        // Cloudflare R2 y otros proveedores S3-compatibles requieren una región ficticia
        // y un ServiceURL personalizado para que AWSSDK use el endpoint correcto.
        var config = new AmazonS3Config
        {
            ServiceURL = _s3.Endpoint,
            ForcePathStyle = true,       // Cloudflare R2 requiere path-style
            AuthenticationRegion = "auto" // R2 acepta "auto"; S3 real usaría la región real
        };

        var credentials = new BasicAWSCredentials(_s3.AccessKeyId, _s3.SecretAccessKey);
        _client = new AmazonS3Client(credentials, config);
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _s3.Bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            // Los archivos son públicos: se leen desde la CDN pública del bucket.
            CannedACL = S3CannedACL.PublicRead
        };

        await _client.PutObjectAsync(request, cancellationToken);

        var publicUrl = $"{_s3.PublicUrlBase.TrimEnd('/')}/{key}";
        _logger.LogInformation("[S3] Foto subida: {Bucket}/{Key}. URL: {Url}", _s3.Bucket, key, publicUrl);

        return publicUrl;
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _s3.Bucket,
                Key = key
            };
            await _client.DeleteObjectAsync(request, cancellationToken);
            _logger.LogInformation("[S3] Foto eliminada: {Bucket}/{Key}", _s3.Bucket, key);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // No existe, nada que hacer.
            _logger.LogWarning("[S3] Intento de eliminar archivo inexistente: {Key}", key);
        }
    }

    public string? TryGetKeyFromUrl(string url)
    {
        var prefix = _s3.PublicUrlBase.TrimEnd('/') + '/';
        if (!url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return url[prefix.Length..];
    }
}
