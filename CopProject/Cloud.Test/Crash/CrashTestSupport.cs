using Cloud.Crash;
using Cloud.Persistence;

namespace Cloud.Test.Crash;

internal sealed class TempDir : IDisposable
{
    public TempDir()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "crash-test-" + Guid.NewGuid().ToString("N"));
    }

    public string Path { get; }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, true);
        }
    }
}

internal sealed class FakeUploader : ICrashUploader
{
    public Func<Task<bool>> Behavior { get; set; } = () => Task.FromResult(true);

    public List<byte[]> Uploaded { get; } = [];

    public Task<bool> UploadAsync(Guid reportId, byte[] reportXml, CancellationToken cancellationToken)
    {
        lock (Uploaded)
        {
            Uploaded.Add(reportXml);
        }

        return Behavior();
    }
}

internal sealed class InMemoryStorage : IStorage
{
    private readonly Dictionary<string, (byte[] Data, int Version)> _items = [];

    public int FailNextUpdates { get; set; }

    public bool FailGetAllKeys { get; set; }

    public Action<string>? BeforeCreate { get; set; }

    public Task<bool> Create(string key, byte[] data)
    {
        BeforeCreate?.Invoke(key);

        if (_items.ContainsKey(key))
        {
            return Task.FromResult(false);
        }

        _items[key] = (data, 1);
        return Task.FromResult(true);
    }

    public Task<(bool Success, byte[] Data)> Read(string key)
    {
        return Task.FromResult(_items.TryGetValue(key, out (byte[] Data, int Version) item) ? (true, item.Data) : (false, []));
    }

    public Task<(bool Success, Dictionary<string, byte[]> Data)> ReadAll(string[] keys)
    {
        return Task.FromResult((true, _items.Where(i => keys.Contains(i.Key)).ToDictionary(i => i.Key, i => i.Value.Data)));
    }

    public Task<bool> Update(string key, byte[] data, string etag)
    {
        if (FailNextUpdates > 0)
        {
            FailNextUpdates--;
            return Task.FromResult(false);
        }

        if (!_items.TryGetValue(key, out (byte[] Data, int Version) item) || item.Version.ToString() != etag)
        {
            return Task.FromResult(false);
        }

        _items[key] = (data, item.Version + 1);
        return Task.FromResult(true);
    }

    public Task<bool> Delete(string key)
    {
        return Task.FromResult(_items.Remove(key));
    }

    public Task<(bool Success, List<string> Keys)> GetAllKeys()
    {
        return Task.FromResult((!FailGetAllKeys, _items.Keys.ToList()));
    }

    public Task<(bool Success, string Etag)> ReadEtag(string key)
    {
        return Task.FromResult(_items.TryGetValue(key, out (byte[] Data, int Version) item) ? (true, item.Version.ToString()) : (false, string.Empty));
    }

    public Task Put(string key, byte[] data)
    {
        _items[key] = (data, 1);
        return Task.CompletedTask;
    }
}

internal sealed class FakeCrashReportService : ICrashReportService
{
    public List<CrashGroup> Groups { get; set; } = [];

    public List<CrashReport> Reports { get; set; } = [];

    public bool Fail { get; set; }

    public int Pending { get; set; }

    public int RetryResult { get; set; }

    public Task<IReadOnlyList<CrashGroup>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        return Fail ? throw new InvalidOperationException("down") : Task.FromResult<IReadOnlyList<CrashGroup>>(Groups);
    }

    public Task<IReadOnlyList<CrashReport>> GetReportsAsync(CrashGroup group, CancellationToken cancellationToken = default)
    {
        return Fail ? throw new InvalidOperationException("down") : Task.FromResult<IReadOnlyList<CrashReport>>(Reports);
    }

    public int GetPendingCount()
    {
        return Pending;
    }

    public Task<int> RetryPendingAsync(CancellationToken cancellationToken = default)
    {
        return Fail ? throw new InvalidOperationException("down") : Task.FromResult(RetryResult);
    }
}

internal static class Samples
{
    public static CrashReport Report(string type = "System.InvalidOperationException", string method = "Run", string assembly = "Chat", DateTime? when = null)
    {
        return new CrashReport {
            TimestampUtc = when ?? DateTime.UtcNow,
            Exceptions = [
                new CrashExceptionInfo {
                    Type = type,
                    Message = "boom",
                    Frames = [new CrashFrame { DeclaringType = assembly + ".A", Method = method, Assembly = assembly }]
                }
            ]
        };
    }

    public static CrashGroup Group(CrashReport report)
    {
        return CrashGroup.FromReport(CrashFingerprinter.Analyze(report), report);
    }
}
