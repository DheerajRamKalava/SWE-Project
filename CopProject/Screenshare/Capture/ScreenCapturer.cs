using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
namespace ScreenShare
{
    public class ScreenCapturer : IImageSource
    {
        public Bitmap Capture()
        {
            Rectangle bounds = Screen.PrimaryScreen!.Bounds;

            Bitmap screenshot = new Bitmap(
                bounds.Width,
                bounds.Height,
                PixelFormat.Format32bppArgb
            );

            using (Graphics graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(
                    bounds.Location,
                    Point.Empty,
                    bounds.Size
                );
            }

            return screenshot;
        }
    }

}
