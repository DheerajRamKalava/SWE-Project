namespace ScreenShare
{
    public class Tile
    {
        public int X { get; set; }
        public int Y { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public ulong Hash { get; set; }

        public byte[] Data { get; set; }
    }
}
