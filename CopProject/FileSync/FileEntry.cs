namespace FileSync;

/// <summary>One line of a folder "manifest" exchanged between peers.</summary>
public record FileEntry(string Path, long Size, DateTime LastWriteUtc);
