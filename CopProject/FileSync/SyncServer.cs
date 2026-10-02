using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace FileSync;

/// <summary>
/// Listens on a port and answers two commands:
///   LIST        -> JSON manifest of every file in the sync folder
///   GET <path>  -> int64 length, then the file bytes (-1 if invalid)
/// </summary>
public sealed class SyncServer : IDisposable
{
    private readonly string _root;
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();

    public SyncServer(string root, int port)
    {
        _root = Path.GetFullPath(root);
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public void Start()
    {
        Directory.CreateDirectory(_root);
        _listener.Start();
        _ = Task.Run(AcceptLoopAsync);
    }

    private async Task AcceptLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(_cts.Token);
                _ = Task.Run(() => Handle(client));
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (SocketException) { break; }
        }
    }

    private void Handle(TcpClient client)
    {
        using (client)
        using (NetworkStream stream = client.GetStream())
        using (var reader = new BinaryReader(stream))
        using (var writer = new BinaryWriter(stream))
        {
            try
            {
                while (true)
                {
                    string cmd = reader.ReadString();

                    if (cmd == "LIST")
                    {
                        var list = Directory
                            .EnumerateFiles(_root, "*", SearchOption.AllDirectories)
                            .Select(f => new FileInfo(f))
                            .Select(fi => new FileEntry(
                                Path.GetRelativePath(_root, fi.FullName).Replace('\\', '/'),
                                fi.Length,
                                fi.LastWriteTimeUtc))
                            .ToList();
                        writer.Write(JsonSerializer.Serialize(list));
                    }
                    else if (cmd == "GET")
                    {
                        string rel = reader.ReadString();
                        string full = Path.GetFullPath(Path.Combine(_root, rel));

                        // block "../" path traversal
                        if (!full.StartsWith(_root + Path.DirectorySeparatorChar) || !File.Exists(full))
                        {
                            writer.Write(-1L);
                        }
                        else
                        {
                            byte[] data = File.ReadAllBytes(full);
                            writer.Write((long)data.Length);
                            writer.Write(data);
                        }
                    }
                    else
                    {
                        break; // BYE or unknown
                    }

                    writer.Flush();
                }
            }
            catch (EndOfStreamException) { }
            catch (IOException) { }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
    }
}
