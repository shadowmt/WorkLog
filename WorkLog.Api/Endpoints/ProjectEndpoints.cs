using Microsoft.EntityFrameworkCore;
using WorkLog.Api.Contracts.Projects;
using WorkLog.Domain.Entities;
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
            WorkLogDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var normalizedName = request.Name.Trim().ToUpperInvariant();

            var existingProject = await dbContext.Projects
                .AnyAsync(x => x.NormalizedName == normalizedName, cancellationToken);

            if (existingProject)
            {
                return Results.Conflict(new
                {
                    message = "同じ名前のプロジェクトが既に存在します。"
                });
            }

            var project = new Project(
                request.Name,
                request.Description);

            dbContext.Projects.Add(project);

            await dbContext.SaveChangesAsync(cancellationToken);

            var response = new ProjectResponse(
                project.Id,
                project.Name,
                project.Description,
                project.IsActive);

            return Results.Created(
                $"/api/projects/{project.Id}",
                response);
        });

        return app;
    }
}
