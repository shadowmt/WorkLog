using WorkLog.Api.Contracts.Projects;
using WorkLog.Application.Exceptions;
using WorkLog.Application.UseCases;

namespace WorkLog.Api.Endpoints;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects", async (
            GetProjectsUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var projects = await useCase.ExecuteAsync(cancellationToken);

            var response = projects
                .Select(project => new ProjectResponse(
                    project.Id,
                    project.Name,
                    project.Description,
                    project.IsActive))
                .ToList();

            return Results.Ok(response);
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
