using WorkLog.Application.Abstractions;

namespace WorkLog.Application.UseCases;

public sealed class DeleteWorkLogEntryUseCase(
    IWorkLogEntryRepository workLogEntryRepository)
{
    public async Task ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entry = await workLogEntryRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (entry is null)
        {
            throw new KeyNotFoundException(
                $"作業記録が見つかりません。ID: {id}");
        }

        await workLogEntryRepository.DeleteAsync(
            entry,
            cancellationToken);
    }
}
