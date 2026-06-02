namespace Adondeamos.Application.DTOs.Auth;

/// <summary>Solicita reenviar el correo de confirmación, identificando al usuario por su email.</summary>
public sealed record ResendConfirmationRequest(string Email);
