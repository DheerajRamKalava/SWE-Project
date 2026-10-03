namespace ScreenShare
{
    public interface IImageDiffer
    {
        List<Tile> FindChangedTiles(Bitmap currentFrame);
    }

}
