using System.Security.Claims;
using Adondeamos.Application.Common.Exceptions;

namespace Adondeamos.Api.Extensions;

/// <summary>Helpers para leer datos del usuario autenticado desde el JWT.</summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Devuelve el id del usuario a partir del claim "sub" del JWT.
    /// Lanza <see cref="UnauthorizedException"/> si el token no trae un id válido.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        // MapInboundClaims está deshabilitado, así que el claim conserva el nombre "sub".
        var subject = principal.FindFirstValue("sub")
                      ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(subject, out var userId))
        {
            return userId;
        }

        throw new UnauthorizedException("El token no contiene un identificador de usuario válido.");
    }
}
