using System.Drawing;

namespace ScreenShare;

public class ScreenSharingService
{
    private readonly IImageSource imageSource;

    private readonly IImageDiffer imageDiffer;

    private Bitmap? previousFrame;

    public ScreenSharingService(IImageSource imageSource,IImageDiffer imageDiffer)
    {
        this.imageSource = imageSource;
        this.imageDiffer = imageDiffer;
    }

    public ImageDiffResult ProcessFrame()
    {
        Bitmap currentFrame =
            imageSource.Capture();

        
         // first frame has nothing to compare against. therefore the first frame must be treated as a full frame.
         
        if (previousFrame == null)
        {
            previousFrame =new Bitmap(currentFrame);

            currentFrame.Dispose();

            return ImageDiffResult.FullFrame(new Bitmap(previousFrame));
        }

        ImageDiffResult result =imageDiffer.Compare(previousFrame,currentFrame);

        
        //current frame becomes the base frame for the next comparison.
         
        Bitmap newPreviousFrame = new Bitmap(currentFrame);

        previousFrame.Dispose();

        previousFrame =newPreviousFrame;

        currentFrame.Dispose();

        return result;
    }

    public void Reset()
    {
        previousFrame?.Dispose();

        previousFrame = null;
    }
}
