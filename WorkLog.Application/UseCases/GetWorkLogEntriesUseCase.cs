using WorkLog.Application.Abstractions;
using WorkLog.Application.Models;

namespace WorkLog.Application.UseCases;

public sealed class GetWorkLogEntriesUseCase(
    IProjectRepository projectRepository,
    IWorkLogEntryRepository workLogEntryRepository)
{
    public async Task<PagedResult<WorkLogEntryListItem>> ExecuteAsync(
        WorkLogEntrySearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        // 日付範囲の検証
        if (criteria.From.HasValue &&
            criteria.To.HasValue &&
            criteria.From.Value > criteria.To.Value)
        {
            throw new ArgumentException(
                "開始日は終了日以前の日付を指定してください。",
                nameof(criteria));
        }

        // ページ番号の検証
        if (criteria.Page < 1)
        {
            throw new ArgumentException(
                "ページ番号は1以上を指定してください。",
                nameof(criteria));
        }

        // ページサイズの検証
        if (criteria.PageSize < 1 || criteria.PageSize > 100)
        {
            throw new ArgumentException(
                "ページサイズは1～100の範囲で指定してください。",
                nameof(criteria));
        }

        // ProjectIdが指定されている場合はProjectの存在を確認
        if (criteria.ProjectId.HasValue)
        {
            var project = await projectRepository.GetByIdAsync(
                criteria.ProjectId.Value,
                cancellationToken);

            if (project is null)
            {
                throw new KeyNotFoundException(
                    $"Projectが見つかりません。ProjectId: {criteria.ProjectId.Value}");
            }
        }

        // 検索条件に従ってRepositoryから取得
        return await workLogEntryRepository.GetPagedAsync(
            criteria,
            cancellationToken);
    }
}
