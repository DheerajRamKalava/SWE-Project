using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ScreenShare;

public class ScreenCapturer : IImageSource
{
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    public Bitmap Capture()
    {
        var bounds = new Rectangle(0,0,GetSystemMetrics(SM_CXSCREEN),GetSystemMetrics(SM_CYSCREEN));

        var screenshot = new Bitmap(bounds.Width,bounds.Height,PixelFormat.Format32bppArgb);

        using (Graphics graphics = Graphics.FromImage(screenshot))
        {
            graphics.CopyFromScreen(bounds.Location,Point.Empty,bounds.Size);
        }

        return screenshot;
    }
}
