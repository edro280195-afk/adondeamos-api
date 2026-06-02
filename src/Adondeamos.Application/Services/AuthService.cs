using Adondeamos.Application.Abstractions;
using Adondeamos.Application.Common.Exceptions;
using Adondeamos.Application.Common.Options;
using Adondeamos.Application.DTOs.Auth;
using Adondeamos.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Adondeamos.Application.Services;

/// <summary>Registro, login y perfil del usuario.</summary>
public sealed class AuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<UpdateMeRequest> _updateMeValidator;
    private readonly EmailConfirmationService _emailConfirmation;
    private readonly AuthOptions _authOptions;

    public AuthService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IUnitOfWork unitOfWork,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<UpdateMeRequest> updateMeValidator,
        EmailConfirmationService emailConfirmation,
        IOptions<AuthOptions> authOptions)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _unitOfWork = unitOfWork;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _updateMeValidator = updateMeValidator;
        _emailConfirmation = emailConfirmation;
        _authOptions = authOptions.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        await _registerValidator.ValidateAndThrowAsync(request, cancellationToken);

        var email = request.Email.Trim();
        var username = request.Username.Trim();

        if (await _users.EmailExistsAsync(email, cancellationToken))
        {
            throw new ConflictException("Ya existe una cuenta con ese correo.");
        }

        if (await _users.UsernameExistsAsync(username, cancellationToken))
        {
            throw new ConflictException("El nombre de usuario ya está en uso.");
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Username = username,
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            // Si AutoConfirmEmail está activo, el correo queda confirmado de inmediato (modo dev/pruebas).
            EmailConfirmed = _authOptions.AutoConfirmEmail
        };

        _users.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Siempre genera y envía el token (en dev lo loguea; en prod lo manda por SMTP).
        await _emailConfirmation.GenerateAndSendAsync(user, cancellationToken);

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await _loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        var user = await _users.GetByUsernameAsync(request.Username.Trim(), cancellationToken);

        // Sin distinguir si falló el usuario o la contraseña (no se filtra qué cuentas existen).
        if (user is null
            || string.IsNullOrEmpty(user.PasswordHash)
            || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Usuario o contraseña incorrectos.");
        }

        // Guard de email confirmado: solo bloquea si la opción está activa en configuración.
        if (_authOptions.RequireConfirmedEmailToLogin && !user.EmailConfirmed)
        {
            throw new ForbiddenException("Debes confirmar tu correo antes de iniciar sesión.");
        }

        return BuildAuthResponse(user);
    }

    public async Task<UserResponse> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Usuario no encontrado.");

        return UserResponse.FromEntity(user);
    }

    public async Task<UserResponse> UpdateProfileAsync(Guid userId, UpdateMeRequest request, CancellationToken cancellationToken = default)
    {
        await _updateMeValidator.ValidateAndThrowAsync(request, cancellationToken);

        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Usuario no encontrado.");

        if (request.Name is not null)
        {
            user.Name = request.Name.Trim();
        }

        if (request.AvatarUrl is not null)
        {
            var avatar = request.AvatarUrl.Trim();
            user.AvatarUrl = avatar.Length == 0 ? null : avatar;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UserResponse.FromEntity(user);
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        var token = _jwtTokenGenerator.GenerateToken(user);
        return new AuthResponse(token.AccessToken, token.ExpiresAtUtc, UserResponse.FromEntity(user));
    }
}
