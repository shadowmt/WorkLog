using WorkLog.Api.Contracts.WorkLogEntries;
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

        return app;
    }
}
