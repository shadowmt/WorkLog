using WorkLog.Application.Abstractions;
using WorkLog.Application.Models;
using WorkLog.Domain.Entities;

namespace WorkLog.Application.UseCases;

public sealed class UpdateWorkLogEntryUseCase(
    IWorkLogEntryRepository workLogEntryRepository,
    IProjectRepository projectRepository,
    TimeProvider timeProvider)
{
    public async Task<WorkLogEntry> ExecuteAsync(
        Guid id,
        Optional<DateOnly> workDate,
        Optional<Guid> projectId,
        Optional<string> description,
        Optional<int> durationMinutes,
        CancellationToken cancellationToken = default)
    {
        if (!workDate.IsSpecified &&
            !projectId.IsSpecified &&
            !description.IsSpecified &&
            !durationMinutes.IsSpecified)
        {
            throw new ArgumentException(
                "更新する項目が指定されていません。");
        }

        // 1. 更新対象のWorkLogEntryを取得
        var entry = await workLogEntryRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (entry is null)
        {
            throw new KeyNotFoundException(
                $"WorkLogEntryが見つかりません。Id: {id}");
        }

        // 2. ProjectIdが指定されている場合だけ存在確認
        var targetProjectId = projectId.IsSpecified
            ? projectId.Value
            : entry.ProjectId;

        if (targetProjectId == Guid.Empty)
        {
            throw new ArgumentException(
                "ProjectIdに空のGUIDは指定できません。",
                nameof(projectId));
        }

        // 既存のWorkLogEntryの更新では、無効化済みProjectへの変更も許可する。
        // 新規登録時とは異なり、既存の作業実績の修正を妨げないため、ここでは存在確認のみ行う。
        if (projectId.IsSpecified)
        {
            var project = await projectRepository.GetByIdAsync(
                targetProjectId,
                cancellationToken);

            if (project is null)
            {
                throw new KeyNotFoundException(
                    $"Projectが見つかりません。Id: {targetProjectId}");
            }
        }

        // 3. 指定された値と既存値を組み合わせて更新
        entry.Update(
            workDate.IsSpecified ? workDate.Value : entry.WorkDate,
            targetProjectId,
            description.IsSpecified ? description.Value! : entry.Description,
            durationMinutes.IsSpecified
                ? durationMinutes.Value
                : entry.DurationMinutes,
            timeProvider.GetUtcNow());

        // 4. 保存
        await workLogEntryRepository.UpdateAsync(
            entry,
            cancellationToken);

        return entry;
    }
}
