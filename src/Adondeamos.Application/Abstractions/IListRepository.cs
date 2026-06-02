using Adondeamos.Domain.Entities;

namespace Adondeamos.Application.Abstractions;

public interface IListRepository
{
    void Add(List list);

    void AddItem(ListItem item);

    void RemoveItem(ListItem item);

    Task<List?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Carga la lista con sus elementos (guardado + lugar), ordenados por posición.</summary>
    Task<List?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Listas del usuario: las personales que creó y las de los grupos a los que pertenece.</summary>
    Task<List<List>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ListItem?> GetItemAsync(Guid listId, Guid saveId, CancellationToken cancellationToken = default);

    Task<bool> ItemExistsAsync(Guid listId, Guid saveId, CancellationToken cancellationToken = default);

    /// <summary>Devuelve la siguiente posición disponible al final de la lista.</summary>
    Task<int> GetNextPositionAsync(Guid listId, CancellationToken cancellationToken = default);
}
