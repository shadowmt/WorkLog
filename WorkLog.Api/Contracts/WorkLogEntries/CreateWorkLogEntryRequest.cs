
using System.ComponentModel.DataAnnotations;

namespace WorkLog.Api.Contracts.WorkLogEntries;

public sealed record CreateWorkLogEntryRequest(
    [Required]
    DateOnly WorkDate,

    [Required]
    Guid ProjectId,

    [Required]
    [StringLength(2000, MinimumLength = 1)]
    string Description,

    [Required]
    [Range(1, 1440)]
    int DurationMinutes);
