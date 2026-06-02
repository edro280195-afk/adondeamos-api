using Adondeamos.Application.DTOs.Saves;
using Adondeamos.Domain.Entities;

namespace Adondeamos.Application.DTOs.Lists;

/// <summary>Elemento de una lista: el guardado, quién lo agregó y su orden.</summary>
public sealed record ListItemResponse(SaveResponse Save, Guid? AddedBy, int Position, DateTime AddedAt)
{
    public static ListItemResponse FromEntity(ListItem item) =>
        new(SaveResponse.FromEntity(item.Save), item.AddedBy, item.Position, item.AddedAt);
}
