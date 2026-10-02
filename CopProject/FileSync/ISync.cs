namespace FileSync;

public interface ISync
{
    /// <summary> Pulls newer/missing files from the peer into the local sync folder. </summary>
    /// <returns> Number of files updated. </returns>
    Task<int> SyncAsync(string peerHost, int peerPort);
}
