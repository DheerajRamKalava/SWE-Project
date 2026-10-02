namespace FileSync;

internal static class Program
{
    /// Usage: FileSync.exe [listenPort] [peerPort] [folder]
    [STAThread]
    private static void Main(string[] args)
    {
        int listenPort = args.Length > 0 ? int.Parse(args[0]) : 5001;
        int peerPort = args.Length > 1 ? int.Parse(args[1]) : 5002;
        string folder = args.Length > 2 ? args[2] : $"SyncFolder_{listenPort}";

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(folder, listenPort, peerPort));
    }
}
