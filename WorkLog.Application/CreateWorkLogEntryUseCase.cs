using WorkLog.Application.Abstractions;
using WorkLog.Domain.Entities;

namespace WorkLog.Application.UseCases;

public sealed class CreateWorkLogEntryUseCase(
    IProjectRepository projectRepository,
    IWorkLogEntryRepository workLogEntryRepository)
{
    public async Task<WorkLogEntry> ExecuteAsync(
        DateOnly workDate,
        Guid projectId,
        string description,
        int durationMinutes,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(
            projectId,
            cancellationToken);

        if (project is null)
        {
            throw new KeyNotFoundException(
                $"Projectが見つかりません。ProjectId: {projectId}");
        }

        if (!project.IsActive)
        {
            throw new InvalidOperationException(
                $"無効なProjectには作業実績を登録できません。ProjectId: {projectId}");
        }

        var entry = new WorkLogEntry(
            workDate,
            projectId,
            description,
            durationMinutes);

        await workLogEntryRepository.AddAsync(
            entry,
            cancellationToken);

        return entry;
    }
}
