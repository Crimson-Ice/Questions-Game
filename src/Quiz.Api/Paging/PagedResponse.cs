namespace Quiz.Api.Paging;

/// <summary>Une page d'une liste, avec le nombre total d'éléments pour construire la pagination.</summary>
public sealed record PagedResponse<T>(List<T> Items, int Page, int PageSize, int TotalCount);
