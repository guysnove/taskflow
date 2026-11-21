using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace TaskFlow.Api;

// Course 2, Chapter 15: the query string of GET /api/tasks. Every part is optional.
// [ApiController] checks the ranges and answers 400 with a problem document if one is wrong.
public record TaskListRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    [FromQuery(Name = "done")]
    public bool? Done { get; init; }
    [FromQuery(Name = "projectId")]
    public int? ProjectId { get; init; }

    [FromQuery(Name = "page")]
    [Range(1, int.MaxValue, ErrorMessage = "Pages are numbered from 1.")]
    public int? Page { get; init; }

    [FromQuery(Name = "pageSize")]
    [Range(1, MaxPageSize, ErrorMessage = "A page holds 1 to 100 tasks.")]
    public int? PageSize { get; init; }
}
