using WorkLog.Api.Contracts.WorkLogEntries;
using WorkLog.Application.Models;
using WorkLog.Application.UseCases;

namespace WorkLog.Api.Endpoints;

public static class WorkLogEntryEndpoints
{
    public static IEndpointRouteBuilder MapWorkLogEntryEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/work-log-entries", async (
            CreateWorkLogEntryRequest request,
            CreateWorkLogEntryUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var entry = await useCase.ExecuteAsync(
                    request.WorkDate,
                    request.ProjectId,
                    request.Description,
                    request.DurationMinutes,
                    cancellationToken);

                return Results.Created(
                    "/api/work-log-entries",
                    new WorkLogEntryResponse(
                        entry.Id,
                        entry.WorkDate,
                        entry.ProjectId,
                        entry.Description,
                        entry.DurationMinutes));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.UnprocessableEntity(new { message = ex.Message });
            }
        });

        app.MapGet("/api/work-log-entries", async (
            DateOnly? from,
            DateOnly? to,
            Guid? projectId,
            int? page,
            int? pageSize,
            GetWorkLogEntriesUseCase useCase,
            TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            // 日付が両方省略された場合は、JSTの当月を対象にする
            if (!from.HasValue && !to.HasValue)
            {
                var jstNow = timeProvider.GetUtcNow().ToOffset(
                    TimeSpan.FromHours(9));

                var today = DateOnly.FromDateTime(jstNow.DateTime);

                from = new DateOnly(today.Year, today.Month, 1);
                to = from.Value.AddMonths(1).AddDays(-1);
            }

            var criteria = new WorkLogEntrySearchCriteria(
                from,
                to,
                projectId,
                page ?? 1,
                pageSize ?? 50);

            try
            {
                var result = await useCase.ExecuteAsync(
                    criteria,
                    cancellationToken);

                var response = new PagedWorkLogEntryResponse(
                    result.Items.Select(x => new WorkLogEntryListItemResponse(
                        x.Id,
                        x.WorkDate,
                        x.ProjectId,
                        x.ProjectName,
                        x.ProjectIsActive,
                        x.Description,
                        x.DurationMinutes))
                    .ToList(),
                    result.Page,
                    result.PageSize,
                    result.TotalCount,
                    result.TotalPages);

                return Results.Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        return app;
    }
}
