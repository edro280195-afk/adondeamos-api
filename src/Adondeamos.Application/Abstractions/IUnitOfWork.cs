namespace Adondeamos.Application.Abstractions;

/// <summary>Confirma los cambios pendientes en una sola transacción lógica.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
