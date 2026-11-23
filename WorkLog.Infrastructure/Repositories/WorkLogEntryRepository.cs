using Microsoft.EntityFrameworkCore;
using WorkLog.Application.Abstractions;
using WorkLog.Application.Models;
using WorkLog.Domain.Entities;
using WorkLog.Infrastructure.Persistence;

namespace WorkLog.Infrastructure.Repositories;

public sealed class WorkLogEntryRepository(
    WorkLogDbContext dbContext) : IWorkLogEntryRepository
{
    public async Task AddAsync(
        WorkLogEntry workLogEntry,
        CancellationToken cancellationToken = default)
    {
        await dbContext.WorkLogEntries.AddAsync(
            workLogEntry,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<WorkLogEntryListItem>> GetPagedAsync(
        WorkLogEntrySearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.WorkLogEntries
            .AsNoTracking()
            .AsQueryable();

        // 作業日の開始日
        if (criteria.From.HasValue)
        {
            query = query.Where(
                x => x.WorkDate >= criteria.From.Value);
        }

        // 作業日の終了日
        if (criteria.To.HasValue)
        {
            query = query.Where(
                x => x.WorkDate <= criteria.To.Value);
        }

        // Project ID
        if (criteria.ProjectId.HasValue)
        {
            query = query.Where(
                x => x.ProjectId == criteria.ProjectId.Value);
        }

        // ページング前の総件数
        var totalCount = await query.CountAsync(cancellationToken);

        // 総ページ数
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(
                (double)totalCount / criteria.PageSize);

        // 並び替え・ページング・取得項目の射影
        var items = await query
            .OrderByDescending(x => x.WorkDate)
            .ThenByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(x => new WorkLogEntryListItem(
                x.Id,
                x.WorkDate,
                x.ProjectId,
                x.Project.Name,
                x.Project.IsActive,
                x.Description,
                x.DurationMinutes))
            .ToListAsync(cancellationToken);

        return new PagedResult<WorkLogEntryListItem>(
            items,
            criteria.Page,
            criteria.PageSize,
            totalCount,
            totalPages);
    }

    public async Task<WorkLogEntry?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.WorkLogEntries
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task UpdateAsync(
        WorkLogEntry workLogEntry,
        CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
