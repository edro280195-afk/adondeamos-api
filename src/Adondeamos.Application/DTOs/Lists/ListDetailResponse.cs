using Adondeamos.Domain.Entities;
using Adondeamos.Domain.Enums;

namespace Adondeamos.Application.DTOs.Lists;

/// <summary>Detalle de una lista con sus elementos (ordenados por posición).</summary>
public sealed record ListDetailResponse(
    Guid Id,
    string Name,
    Guid? GroupId,
    ContentVisibility Visibility,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<ListItemResponse> Items)
{
    public static ListDetailResponse FromEntity(List list) =>
        new(
            list.Id,
            list.Name,
            list.GroupId,
            list.Visibility,
            list.CreatedAt,
            list.UpdatedAt,
            list.Items.Select(ListItemResponse.FromEntity).ToList());
}
