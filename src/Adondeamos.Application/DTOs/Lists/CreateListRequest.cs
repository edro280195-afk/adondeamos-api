using Adondeamos.Domain.Enums;

namespace Adondeamos.Application.DTOs.Lists;

/// <summary>Crea una lista: personal si <c>GroupId</c> es nulo; de grupo si trae valor.</summary>
public sealed record CreateListRequest(string Name, Guid? GroupId, ContentVisibility? Visibility);
