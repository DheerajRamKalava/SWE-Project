using System.Drawing;

namespace ScreenShare;

public class ScreenSharingService
{
    private readonly IImageSource _imageSource;

    private readonly IImageDiffer _imageDiffer;

    private Bitmap? _previousFrame;

    public ScreenSharingService(IImageSource imageSource, IImageDiffer imageDiffer)
    {
        _imageSource = imageSource;
        _imageDiffer = imageDiffer;
    }

    public ImageDiffResult ProcessFrame()
    {
        Bitmap currentFrame = _imageSource.Capture();

        // First frame has nothing to compare against.
        // Therefore, the first frame must be treated as a full frame.
        if (_previousFrame == null)
        {
            _previousFrame = new Bitmap(currentFrame);

            // currentFrame.Dispose();

            return ImageDiffResult.CreateFullFrame(new Bitmap(_previousFrame));
        }

        ImageDiffResult result = _imageDiffer.Compare(_previousFrame, currentFrame);

        // Current frame becomes the base frame for the next comparison.
        Bitmap newPreviousFrame = new Bitmap(currentFrame);

        _previousFrame.Dispose();

        _previousFrame = newPreviousFrame;

        currentFrame.Dispose();

        return result;
    }

    public void Reset()
    {
        _previousFrame?.Dispose();

        _previousFrame = null;
    }
}
