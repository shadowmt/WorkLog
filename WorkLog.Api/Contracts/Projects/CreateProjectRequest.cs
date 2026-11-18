using System.ComponentModel.DataAnnotations;

namespace WorkLog.Api.Contracts.Projects;

public sealed record CreateProjectRequest(
    [Required]
    [StringLength(100, MinimumLength = 1)]
    string Name,

    [StringLength(1000)]
    string? Description);
