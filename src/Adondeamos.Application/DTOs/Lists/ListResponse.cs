using Adondeamos.Domain.Entities;
using Adondeamos.Domain.Enums;

namespace Adondeamos.Application.DTOs.Lists;

/// <summary>Lista en el listado del usuario. <c>GroupId</c> nulo = personal; con valor = de grupo.</summary>
public sealed record ListResponse(
    Guid Id,
    string Name,
    Guid? GroupId,
    ContentVisibility Visibility,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static ListResponse FromEntity(List list) =>
        new(list.Id, list.Name, list.GroupId, list.Visibility, list.CreatedAt, list.UpdatedAt);
}
