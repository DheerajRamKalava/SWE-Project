namespace Cloud.Crash;

/// <summary>
/// Adds uploaded crash reports to their groups. This is what runs on Azure: the Function passes it each new report.
/// </summary>
public sealed class CrashIngestionService
{
    /// <summary>
    /// How many times a group update is retried when another writer got there first.
    /// </summary>
    public const int MaxAttempts = 5;

    private static readonly TimeSpan s_defaultRetryDelay = TimeSpan.FromMilliseconds(50);

    private readonly Persistence.IStorage _groups;
    private readonly TimeSpan _retryDelay;

    /// <summary>
    /// Creates the service.
    /// </summary>
    /// <param name="groups">The storage of the CrashGroups container.</param>
    /// <param name="retryDelay">The pause after a failed attempt, multiplied by the attempt number.</param>
    public CrashIngestionService(Persistence.IStorage groups, TimeSpan? retryDelay = null)
    {
        ArgumentNullException.ThrowIfNull(groups);

        _groups = groups;
        _retryDelay = retryDelay ?? s_defaultRetryDelay;
    }

    /// <summary>
    /// Fingerprints one report and counts it in its group. Safe to call twice for the same report and
    /// safe with several instances running at once: updates use the ETag of the group document.
    /// </summary>
    /// <param name="reportXml">The serialized report.</param>
    /// <returns>What happened to the report.</returns>
    public async Task<CrashProcessResult> ProcessAsync(byte[] reportXml)
    {
        ArgumentNullException.ThrowIfNull(reportXml);

        CrashReport report;

        try
        {
            report = CrashXmlSerializer.Deserialize<CrashReport>(reportXml);
        }
        catch (InvalidDataException)
        {
            return CrashProcessResult.Invalid;
        }

        CrashSignature signature = CrashFingerprinter.Analyze(report);
        string key = signature.Fingerprint;

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            // Read the ETag before the data: if the group changes in between, the update below fails and we retry.
            (bool exists, string etag) = await _groups.ReadEtag(key).ConfigureAwait(false);

            if (!exists)
            {
                CrashGroup created = CrashGroup.FromReport(signature, report);

                if (await _groups.Create(key, CrashXmlSerializer.Serialize(created)).ConfigureAwait(false))
                {
                    return CrashProcessResult.Recorded;
                }
            }
            else
            {
                (bool found, byte[] data) = await _groups.Read(key).ConfigureAwait(false);

                if (found)
                {
                    CrashGroup group = ReadGroupOrReplace(data, signature, report, out bool replaced);

                    if (!replaced && !group.Record(report))
                    {
                        return CrashProcessResult.Duplicate;
                    }

                    if (await _groups.Update(key, CrashXmlSerializer.Serialize(group), etag).ConfigureAwait(false))
                    {
                        return CrashProcessResult.Recorded;
                    }
                }
            }

            if (attempt < MaxAttempts)
            {
                await Task.Delay(_retryDelay * attempt).ConfigureAwait(false);
            }
        }

        return CrashProcessResult.Conflict;
    }

    private static CrashGroup ReadGroupOrReplace(byte[] data, CrashSignature signature, CrashReport report, out bool replaced)
    {
        try
        {
            replaced = false;
            return CrashXmlSerializer.Deserialize<CrashGroup>(data);
        }
        catch (InvalidDataException)
        {
            // A corrupt group document would block this fingerprint forever, so start it again.
            replaced = true;
            return CrashGroup.FromReport(signature, report);
        }
    }
}
