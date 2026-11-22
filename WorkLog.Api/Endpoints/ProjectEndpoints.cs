using Microsoft.EntityFrameworkCore;
using WorkLog.Api.Contracts.Projects;
using WorkLog.Application.Exceptions;
using WorkLog.Application.UseCases;
using WorkLog.Infrastructure.Persistence;

namespace WorkLog.Api.Endpoints;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects", async (
            WorkLogDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var projects = await dbContext.Projects
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new ProjectResponse(
                    x.Id,
                    x.Name,
                    x.Description,
                    x.IsActive))
                .ToListAsync(cancellationToken);

            return Results.Ok(projects);
        });

        app.MapPost("/api/projects", async (
            CreateProjectRequest request,
            CreateProjectUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var project = await useCase.ExecuteAsync(
                    request.Name,
                    request.Description,
                    cancellationToken);

                var response = new ProjectResponse(
                    project.Id,
                    project.Name,
                    project.Description,
                    project.IsActive);

                return Results.Created(
                    $"/api/projects/{project.Id}",
                    response);
            }
            catch (DuplicateProjectNameException ex)
            {
                return Results.Conflict(new
                {
                    message = ex.Message
                });
            }
        });

        return app;
    }
}
