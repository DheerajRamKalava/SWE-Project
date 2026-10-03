namespace ScreenShare
{
    public class ScreenCapturer : IScreenCapturer
    {
        public Bitmap Capture()
        {
            Rectangle bounds = Screen.PrimaryScreen!.Bounds;

            Bitmap bitmap = new Bitmap(
                bounds.Width,
                bounds.Height,
                PixelFormat.Format32bppArgb
            );

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(
                    bounds.Location,
                    Point.Empty,
                    bounds.Size
                );
            }

            return bitmap;
        }
    }

}
