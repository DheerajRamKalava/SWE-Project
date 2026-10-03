namespace ScreenShare
{
    public class ImageDiffer : IImageDiffer
    {
        private const int TILE_SIZE = 64;
        private readonly Dictionary<(int X, int Y), ulong> previousTileHashes;

        public ImageDiffer()
        {
            previousTileHashes =
                new Dictionary<(int X, int Y), ulong>();
        }

        private ulong CalculateTileHash(Bitmap bitmap, int startX, int startY, int width, int height)
        {
            ulong hash = 14695981039346656037UL;

            for (int y = startY; y < startY + height; y++)
            {
                for (int x = startX; x < startX + width; x++)
                {
                    System.Drawing.Color pixel = bitmap.GetPixel(x, y);

                    hash ^= pixel.R;
                    hash *= 1099511628211UL;

                    hash ^= pixel.G;
                    hash *= 1099511628211UL;

                    hash ^= pixel.B;
                    hash *= 1099511628211UL;
                }
            }

            return hash;
        }

        private List<Tile> FindChangedTiles(Bitmap currentFrame)
        {
            List<Tile> changedTiles = new List<Tile>();

            int screenWidth = currentFrame.Width;
            int screenHeight = currentFrame.Height;

            for (int y = 0; y < screenHeight; y += TILE_SIZE)
            {
                for (int x = 0; x < screenWidth; x += TILE_SIZE)
                {
                    int tileWidth = Math.Min(
                        TILE_SIZE,
                        screenWidth - x
                    );

                    int tileHeight = Math.Min(
                        TILE_SIZE,
                        screenHeight - y
                    );

                    ulong currentHash = CalculateTileHash(
                        currentFrame,
                        x,
                        y,
                        tileWidth,
                        tileHeight
                    );

                    var tilePosition = (x, y);

                    bool changed = true;

                    if (_previousTileHashes.TryGetValue(
                            tilePosition,
                            out ulong previousHash))
                    {
                        if (previousHash != currentHash)
                        {
                            Console.WriteLine(
                                $"Changed tile ({x}, {y}) | " +
                                $"Old: {previousHash:X16} | " +
                                $"New: {currentHash:X16}"
                            );
                        }
                        changed = previousHash != currentHash;
                    }

                    if (changed)
                    {
                        Tile tile = new Tile {
                            X = x,
                            Y = y,
                            Width = tileWidth,
                            Height = tileHeight,
                            Hash = currentHash,
                            Data = ExtractTileData(currentFrame, x, y, tileWidth, tileHeight)
                        };

                        changedTiles.Add(tile);
                    }

                    _previousTileHashes[tilePosition] = currentHash;
                }
            }

            return changedTiles;
        }

        private byte[] ExtractTileData(Bitmap bitmap, int startX, int startY, int width, int height)
        {
            using (Bitmap tileBitmap =
                bitmap.Clone(
                    new Rectangle(
                        startX,
                        startY,
                        width,
                        height
                    ),
                    bitmap.PixelFormat))
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    tileBitmap.Save(
                        stream,
                        ImageFormat.Png
                    );

                    return stream.ToArray();
                }
            }
        }
    }

}
