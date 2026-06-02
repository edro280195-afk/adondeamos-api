namespace Adondeamos.Application.DTOs.Auth;

/// <summary>Token en claro enviado al hacer clic en el link de confirmación de email.</summary>
public sealed record ConfirmEmailRequest(string Token);
