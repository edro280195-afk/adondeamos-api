using System.Security.Cryptography;
using Adondeamos.Application.Abstractions;
using Adondeamos.Application.Common.Exceptions;
using Adondeamos.Application.Common.Options;
using Adondeamos.Application.DTOs.Auth;
using Adondeamos.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Adondeamos.Application.Services;

/// <summary>
/// Generación, envío y verificación de tokens de confirmación de correo.
/// Separado de <see cref="AuthService"/> para mantener responsabilidades acotadas.
/// </summary>
public sealed class EmailConfirmationService
{
    private const int TokenBytesLength = 32;         // 256 bits -> 43 chars base64url
    private static readonly TimeSpan TokenClockSkew = TimeSpan.FromSeconds(30);

    private readonly IUserRepository _users;
    private readonly IEmailVerificationTokenRepository _tokens;
    private readonly IEmailSender _emailSender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuthOptions _authOptions;
    private readonly AppOptions _appOptions;
    private readonly IValidator<ConfirmEmailRequest> _confirmValidator;
    private readonly IValidator<ResendConfirmationRequest> _resendValidator;

    public EmailConfirmationService(
        IUserRepository users,
        IEmailVerificationTokenRepository tokens,
        IEmailSender emailSender,
        IUnitOfWork unitOfWork,
        IOptions<AuthOptions> authOptions,
        IOptions<AppOptions> appOptions,
        IValidator<ConfirmEmailRequest> confirmValidator,
        IValidator<ResendConfirmationRequest> resendValidator)
    {
        _users = users;
        _tokens = tokens;
        _emailSender = emailSender;
        _unitOfWork = unitOfWork;
        _authOptions = authOptions.Value;
        _appOptions = appOptions.Value;
        _confirmValidator = confirmValidator;
        _resendValidator = resendValidator;
    }

    /// <summary>
    /// Genera un token de confirmación, lo persiste (solo el hash) y envía el correo.
    /// Si <see cref="AuthOptions.AutoConfirmEmail"/> está activo, el usuario ya llega con
    /// email_confirmed=true y el token se genera igualmente para que el flujo esté completo.
    /// </summary>
    public async Task GenerateAndSendAsync(User user, CancellationToken cancellationToken = default)
    {
        // Invalida tokens anteriores del mismo usuario antes de crear uno nuevo.
        await _tokens.ConsumeAllPendingForUserAsync(user.Id, cancellationToken);

        var (tokenClear, tokenHash) = GenerateToken();
        var expiration = DateTime.UtcNow.AddHours(_authOptions.ConfirmationTokenExpirationHours);

        var tokenEntity = new EmailVerificationToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = expiration
        };

        _tokens.Add(tokenEntity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var link = BuildConfirmationLink(tokenClear);
        await _emailSender.SendConfirmationAsync(user.Email, user.Name, link, cancellationToken);
    }

    /// <summary>
    /// Valida el token en claro del link de confirmación, marca el email como confirmado
    /// y consume el token (uno solo uso).
    /// </summary>
    public async Task ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default)
    {
        await _confirmValidator.ValidateAndThrowAsync(request, cancellationToken);

        var hash = HashToken(request.Token.Trim());
        var token = await _tokens.GetValidByHashAsync(hash, cancellationToken)
            ?? throw new NotFoundException("El token de confirmación no es válido, ya fue usado o expiró.");

        if (token.ExpiresAt < DateTime.UtcNow - TokenClockSkew)
        {
            throw new NotFoundException("El token de confirmación no es válido, ya fue usado o expiró.");
        }

        var user = await _users.GetByIdAsync(token.UserId, cancellationToken)
            ?? throw new NotFoundException("El usuario asociado al token no existe.");

        user.EmailConfirmed = true;
        token.ConsumedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Genera un nuevo token y reenvía el correo. No debe revelar si el email existe (responde
    /// igual aunque no se encuentre, para evitar enumeración de usuarios).
    /// </summary>
    public async Task ResendAsync(ResendConfirmationRequest request, CancellationToken cancellationToken = default)
    {
        await _resendValidator.ValidateAndThrowAsync(request, cancellationToken);

        var user = await _users.GetByEmailAsync(request.Email.Trim(), cancellationToken);

        // Si no existe o ya está confirmado, respondemos sin error (evita enumeración).
        if (user is null || user.EmailConfirmed)
        {
            return;
        }

        await GenerateAndSendAsync(user, cancellationToken);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────────

    private static (string clear, string hash) GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenBytesLength);
        // Base64Url sin padding: URL-safe y apto para query string.
        var clear = Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        return (clear, HashToken(clear));
    }

    internal static string HashToken(string tokenClear)
    {
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(tokenClear));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private string BuildConfirmationLink(string tokenClear)
    {
        var baseUrl = _appOptions.ConfirmEmailUrlBase.TrimEnd('/');
        return $"{baseUrl}/auth/confirm-email?token={Uri.EscapeDataString(tokenClear)}";
    }
}
