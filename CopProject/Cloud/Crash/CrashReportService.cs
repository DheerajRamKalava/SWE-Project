using System.Diagnostics.CodeAnalysis;
using Cloud.Persistence;

namespace Cloud.Crash;

/// <summary>
/// Reads crash groups and reports from the CrashGroups and CrashReports containers.
/// </summary>
public sealed class CrashReportService : ICrashReportService
{
    private readonly IStorage _groups;
    private readonly IStorage _reports;
    private readonly CrashPendingUploader? _pending;

    /// <summary>
    /// Creates the service.
    /// </summary>
    /// <param name="groups">The storage of the CrashGroups container.</param>
    /// <param name="reports">The storage of the CrashReports container.</param>
    /// <param name="pending">The uploader for reports saved on this device, or null if none.</param>
    public CrashReportService(IStorage groups, IStorage reports, CrashPendingUploader? pending = null)
    {
        ArgumentNullException.ThrowIfNull(groups);
        ArgumentNullException.ThrowIfNull(reports);

        _groups = groups;
        _reports = reports;
        _pending = pending;
    }

    /// <summary>
    /// Creates the service on top of the real Cosmos containers.
    /// </summary>
    /// <param name="pending">The uploader for reports saved on this device, or null if none.</param>
    /// <returns>The service.</returns>
    public static async Task<CrashReportService> CreateAsync(CrashPendingUploader? pending = null)
    {
        IStorage groups = await StorageFactory.CreateAsync(ModuleType.CrashGroups).ConfigureAwait(false);
        IStorage reports = await StorageFactory.CreateAsync(ModuleType.CrashReports).ConfigureAwait(false);

        return new CrashReportService(groups, reports, pending);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CrashGroup>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        (bool success, List<string> keys) = await _groups.GetAllKeys().ConfigureAwait(false);

        if (!success)
        {
            throw new InvalidOperationException("The crash groups could not be listed.");
        }

        List<CrashGroup> groups = [];

        foreach (string key in keys)
        {
            cancellationToken.ThrowIfCancellationRequested();

            (bool found, byte[] data) = await _groups.Read(key).ConfigureAwait(false);

            if (found && TryDeserialize(data, out CrashGroup? group))
            {
                groups.Add(group);
            }
        }

        return groups
            .OrderByDescending(group => group.LastSeenUtc.ToUniversalTime())
            .ThenByDescending(group => group.Count)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CrashReport>> GetReportsAsync(CrashGroup group, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(group);

        List<CrashReport> reports = [];

        foreach (Guid id in group.RecentReportIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            (bool found, byte[] data) = await _reports.Read(id.ToString("D")).ConfigureAwait(false);

            if (found && TryDeserialize(data, out CrashReport? report))
            {
                reports.Add(report);
            }
        }

        return reports
            .OrderByDescending(report => report.TimestampUtc.ToUniversalTime())
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<CrashReport> GetPendingReports()
    {
        return _pending?.GetPendingReports() ?? [];
    }

    /// <inheritdoc />
    public int GetPendingCount()
    {
        return _pending?.PendingCount ?? 0;
    }

    /// <inheritdoc />
    public async Task<int> RetryPendingAsync(CancellationToken cancellationToken = default)
    {
        if (_pending is null)
        {
            return 0;
        }

        return await _pending.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static bool TryDeserialize<T>(byte[] data, [NotNullWhen(true)] out T? value)
        where T : class
    {
        try
        {
            value = CrashXmlSerializer.Deserialize<T>(data);
            return true;
        }
        catch (InvalidDataException)
        {
            value = null;
            return false;
        }
    }
}
