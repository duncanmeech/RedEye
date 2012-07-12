using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.IO;

namespace RedEye
{
    /// <summary>
    /// Various utility functions for manipulating images
    /// </summary>
    public class ImageUtility
    {
        /// <summary>
        /// All members are static
        /// </summary>
        private ImageUtility()
        {
        }

        /// <summary>
        /// A generally available identity matrix
        /// </summary>
        public static Matrix IdentityMatrix = new Matrix();


        /// <summary>
        /// max dimensions of images we can handle. We might do more but certain algorithms ( seed file )
        /// use hard wired stacks with limits determined by these numbers.
        /// </summary>
        public static int kMAX_IMAGE_WIDTH = 2048;

        public static int kMAX_IMAGE_HEIGHT = 2048;

        /// <summary>
        /// acceptable ranges for gamma correction values
        /// </summary>
        public static float kMIN_GAMMA = 0.01f;

        public static float kMAX_GAMMA = 5.0f;

        public static float kDEFAULT_GAMMA = 1.0f;

        /// <summary>
        /// Create a new empty image of the given size and format
        /// </summary>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="pf"></param>
        /// <returns></returns>
        public static Bitmap CreateImage(int w, int h, PixelFormat pf)
        {
            try
            {

                return new Bitmap(w, h, pf);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return null;
        }
        /// <summary>
        /// Create a new empty image of the given size and format and initialize with the given color
        /// </summary>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="pf"></param>
        /// <returns></returns>
        public static Bitmap CreateImage(int w, int h, PixelFormat pf, Color fillColor)
        {
            Bitmap b = null;

            try
            {

                // create new image

                b = new Bitmap(w, h, pf);

                // create a compatible Graphics

                using (Graphics g = Graphics.FromImage(b))
                {

                    // fill with given color

                    using (SolidBrush br = new SolidBrush(fillColor))
                    {
                        g.FillRectangle(br, 0, 0, w, h);
                    }

                }

                // return bitmap

                return b;


            }
            catch (Exception)
            {

            }

            // if here bitmap creation failed

            return null;
        }

        /// <summary>
        /// Create an image containing the given source rectangle of the original image
        /// </summary>
        /// <param name="sourceImage"></param>
        /// <param name="sourceRectangle"></param>
        /// <returns></returns>
        public static Image CreateImage(Image sourceImage, Rectangle sourceRectangle)
        {
            try
            {

                // create new bitmap

                Bitmap newImage = new Bitmap(sourceRectangle.Width, sourceRectangle.Height, PixelFormat.Format32bppPArgb);

                // get graphics surface

                using (Graphics g = Graphics.FromImage(newImage))
                {

                    // copy over since we want the new image data and nothing of the newly created image to be visible.

                    g.CompositingMode = CompositingMode.SourceCopy;

                    // render given section into new image

                    g.DrawImage(sourceImage, new Rectangle(0, 0, newImage.Width, newImage.Height), sourceRectangle, GraphicsUnit.Pixel);

                }
                // return new image

                return newImage;

            }
            catch (Exception)
            {
                return null;
            }

        }



        /// <summary>
        /// Generate a new image based on the given image. Size is specified by caller.
        /// InterpolationMode,SmoothingMode and PixelOffsetMode used to render the new image
        /// are specified by the caller.		/// </summary>
        /// <param name="sourceImage"></param>
        /// <param name="newWidth"></param>
        /// <param name="newHeight"></param>
        /// <param name="imode"></param>
        /// <param name="smode"></param>
        /// <param name="pomode"></param>
        /// <returns></returns>
        public static Image CreateImage(Image sourceImage, int newWidth, int newHeight, InterpolationMode imode, SmoothingMode smode, PixelOffsetMode pomode)
        {
            try
            {

                // create the new image

                Bitmap newImage = new Bitmap(newWidth, newHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

                // get graphics object containing the new image

                using (Graphics g = Graphics.FromImage(newImage))
                {

                    // set the interpolation mode of the graphics

                    g.InterpolationMode = imode;

                    // set smoothing mode 

                    g.SmoothingMode = smode;

                    // set pixel offset mode

                    g.PixelOffsetMode = pomode;

                    // draw into the image

                    g.DrawImage(sourceImage, new Rectangle(0, 0, newWidth, newHeight));

                }

                // return the new image

                return newImage;

            }
            catch (Exception)
            {

                return null;
            }

        }

        /// <summary>
        /// Create a new ( empty ) image of the given size and draw the given image centered in it
        /// </summary>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="source"></param>
        /// <param name="pf"></param>
        /// <returns></returns>
        public static Image CreateImage(int w, int h, Image source, PixelFormat pf)
        {
            // create blank image

            Image i = ImageUtility.CreateImage(w, h, pf, Color.Transparent);

            if (i != null)
            {
                // draw image centered

                using (Graphics g = Graphics.FromImage(i))
                {

                    g.DrawImageUnscaled(source, (i.Width / 2) - (source.Width / 2), (i.Height / 2) - (source.Height / 2));

                }
            }

            // return image to caller

            return i;
        }

        /// <summary>
        /// Create an image with the given pixel format and rendered the supplied image into it.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="pf"></param>
        public static Image CreateImage(Image source, PixelFormat pf)
        {

            try
            {

                // create new image

                Bitmap newImage = new Bitmap(source.Width, source.Height, pf);

                // get graphics surface

                using (Graphics g = Graphics.FromImage(newImage))
                {

                    // draw source image into new one ( using pixel units )

                    Rectangle r = Rectangle.FromLTRB(0, 0, source.Width, source.Height);

                    g.DrawImage(source, r, r, GraphicsUnit.Pixel);

                }

                // return new image

                return newImage;

            }
            catch (Exception)
            {
                return null;
            }

        }

         /// <summary>
        /// Return a point that represents an aspect ratio correct thumbnail size. bx/by are the source width/height. mw/mh
        /// are the maximum dimensions of the thumbnail
        /// </summary>
        /// <param name="sw"></param>
        /// <param name="sh"></param>
        /// <returns></returns>
        public static Point GetThumbSizeAR(int sx, int sy, int mw, int mh, bool upscale)
        {
            // will hold thumb size on exit from this section

            int tx, ty;

            // use floats for calculations to avoid rounding errors

            float bx = (float)sx;

            float by = (float)sy;

            float wx = (float)mw;

            float wy = (float)mh;


            // if image fits entirely in window then just go with image size unless upscaling required

            if ((bx <= wx) && (by <= wy) && (upscale == false))
            {

                tx = sx;
                ty = sy;

            }
            else
            {

                // 1. Figure out which dimension is the worst fit, this is the axis/side
                //    that we have to accomodate.

                if ((wx / bx) < (wy / by))
                {
                    // width is the worst fit 

                    // make width == window width

                    tx = mw;

                    // make height in correct ratio to original

                    ty = (int)(by * (wx / bx));

                }
                else
                {
                    // height is the worst fit 

                    // make height == window height

                    ty = mh;

                    // make height in correct ratio to original

                    tx = (int)(bx * (wy / by));
                }


            }

            // return as point

            return new Point(tx, ty);
        }

        /// <summary>
        /// Given a source size ( sx,sy ) and a destination size ( dx,dy ) return an
        /// aspect ratio correct size of the largest scaled version of sx,sy that will fit
        /// in the area.
        /// </summary>
        /// <param name="sw"></param>
        /// <param name="sh"></param>
        /// <returns></returns>
        public static PointF GetThumbSizeAR(float sx, float sy, float dx, float dy, bool upscale)
        {
            // will hold thumb size on exit from this section

            float tx, ty;


            // if source fits in destination then just return that

            if ((sx <= dx) && (sy <= dy) && (upscale == false))
            {
                tx = sx;
                ty = sy;
            }
            else
            {

                // 1. Figure out which dimension is the worst fit, this is the axis/side
                //    that we have to accomodate.

                if ((dx / sx) < (dy / sy))
                {
                    // width is the worst fit 

                    // make width == window width

                    tx = dx;

                    // make height in correct ratio to original

                    ty = sy * (dx / sx);

                }
                else
                {
                    // height is the worst fit 

                    // make height == window height

                    ty = dy;

                    // make height in correct ratio to original

                    tx = sx * (dy / sy);
                }


            }

            // return as point

            return new PointF(tx, ty);
        }

        /// <summary>
        /// Create a high quality thumbnail that preserves the aspect ratio of the original source image
        /// but is just large enough to fit within width/height
        /// </summary>
        /// <param name="source"></param>
        /// <param name="wx"></param>
        /// <param name="wy"></param>
        /// <returns></returns>
        public static Image CreateThumbnail(Image source, int width, int height)
        {
            // get aspect ratio correct thumbnail

            Point p = ImageUtility.GetThumbSizeAR(source.Width, source.Height, width, height, false);

            // ok, tx/ty are the required size

            // create the new image ( must be a non-palette image of the system won't be able
            // to create a graphics object from it ).

            Bitmap newImage = new Bitmap(p.X, p.Y, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            // get graphics object associated with new bitmap

            Graphics g = Graphics.FromImage(newImage);

            // set the interpolation mode of the graphics

            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            // draw into the image

            g.DrawImage(source, new Rectangle(0, 0, p.X, p.Y));

            // release the graphics object

            g.Dispose();

            // return the new image

            return (Image)newImage;

        }

        /// <summary>
        /// Threshold the image using the given value.
        /// </summary>
        /// <param name="source image ( will be overwritten with thresholded image)"></param>
        /// <param name="the threshold value ( 0..1)"></param>
        public static void ThresholdImage(Image source, float t)
        {

            // create image attributes to blit image into itself with.

            ImageAttributes ia = new ImageAttributes();

            ia.SetThreshold(t);

            // get a graphics for source image

            Graphics g = Graphics.FromImage(source);

            // set compositing mode to copy over

            g.CompositingMode = CompositingMode.SourceCopy;

            // turn off aliasing etc

            g.InterpolationMode = InterpolationMode.NearestNeighbor;

            g.SmoothingMode = SmoothingMode.None;

            // create destination rectangle

            Rectangle d = new Rectangle(0, 0, source.Width, source.Height);

            // paint into self

            g.DrawImage(source, d, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, ia);

            // cleanup

            ia.Dispose();

            g.Dispose();

        }

        /// <summary>
        /// Gray scale the given image in situ. The luminance value must be 0..1 and is resulting brightness of
        /// the image. Use a value of 0.5 for the default brightness
        /// </summary>
        /// <param name="source"></param>
        /// <param name="luminance"></param>
        /// 

        /// <summary>
        /// RGB scale components for gray scaling. These values are correct for RGB linear space unlike the commonly quoted
        /// values which are infact for YIQ space. see http://www.sgi.com/software/opengl/advanced98/notes/node182.html for more detaila
        /// </summary>
        private const float kR = 0.3086f;

        private const float kG = 0.6094f;

        private const float kB = 0.0820f;

        public static void GrayScaleImage(Image source, float lum)
        {

            // create image attributes to blit image into itself with.

            ImageAttributes ia = new ImageAttributes();

            // create gray scale color matrix that will also translate colors according to brightness.
            // NOTE: The alpha translate is 0.0 so that transparent pixels will be unaffected.

            ColorMatrix cm = new ColorMatrix();

            cm[0, 0] = kR; cm[0, 1] = kR; cm[0, 2] = kR; cm[0, 3] = 0.0f; cm[0, 4] = 0.0f;
            cm[1, 0] = kG; cm[1, 1] = kG; cm[1, 2] = kG; cm[1, 3] = 0.0f; cm[1, 4] = 0.0f;
            cm[2, 0] = kB; cm[2, 1] = kB; cm[2, 2] = kB; cm[2, 3] = 0.0f; cm[2, 4] = 0.0f;
            cm[3, 0] = 0.000f; cm[3, 1] = 0.000f; cm[3, 2] = 0.000f; cm[3, 3] = 1.0f; cm[3, 4] = 0.0f;
            cm[4, 0] = lum; cm[4, 1] = lum; cm[4, 2] = lum; cm[4, 3] = 0.0f; cm[4, 4] = 1.0f;

            // associate matrix with attributes

            ia.SetColorMatrix(cm);

            // get a graphics for source image

            Graphics g = Graphics.FromImage(source);

            // set compositing mode to copy over

            g.CompositingMode = CompositingMode.SourceCopy;

            // turn off aliasing etc

            g.InterpolationMode = InterpolationMode.NearestNeighbor;

            g.SmoothingMode = SmoothingMode.None;

            // create destination rectangle

            Rectangle d = new Rectangle(0, 0, source.Width, source.Height);

            // paint into self

            g.DrawImage(source, d, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, ia);

            // cleanup

            ia.Dispose();

            g.Dispose();

        }

        /// <summary>
        /// Create a new YUV image from an RGB image ( alpha unchanged )
        /// </summary>
        public static Image CreateYUVAfromRGBAImage(Image source)
        {

            // image to be returned

            Image newImage = null;

            // create image attributes to blit image into itself with.

            using (ImageAttributes ia = new ImageAttributes())
            {

                // use color matrix for conversion

                XColorMatrix xcm = new XColorMatrix();

                xcm.RGBtoYUV();

                // associate matrix with attributes

                ia.SetColorMatrix(xcm.ColorMatrix);

                // create new image

                newImage = CreateImage(source.Width, source.Height, source.PixelFormat);

                // get a graphics for target image

                using (Graphics g = Graphics.FromImage(newImage))
                {

                    // set compositing mode to copy over

                    g.CompositingMode = CompositingMode.SourceCopy;

                    // turn off aliasing etc

                    g.InterpolationMode = InterpolationMode.NearestNeighbor;

                    g.SmoothingMode = SmoothingMode.None;

                    // create destination rectangle

                    Rectangle d = new Rectangle(0, 0, source.Width, source.Height);

                    // paint into self

                    g.DrawImage(source, d, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, ia);

                }

            }

            // return YUV(A)

            return newImage;

        }

        /// <summary>
        /// Create a new RGB(A) image from an YUV(A) image ( alpha unchanged )
        /// </summary>
        public static Image CreateRGBAfromYUVAImage(Image source)
        {

            // image to be returned

            Image newImage = null;

            // create image attributes to blit image into itself with.

            using (ImageAttributes ia = new ImageAttributes())
            {

                // use color matrix for conversion

                XColorMatrix xcm = new XColorMatrix();

                xcm.YUVtoRGB();

                // associate matrix with attributes

                ia.SetColorMatrix(xcm.ColorMatrix);

                // create new image

                newImage = CreateImage(source.Width, source.Height, source.PixelFormat);

                // get a graphics for target image

                using (Graphics g = Graphics.FromImage(newImage))
                {

                    // set compositing mode to copy over

                    g.CompositingMode = CompositingMode.SourceCopy;

                    // turn off aliasing etc

                    g.InterpolationMode = InterpolationMode.NearestNeighbor;

                    g.SmoothingMode = SmoothingMode.None;

                    // create destination rectangle

                    Rectangle d = new Rectangle(0, 0, source.Width, source.Height);

                    // paint into self

                    g.DrawImage(source, d, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, ia);

                }

            }

            // return RGB(A)

            return newImage;

        }


        /// <summary>
        /// Scale the color and alpha channels of the image. Useful values for rgba are
        /// -1.0f to 1.0f with 0.0f resulting in no change to the channel
        /// </summary>
        /// <param name="source"></param>
        /// <param name="luminance"></param>
        public static void ScaleChannels(Image source, float rc, float gc, float bc, float ac)
        {
            // create image attributes

            ImageAttributes ia = new ImageAttributes();

            // create color matrix used to scale the channels

            ColorMatrix cm = new ColorMatrix();

            cm[0, 0] = 1.0f; cm[0, 1] = 0.0f; cm[0, 2] = 0.0f; cm[0, 3] = 0.0f; cm[0, 4] = 0.0f;
            cm[1, 0] = 0.0f; cm[1, 1] = 1.0f; cm[1, 2] = 0.0f; cm[1, 3] = 0.0f; cm[1, 4] = 0.0f;
            cm[2, 0] = 0.0f; cm[2, 1] = 0.0f; cm[2, 2] = 1.0f; cm[2, 3] = 0.0f; cm[2, 4] = 0.0f;
            cm[3, 0] = 0.0f; cm[3, 1] = 0.0f; cm[3, 2] = 0.0f; cm[3, 3] = 1.0f; cm[3, 4] = 0.0f;
            cm[4, 0] = rc; cm[4, 1] = gc; cm[4, 2] = bc; cm[4, 3] = ac; cm[4, 4] = 1.0f;

            ia.SetColorMatrix(cm);

            // get a graphics for source image

            Graphics g = Graphics.FromImage(source);

            // set compositing mode to copy over

            g.CompositingMode = CompositingMode.SourceCopy;

            // turn off aliasing etc

            g.InterpolationMode = InterpolationMode.NearestNeighbor;

            g.SmoothingMode = SmoothingMode.None;

            // create destination rectangle

            Rectangle d = new Rectangle(0, 0, source.Width, source.Height);

            // paint into self

            g.DrawImage(source, d, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, ia);

            // cleanup image attributes

            ia.Dispose();

            // cleanup graphics

            g.Dispose();


        }

        /// <summary>
        /// Redraw image into itself using the given gamma correction. 0.1 to 5.0 are allowed. 2.0 produces no change
        /// </summary>
        /// <param name="source"></param>
        /// <param name="luminance"></param>
        public static void SetGamma(Image source, float _gamma)
        {
            // clamp to range

            float gamma = Math.Max(0.1f, Math.Min(_gamma, 5.0f));

            // create image attributes

            ImageAttributes ia = new ImageAttributes();

            ia.SetGamma(gamma);

            // get a graphics for source image

            Graphics g = Graphics.FromImage(source);

            // set compositing mode to copy over

            g.CompositingMode = CompositingMode.SourceCopy;

            // turn off aliasing etc

            g.InterpolationMode = InterpolationMode.NearestNeighbor;

            g.SmoothingMode = SmoothingMode.None;

            // create destination rectangle

            Rectangle d = new Rectangle(0, 0, source.Width, source.Height);

            // paint into self

            g.DrawImage(source, d, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, ia);

            // cleanup

            ia.Dispose();

            g.Dispose();

        }

        /// <summary>
        /// Apply simple motion blur to the image. There should be equal numbers of entries in
        /// offsets and alphaValues. Offsets are the image offsets for successive motion frames.
        /// alphaValues are the alpha channel scale values for each frame. They should be
        /// set to -1 to 0. 0 produces no changes in the image. -1 creates a completely transparent image.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="offsets"></param>
        /// <param name="alphaValues"></param>
        public static void MotionBlurImage(Image source, PointF[] offsets, float[] alphaValues)
        {
            // sanity check parameters

            if (source == null)
                return;

            if ((offsets == null) || (alphaValues == null))
                return;

            if (offsets.Length != alphaValues.Length)
                return;


            // create new image transparent image

            Bitmap b = ImageUtility.CreateImage(source.Width, source.Height, PixelFormat.Format32bppPArgb, Color.Transparent);

            // get drawing surface for new image

            Graphics g = Graphics.FromImage(b);

            // of course we need compositing

            g.CompositingMode = CompositingMode.SourceOver;

            // iterate frames

            for (int i = 0; i < offsets.Length; i++)
            {

                // create cloned image and scale alpha

                Image temp = (Image)source.Clone();

                ImageUtility.ScaleChannels(temp, 0.0f, 0.0f, 0.0f, alphaValues[i]);

                // draw with correct offset

                g.DrawImage(temp, offsets[i].X, offsets[i].Y);

            }

            // draw original image at original location !

            g.DrawImage(source, 0, 0);

            // dispose surface

            g.Dispose();

            // draw new image into source image

            g = Graphics.FromImage(source);

            g.DrawImage(b, 0, 0);

            g.Dispose();
        }

        /// <summary>
        /// Create a mirror of the image in x and/or y
        /// </summary>
        /// <param name="source"></param>
        /// <param name="luminance"></param>
        public static void MirrorImage(Image source, bool xMirror, bool yMirror)
        {
            // get transform values

            float xTransform = (xMirror == true ? -1.0f : 1.0f);

            float yTransform = (yMirror == true ? -1.0f : 1.0f);

            // generate matrix to flip the image based on current settings

            Matrix m = new Matrix();

            m.Reset();

            // we are going to draw the image at the origin so transform so that the origin is at the center of image

            m.Translate((float)source.Width / 2.0f * xTransform, (float)source.Height / 2.0f * yTransform, MatrixOrder.Prepend);

            // scale which flips the image

            m.Scale(xTransform, yTransform, MatrixOrder.Append);

            // get a graphics object for the original image

            Graphics g = Graphics.FromImage(source);

            // set compositing mode to copy over

            g.CompositingMode = CompositingMode.SourceCopy;

            // turn off aliasing etc

            g.InterpolationMode = InterpolationMode.NearestNeighbor;

            g.SmoothingMode = SmoothingMode.None;

            // set transform into graphics

            g.Transform = m;

            // draw image at origin ( which is now translated to center of window )

            g.DrawImage(source, -((float)source.Width / 2.0f), -((float)source.Height / 2.0f));

            // dispose locals

            g.Dispose();

        }

        /// <summary>
        /// Uniformly scale RGB channels to change brightness. useful values are -1 to 1 with 0.0f
        /// causing no change
        /// </summary>
        /// <param name="source"></param>
        /// <param name="luminance"></param>
        public static void SetBrightness(Image source, float bt)
        {

            // create image attributes

            ImageAttributes ia = new ImageAttributes();

            // create color matrix used to scale the channels

            ColorMatrix cm = new ColorMatrix();

            cm[0, 0] = 1.0f; cm[0, 1] = 0.0f; cm[0, 2] = 0.0f; cm[0, 3] = 0.0f; cm[0, 4] = 0.0f;
            cm[1, 0] = 0.0f; cm[1, 1] = 1.0f; cm[1, 2] = 0.0f; cm[1, 3] = 0.0f; cm[1, 4] = 0.0f;
            cm[2, 0] = 0.0f; cm[2, 1] = 0.0f; cm[2, 2] = 1.0f; cm[2, 3] = 0.0f; cm[2, 4] = 0.0f;
            cm[3, 0] = 0.0f; cm[3, 1] = 0.0f; cm[3, 2] = 0.0f; cm[3, 3] = 1.0f; cm[3, 4] = 0.0f;
            cm[4, 0] = bt; cm[4, 1] = bt; cm[4, 2] = bt; cm[4, 3] = 0.0f; cm[4, 4] = 1.0f;

            // set matrix into attributes

            ia.SetColorMatrix(cm);

            // get a graphics for source image

            Graphics g = Graphics.FromImage(source);

            // set compositing mode to copy over

            g.CompositingMode = CompositingMode.SourceCopy;

            // turn off aliasing etc

            g.InterpolationMode = InterpolationMode.NearestNeighbor;

            g.SmoothingMode = SmoothingMode.None;

            // create destination rectangle

            Rectangle d = new Rectangle(0, 0, source.Width, source.Height);

            // paint into self

            g.DrawImage(source, d, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, ia);

            // cleanup

            ia.Dispose();

            g.Dispose();

        }


        /// <summary>
        /// Return an image that is the rotated version of the given image. Rotation is
        /// at 'angle' via 'center'. New image is the same size as the rotated image
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="center"></param>
        /// <returns></returns>
        public static Image RotateImage(Image source, float angle, PointF center)
        {

            // create new image

            Image newImage = CreateImage(source.Width, source.Height, PixelFormat.Format32bppPArgb, Color.Transparent);

            // get graphics surface

            Graphics g = Graphics.FromImage(newImage);

            // use high quality resampling

            g.SmoothingMode = SmoothingMode.HighQuality;

            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // setup transform matrix

            Matrix rm = new Matrix();

            rm.RotateAt(angle, center, MatrixOrder.Append);

            // insert transform into graphics

            g.Transform = rm;

            // draw image 

            g.DrawImage(source, center.X - (float)source.Width / 2.0f, center.Y - (float)source.Height / 2);

            // dispose locals

            g.Dispose();

            rm.Dispose();

            // return new image

            return newImage;

        }

        /// <summary>
        /// Widen path by positive amount in w
        /// </summary>
        /// <param name="gp"></param>
        /// <param name="w"></param>
        public static void WidenPath(GraphicsPath gp, float w)
        {
            if (gp == null)
                return;

            if (w < 0.0f)
                return;

            if (gp.GetBounds().IsEmpty)
                return;

            // create widening pen

            Pen widenPen = new Pen(Color.White, w);

            // by default pens are mitered, which will produce wierd results when widening a path

            widenPen.LineJoin = LineJoin.Bevel;

            // widen

            gp.Widen(widenPen);

            // cleanup pen

            widenPen.Dispose();
        }


        /// <summary>
        /// Scale and rotate a graphics path around 0,0 the moves path to the given
        /// origin
        /// </summary>
        /// <param name="gp"></param>
        /// <param name="origin"></param>
        /// <param name="rotation"></param>
        /// <param name="xScale"></param>
        /// <param name="yScale"></param>
        public static void ScaleRotateTranslatePath(GraphicsPath gp,
            PointF origin,
            float rotation,
            float xScale, float yScale)
        {

            // create affine transform

            Matrix m = new Matrix();

            // rotate and scale

            m.Scale(xScale, yScale, MatrixOrder.Prepend);

            m.Rotate(rotation, MatrixOrder.Prepend);

            m.Translate(origin.X, origin.Y, MatrixOrder.Append);

            // transform path

            gp.Transform(m);

        }

        /// <summary>
        /// Scale and rotate a graphics path around 0,0 the moves path to the given
        /// origin
        /// </summary>
        /// <param name="gp"></param>
        /// <param name="origin"></param>
        /// <param name="rotation"></param>
        /// <param name="xScale"></param>
        /// <param name="yScale"></param>
        public static PointF[] ScaleRotateTranslatePoints(PointF[] p0,
            PointF origin,
            float rotation,
            float xScale, float yScale)
        {
            // create affine transform

            Matrix m = new Matrix();

            // clone original points

            PointF[] p1 = (PointF[])p0.Clone();

            // rotate and scale

            m.Scale(xScale, yScale, MatrixOrder.Prepend);

            m.Rotate(rotation, MatrixOrder.Prepend);

            m.Translate(origin.X, origin.Y);

            // transform points

            m.TransformPoints(p1);

            // return points

            return p1;

        }

        /// <summary>
        /// Return a 3x3 convolution filter for sharpening a image.
        /// The amount of sharpening is simulated by a value 1..100 ( amount parameter )
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static float[] GetSharpenKernel(int amount)
        {

            float corner = 0.0f;

            float side = amount / -50.0f;

            float center = (side * -4.0f) + (corner * -4.0f) + 1.0f;

            float[] elements = new float[] {
											   corner, side, corner,
											   side,  center, side,
											   corner, side,  corner
										   };

            return elements;


        }

        /// <summary>
        /// Generate a convolution filter kernal for a gassian blur operation.
        /// Size of the diameter of the effective radius of the blur kernel. It
        /// must be odd ( i.e. 3,5,7,9 etc ). Amount is used to control
        /// the standard deviation factor of the gaussian calculation algorithm 
        /// and it affects the effectiveness of the blur operation.
        /// Useful ranges for the parameters are size[3..21] and amount[1..100].
        /// a size of 3 and an amount of 15 is a good default.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="standard_deviation"></param>
        /// <returns></returns>
        public static float[] GetGaussianBlurKernel(int size, int amount)
        {
            /*
            v = e ^ ( -(x*x + y*y) / (2 * sd * sd) )
            Where sd is the standard deviation and x and y are the relative position to the center.
            */

            float standard_deviation = (float)amount / 20.0f;

            double nominator = 2 * standard_deviation * standard_deviation;
            float[] values = new float[size * size];

            int center = (size - 1) / 2;
            int limit = size - 1;
            int xx;
            int yy;
            float sum = 0f;
            float value = 0f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if ((y <= center) && (x <= center))
                    {
                        if (x >= y)
                        {
                            //calculate new value
                            xx = center - x;
                            yy = center - y;
                            value = (float)Math.Exp(-(xx * xx + yy * yy) / nominator);
                            values[(y * size) + x] = value;
                            sum += value;
                        }
                        else
                        {
                            //copy existing value
                            value = values[(x * size) + y];
                            values[(y * size) + x] = value;
                            sum += value;
                        }
                    }
                    else
                    {
                        xx = x;
                        yy = y;
                        if (yy > center) yy = limit - yy;
                        if (xx > center) xx = limit - xx;
                        value = values[(yy * size) + xx];
                        values[(y * size) + x] = value;
                        sum += value;
                    }
                }
            }

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = values[i] / sum;
            }

            return values;
        }

        /// <summary>
        /// Get an convolution kernel for the unsharp operation. Its basically an
        /// center biased, inverse blur kernel
        /// </summary>
        /// <param name="size"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static float[] GetUnsharpKernel(int size, int amount)
        {
            float[] elements = GetGaussianBlurKernel(size, amount);

            int cen = ((size * size) - 1) / 2;

            elements[cen] = 0.0f;

            float sum = 0f;

            for (int i = 0; i < elements.Length; i++)
            {
                sum += elements[i];
                elements[i] = -elements[i];
            }
            elements[cen] = sum + 1;

            return elements;

        }

        /// <summary>
        /// Return an unsharpended image. Radius 0.. ImageUtility.kMAX_BLUR_RADIUS. depth == 0..kMAX_SHARP_DEPTH
        /// </summary>
        /// <param name="pSrc"></param>
        /// <param name="radius"></param>
        /// <param name="depth"></param>
        /// <param name="pRect"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static Bitmap GetUnsharpenedBitmap(Bitmap pSrc, float radius, float depth)
        {
            // falling pointers below

            unsafe
            {

                // Start with blurred image

                Bitmap pResult = ImageUtility.GetGuassianBlurredImage(pSrc, radius);

                // Subtract blurred bitmap from original to get Unsharp Mask

                Rectangle rcSrc = new Rectangle(0, 0, pSrc.Width, pSrc.Height);

                // lock source

                BitmapData dataSrc = pSrc.LockBits(rcSrc, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                Rectangle rcResult = rcSrc;

                BitmapData dataResult = pResult.LockBits(rcResult, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

                const int nPlanes = 4;

                // mask of planes to change

                int flags = 7;

                // On modern systems, the difference is not big, but real math is still somewhat
                // slower than integer math. But if this ever changes, you may define float_MATH.
#if float_MATH
				float depthPlus = depth + 1.0f;
#else
                int denom = 10000;	// use an arbitrary denominator, not too small
                int dpt = (int)((float)denom * depth);
                int dptplus = dpt + denom;
#endif

                byte* pStartSrc = (byte*)dataSrc.Scan0;
                byte* pStartResult = (byte*)dataResult.Scan0;

                for (int plane = 0; plane < nPlanes; plane++)	// loop through color planes
                {
                    bool bThisPlane = (flags & 1) != 0;
                    flags >>= 1;

                    byte* pLineSrc = pStartSrc;
                    byte* pLineResult = pStartResult;

                    if (bThisPlane)
                    {
                        for (uint line = 0; line < dataResult.Height; line++)	// loop through lines
                        {
                            byte* pPixelSrc = pLineSrc;
                            byte* pPixelResult = pLineResult;

                            for (uint pxl = 0; pxl < dataResult.Width; pxl++)	// loop through pixels
                            {
#if float_MATH
								float v = depthPlus * *pPixelSrc - depth * *pPixelResult;
								if (v > 255.0f) v = 255.0f;
								if (v < 0.0f) v = 0.0f;
#else
                                int v = dptplus * *pPixelSrc - dpt * *pPixelResult;
                                v /= denom;

                                // Clipping is very essential here. for large values of depth
                                // (> 5.0f) more than half of the pixel values are clipped.
                                if (v > 255) v = 255;
                                if (v < 0) v = 0;
#endif
                                *pPixelResult = (byte)v;
                                pPixelSrc += nPlanes;
                                pPixelResult += nPlanes;
                            }
                            pLineSrc += dataSrc.Stride;
                            pLineResult += dataResult.Stride;
                        }
                    }
                    else		// no subtraction, just copy
                    {
                        for (uint line = 0; line < dataResult.Height; line++)	// loop through lines
                        {
                            byte* pPixelSrc = pLineSrc;
                            byte* pPixelResult = pLineResult;

                            for (uint pxl = 0; pxl < dataResult.Width; pxl++)
                            {
                                *pPixelResult = *pPixelSrc;
                                pPixelSrc += nPlanes;
                                pPixelResult += nPlanes;
                            }

                            pLineSrc += dataSrc.Stride;
                            pLineResult += dataResult.Stride;
                        }	// next line
                    }

                    pStartSrc++;
                    pStartResult++;

                }	// next plane

                pResult.UnlockBits(dataResult);

                pSrc.UnlockBits(dataSrc);

                return pResult;

            }
        }

        /// <summary>
        /// Return a blurred version of the image. Acceptable values are 0.0f -> kMAX_BLUR_RADIUS
        /// </summary>
        /// <param name="source"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static Bitmap GetGuassianBlurredImage(Bitmap source, float radius)
        {
            ImageUtility.SetRadius(Math.Max(0, Math.Min(kMAX_BLUR_RADIUS, radius)));

            // mask of planes to effect

            uint mask = 15;

            using (Bitmap temp = ImageUtility.ConvoluteDimension(source, mask, true, true))
            {
                return ImageUtility.ConvoluteDimension(temp, mask, false, true);
            }

        }

        public static readonly uint kMAX_BLUR_RADIUS = 16;

        public static readonly uint kMAX_UNSHARP_DEPTH = 4;

        private static uint m_Dim;

        private static uint m_MaxDim = kMAX_BLUR_RADIUS * 5 + 1;

        private static int m_Denominator;

        private static int[] m_FilterVector;

        private static bool SetRadius(float radius)
        {

            m_FilterVector = null;

            m_Dim = 0;

            // d is the effective diameter in pixels; all weight factors outside d are assumed
            // to be zero. The factor 5.0 is somewhat arbitrary; a value between 4.0 and 6.0
            // is generally recommended.

            int d = (int)(5.0f * radius + 1.0f);

            if (d > m_MaxDim) return false;	// radius to great
            d |= 1;							// must be odd
            m_Dim = (uint)d;

            if (m_Dim == 0) return true;	// radius 0 is acceptable; effectively no convolution

            m_FilterVector = new int[m_Dim];
            d /= 2;

            float num = 2 * radius * radius;
            float f = (float)Math.Exp(d * d / num);
            m_Denominator = (int)(f);

            m_FilterVector[d] = m_Denominator;

            for (uint i = 1; i <= d; i++)
            {
                int i2 = -(int)(i * i);
                int v = (int)(f * Math.Exp(i2 / num));
                m_FilterVector[d - i] = v;
                m_FilterVector[d + i] = v;
                m_Denominator += 2 * v;
            }

            return true;
        }

        public static Bitmap ConvoluteDimension(Bitmap pSrc, uint flags, bool bHorizontal, bool bCopy)
        {
            unsafe // hard hat area, pointers in use.
            {

                fixed (int* filterVector = &m_FilterVector[0])
                {

                    Rectangle rc = new Rectangle(0, 0, pSrc.Width, pSrc.Height);

                    // if size of kernel is <= 1 then no tranformation, so just clone and return

                    if (m_Dim <= 1)
                        return pSrc.Clone() as Bitmap;

                    // lockBits on source

                    BitmapData dataSrc = pSrc.LockBits(rc, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                    uint d = m_Dim / 2;

                    int nPlanes = 4;

                    // create destination

                    Bitmap pDest = new Bitmap(pSrc.Width, pSrc.Height, PixelFormat.Format32bppArgb);

                    // lockBits on destination

                    BitmapData dataDest = pDest.LockBits(rc, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

                    byte* pStartSrc = (byte*)dataSrc.Scan0;

                    byte* pStartDest = (byte*)dataDest.Scan0;

                    uint nLines;		// number of lines (horizontal or vertical)
                    uint nPixels;		// number of pixels per line
                    uint dPixelSrc;		// pixel step in source
                    uint dPixelDest;	// pixel step in destination
                    uint dLineSrc;		// line step in source
                    uint dLineDest;		// line step in destination

                    if (bHorizontal)
                    {
                        nLines = (uint)dataDest.Height;
                        nPixels = (uint)dataDest.Width;
                        dPixelSrc = (uint)nPlanes;
                        dPixelDest = (uint)nPlanes;
                        dLineSrc = (uint)dataSrc.Stride;
                        dLineDest = (uint)dataDest.Stride;
                    }
                    else
                    {
                        nLines = (uint)dataDest.Width;
                        nPixels = (uint)dataDest.Height;
                        dPixelSrc = (uint)dataSrc.Stride;
                        dPixelDest = (uint)dataDest.Stride;
                        dLineSrc = (uint)nPlanes;
                        dLineDest = (uint)nPlanes;
                    }

                    // This line added in version 1.1: avoid overrun in small bitmaps.
                    if (d > nPixels / 2) d = nPixels / 2;

                    for (int plane = 0; plane < nPlanes; plane++)	// loop through color planes
                    {
                        bool bThisPlane = (flags & 1) != 0;

                        flags >>= 1;

                        byte* pLineSrc = pStartSrc;
                        byte* pLineDest = pStartDest;

                        if (bThisPlane)
                        {
                            for (uint line = 0; line < nLines; line++)	// loop through lines
                            {
                                byte* pPixelDest = pLineDest;

                                for (uint pxl = 0; pxl < d; pxl++)	// loop through pixels in left/top margin
                                {
                                    int* pFactors = filterVector + d - pxl;

                                    uint xEnd = pxl + d;
                                    if (xEnd > nPixels) xEnd = nPixels;

                                    int denom = 0;
                                    int sum = 0;

                                    byte* pPixelSrc = pLineSrc;

                                    for (uint x = 0; x < xEnd; x++)
                                    {
                                        denom += *pFactors;
                                        sum += *pFactors++ * *pPixelSrc;
                                        pPixelSrc += dPixelSrc;
                                    }

                                    if (denom != 0) sum /= denom;
                                    *pPixelDest = (byte)sum;

                                    pPixelDest += dPixelDest;
                                }

                                for (uint pxl = d; pxl < nPixels - d; pxl++)	// loop through pixels in main area
                                {
                                    int* pFactors = filterVector;
                                    int sum = 0;

                                    uint xBegin = pxl - d;
                                    byte* pPixelSrc = &pLineSrc[xBegin * dPixelSrc];

                                    for (uint x = xBegin; x <= pxl + d; x++)
                                    {
                                        sum += *pFactors++ * *pPixelSrc;
                                        pPixelSrc += dPixelSrc;
                                    }

                                    sum /= m_Denominator;
                                    *pPixelDest = (byte)sum;

                                    pPixelDest += dPixelDest;
                                }

                                for (uint pxl = nPixels - d; pxl < nPixels; pxl++)
                                // loop through pixels in right/bottom margin
                                {
                                    int* pFactors = filterVector;
                                    int denom = 0;
                                    int sum = 0;

                                    int xBegin = (int)pxl - (int)d;
                                    if (xBegin < 0) xBegin = 0;
                                    byte* pPixelSrc = &pLineSrc[xBegin * dPixelSrc];

                                    for (uint x = (uint)xBegin; x < nPixels; x++)
                                    {
                                        denom += *pFactors;
                                        sum += *pFactors++ * *pPixelSrc;
                                        pPixelSrc += dPixelSrc;
                                    }

                                    if (denom != 0) sum /= denom;

                                    *pPixelDest = (byte)sum;

                                    pPixelDest += dPixelDest;
                                }

                                pLineSrc += dLineSrc;
                                pLineDest += dLineDest;
                            }	// next line

                        }
                        else if (bCopy)		// no convolution, just copy
                        {
                            for (uint line = 0; line < nLines; line++)	// loop through lines
                            {
                                byte* pPixelSrc = pLineSrc;
                                byte* pPixelDest = pLineDest;

                                for (uint pxl = 0; pxl < nPixels; pxl++) // loop through pixels
                                {
                                    *pPixelDest = *pPixelSrc;
                                    pPixelSrc += dPixelSrc;
                                    pPixelDest += dPixelDest;
                                }

                                pLineSrc += dLineSrc;
                                pLineDest += dLineDest;
                            }	// next line

                        }

                        pStartSrc++;
                        pStartDest++;
                    }	// next plane

                    pDest.UnlockBits(dataDest);
                    pSrc.UnlockBits(dataSrc);

                    return pDest;
                }

            }

        }


        /// <summary>
        /// reusable no change lookup table
        /// </summary>
        public static byte[] knop_lut = null;
        public static byte[] kNOP_LUT
        {
            get
            {
                if (ImageUtility.knop_lut == null)
                {
                    ImageUtility.knop_lut = new byte[256];

                    for (int i = 0; i < 256; i++)
                        ImageUtility.knop_lut[i] = (byte)i;
                }

                return ImageUtility.knop_lut;
            }
        }

        /// <summary>
        /// Use the supplied lookup tables to convert each byte of each channel. If a channel is null
        /// the channel will be unchanged.
        /// </summary>
        /// <param name="b"></param>
        public static void LUTImage(Bitmap b, byte[] rlut, byte[] glut, byte[] blut, byte[] alut)
        {
            // sanity check

            if (b == null)
                return;

            unsafe
            {

                // lock pixels

                BitmapData sbd = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

                // get pointer to start of image

                byte* sp = (byte*)(void*)sbd.Scan0;

                // iterate

                for (int y = 0; y < b.Height; y++)
                {
                    for (int x = 0; x < b.Width; x++)
                    {
                        sp[y * sbd.Stride + x * 4 + 0] = blut[sp[y * sbd.Stride + x * 4 + 0]];
                        sp[y * sbd.Stride + x * 4 + 1] = glut[sp[y * sbd.Stride + x * 4 + 1]];
                        sp[y * sbd.Stride + x * 4 + 2] = rlut[sp[y * sbd.Stride + x * 4 + 2]];
                        sp[y * sbd.Stride + x * 4 + 3] = alut[sp[y * sbd.Stride + x * 4 + 3]];

                    }
                }

                // unlock

                b.UnlockBits(sbd);

            }

        }

        /// <summary>
        /// Negate the RGB elements of the image, Leaves alpha unchanged
        /// </summary>
        /// <param name="b"></param>
        public static void NegateImage(Bitmap b)
        {
            // sanity check

            if (b == null)
                return;

            unsafe
            {

                // lock pixels

                BitmapData sbd = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

                // get pointer to start of image

                byte* sp = (byte*)(void*)sbd.Scan0;

                // iterate

                for (int y = 0; y < b.Height; y++)
                {
                    for (int x = 0; x < b.Width; x++)
                    {
                        sp[y * sbd.Stride + x * 4 + 0] = (byte)(255 - sp[y * sbd.Stride + x * 4 + 0]);
                        sp[y * sbd.Stride + x * 4 + 1] = (byte)(255 - sp[y * sbd.Stride + x * 4 + 1]);
                        sp[y * sbd.Stride + x * 4 + 2] = (byte)(255 - sp[y * sbd.Stride + x * 4 + 2]);
                    }
                }

                // unlock

                b.UnlockBits(sbd);

            }

        }

        /// <summary>
        /// Change contrast of original image. Valid values are -100 .. +100. Alpha values
        /// are unchanged
        /// </summary>
        /// <param name="b"></param>
        /// <param name="nContrast"></param>
        /// <returns></returns>
        public static bool Contrast(Bitmap b, sbyte nContrast)
        {
            unsafe
            {

                if (nContrast < -100) return false;

                if (nContrast > 100) return false;

                double pixel = 0, contrast = (100.0 + nContrast) / 100.0;

                contrast *= contrast;

                int red, green, blue;

                // lock pixels

                BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

                int stride = bmData.Stride;

                System.IntPtr Scan0 = bmData.Scan0;

                byte* p = (byte*)(void*)Scan0;

                int nOffset = stride - b.Width * 4;

                for (int y = 0; y < b.Height; ++y)
                {
                    for (int x = 0; x < b.Width; ++x)
                    {
                        blue = p[0];
                        green = p[1];
                        red = p[2];

                        pixel = red / 255.0;
                        pixel -= 0.5;
                        pixel *= contrast;
                        pixel += 0.5;
                        pixel *= 255;
                        if (pixel < 0) pixel = 0;
                        if (pixel > 255) pixel = 255;
                        p[2] = (byte)pixel;

                        pixel = green / 255.0;
                        pixel -= 0.5;
                        pixel *= contrast;
                        pixel += 0.5;
                        pixel *= 255;
                        if (pixel < 0) pixel = 0;
                        if (pixel > 255) pixel = 255;
                        p[1] = (byte)pixel;

                        pixel = blue / 255.0;
                        pixel -= 0.5;
                        pixel *= contrast;
                        pixel += 0.5;
                        pixel *= 255;
                        if (pixel < 0) pixel = 0;
                        if (pixel > 255) pixel = 255;
                        p[0] = (byte)pixel;

                        p += 4;
                    }
                    p += nOffset;
                }

                b.UnlockBits(bmData);

                return true;

            }
        }

        /// <summary>
        /// Create a new image which is equal to the source * the convolution filter.
        /// size is the row/column size of the kernel. e.g. if size is 3 then the kernal should
        /// contain ( at least ) 9 elements
        /// </summary>
        /// <param name="source"></param>
        /// <param name="kernel"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Bitmap Convolve(Bitmap source, float[] kernel, int size)
        {
            // we bad, we dangerous, thug code

            unsafe
            {

                // make width/height easy to read

                int w = source.Width;

                int h = source.Height;

                // if the size of the image is too small for the kernal return null

                if ((w < size) || (h < size))
                    return null;

                // create a destination image

                Bitmap destination = CreateImage(w, h, PixelFormat.Format32bppPArgb);

                // lock down source and destination

                BitmapData sbd = source.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);

                BitmapData dbd = destination.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);

                // get source/destination pixel pointers

                byte* sp = (byte*)(void*)sbd.Scan0;

                byte* dp = (byte*)(void*)dbd.Scan0;

                // iterate source image ( we have to ignore borders )

                int xmin = (size - 1) / 2;

                int xmax = w - xmin - 1;

                int ymin = (size - 1) / 2;

                int ymax = h - ymin - 1;

                // calculate half width - 1. Used to get pixel offsets
                // from a zero based index

                int hw = (size - 1) / 2;

                for (int y = ymin; y <= ymax; y++)
                {
                    for (int x = xmin; x <= xmax; x++)
                    {

                        // process a single pixel through the kernel 

                        float rsum, gsum, bsum, asum;

                        rsum = gsum = bsum = asum = 0.0f;

                        float ksum = 0.0f;

                        for (int j = 0; j < size; j++)
                        {
                            for (int i = 0; i < size; i++)
                            {

                                // calculate pixel offsets

                                int yp = hw - j;
                                int xp = hw - i;

                                float k = kernel[i + j * size];

                                bsum += k * sp[(y + yp) * sbd.Stride + (x + xp) * 4 + 0];

                                gsum += k * sp[(y + yp) * sbd.Stride + (x + xp) * 4 + 1];

                                rsum += k * sp[(y + yp) * sbd.Stride + (x + xp) * 4 + 2];

                                asum += k * sp[(y + yp) * sbd.Stride + (x + xp) * 4 + 3];

                                ksum += k;

                            }
                        }
                        // calculate new pixel values

                        byte red = (byte)Math.Min(255, Math.Max(rsum / ksum, 0));
                        byte green = (byte)Math.Min(255, Math.Max(gsum / ksum, 0));
                        byte blue = (byte)Math.Min(255, Math.Max(bsum / ksum, 0));
                        byte alpha = (byte)Math.Min(255, Math.Max(asum / ksum, 0));


                        // set destination pixel

                        dp[y * dbd.Stride + x * 4 + 0] = blue;
                        dp[y * dbd.Stride + x * 4 + 1] = green;
                        dp[y * dbd.Stride + x * 4 + 2] = red;
                        dp[y * dbd.Stride + x * 4 + 3] = alpha;

                    }
                }


                // unlock

                source.UnlockBits(sbd);

                destination.UnlockBits(dbd);

                // return new image

                return destination;

            }
        }


        /// <summary>
        /// Histogram equalization on a gray scale image. You can pass a color image but the resulting image
        /// is always gray anyway.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Bitmap EqualizeGrayImage(Bitmap s)
        {
            // get gray scale

            Bitmap g = ImageUtility.CreateImage(s, PixelFormat.Format32bppPArgb) as Bitmap;

            ImageUtility.GrayScaleImage(g, 0.0f);

            // create histogram for counting gray level incidences

            int[] h = new int[256];

            byte[] lut = new byte[256];

            // max gray value found

            int max = 0;

            // lock down 

            unsafe
            {

                BitmapData bd = g.LockBits(new Rectangle(0, 0, g.Width, g.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);

                // get source pixel pointers

                byte* sp = (byte*)(void*)bd.Scan0;

                // iterate pixels

                for (uint y = 0; y < g.Height; y++)
                {
                    uint row = y * (uint)bd.Stride;

                    for (uint x = 0; x < g.Width; x++)
                    {
                        // pixels are arrange BGRA ! we can use any of BGR

                        byte v = sp[row + x * 4 + 0];

                        h[v]++;

                        // update max value found

                        max = Math.Max(max, (int)v);

                    }
                }


                // normalize the histogram and and create a color remap table

                int t = g.Width * g.Height;

                int sum = 0;

                for (int i = 0; i <= max; i++)
                {
                    sum += h[i];

                    lut[i] = (byte)(sum * max / t);
                }

                // transform pixels in gray image

                for (uint y = 0; y < g.Height; y++)
                {

                    uint row = y * (uint)bd.Stride;

                    for (uint x = 0; x < g.Width; x++)
                    {
                        // get new gray value by using old value as lookup into lut

                        byte v = lut[sp[row + x * 4 + 0]];

                        // write into BGR

                        sp[row + x * 4 + 0] =

                            sp[row + x * 4 + 1] =

                            sp[row + x * 4 + 2] = v;


                    }
                }

                // unlock gray image

                g.UnlockBits(bd);

            }


            // return gray image

            return g;

        }


        /// <summary>
        /// Perform a basic histogram equalization on the image. Use gray levels from a gray scale version of the source image
        /// and construct a new image using the normalized histogram. The new image is constructed by modifying the L component of
        /// an HSL image thus this function works for color images as well as gray scale images.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Bitmap Equalize(Bitmap s)
        {
            // get gray scale

            Bitmap g = ImageUtility.CreateImage(s, PixelFormat.Format32bppPArgb) as Bitmap;

            ImageUtility.GrayScaleImage(g, 0.0f);

            // get HSLA map of source image

            float[] hlsa = ImageUtility.GetHSLAImage(s);

            // create output image

            Bitmap o = ImageUtility.CreateImage(s.Width, s.Height, PixelFormat.Format32bppPArgb);

            // create histogram for counting gray level incidences

            int[] h = new int[256];

            byte[] lut = new byte[256];

            // max gray value found

            int max = 0;

            // lock down 

            unsafe
            {

                BitmapData bd = g.LockBits(new Rectangle(0, 0, g.Width, g.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);

                // get source pixel pointers

                byte* sp = (byte*)(void*)bd.Scan0;

                // iterate pixels

                for (int y = 0; y < g.Height; y++)
                {
                    for (int x = 0; x < g.Width; x++)
                    {
                        // pixels are arrange BGRA ! we can use any of BGR

                        byte v = sp[y * bd.Stride + x * 4 + 0];

                        h[v]++;

                        // update max value found

                        max = Math.Max(max, (int)v);

                    }
                }


                // normalize the histogram and fill out lut

                float t = g.Width * g.Height;

                float sum = 0;

                for (int i = 0; i <= max; i++)
                {
                    sum += h[i];

                    lut[i] = (byte)(sum * max / t);
                }

                // lockdown the output image

                BitmapData od = o.LockBits(new Rectangle(0, 0, o.Width, o.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);

                // get source pixel pointers

                byte* op = (byte*)(void*)od.Scan0;

                // transform pixels in gray image

                for (int y = 0; y < o.Height; y++)
                {
                    for (int x = 0; x < o.Width; x++)
                    {

                        // get H/S values from HSLA map and luminance from lookup table

                        float H = hlsa[(x + y * s.Width) * 4 + 0];

                        float S = hlsa[(x + y * s.Width) * 4 + 1];

                        float L = lut[sp[y * bd.Stride + x * 4 + 0]] / 255.0f;

                        float A = hlsa[(x + y * s.Width) * 4 + 3];

                        // get destination pixel

                        Color oc = RgbaHls.ColorFromHSLA(H, S, L, A);

                        // write destination BGRA

                        op[y * od.Stride + x * 4 + 0] = oc.B;

                        op[y * od.Stride + x * 4 + 1] = oc.G;

                        op[y * od.Stride + x * 4 + 2] = oc.R;

                        op[y * od.Stride + x * 4 + 3] = oc.A;


                    }
                }

                // unlock output image

                o.UnlockBits(od);


                // unlock gray image

                g.UnlockBits(bd);

            }

            // dispose temporary gray scale

            g.Dispose();

            // return gray image

            return o;

        }

        /// <summary>
        /// Return an array of hue, saturation, luminance, alpha ( 0..1 ) of the source image.
        /// Hue		= [ ( x + y * width ) * 4 + 0 ]
        /// Sat		= [ ( x + y * width ) * 4 + 1 ]
        /// Lum		= [ ( x + y * width ) * 4 + 2 ]
        /// Alpha	= [ ( x + y * width ) * 4 + 3 ]
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static float[] GetHSLAImage(Bitmap source)
        {
            // sanity check

            if (source == null)
                return null;

            // create output array 4 floats per pixel

            float[] a = new float[source.Width * source.Height * 4];

            // lock down source image

            unsafe
            {

                BitmapData bd = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);

                // get source pixel pointers

                byte* sp = (byte*)(void*)bd.Scan0;

                // iterate pixels

                for (int y = 0; y < source.Height; y++)
                {
                    for (int x = 0; x < source.Width; x++)
                    {
                        // pixels are arrange BGRA !

                        Color c = Color.FromArgb(sp[y * bd.Stride + x * 4 + 3],			// A
                            sp[y * bd.Stride + x * 4 + 2],			// R
                            sp[y * bd.Stride + x * 4 + 1],			// G
                            sp[y * bd.Stride + x * 4 + 0]);			// B

                        // Get HSLA

                        RgbaHls hlsa = new RgbaHls(c);

                        // write values as float into array

                        a[(x + y * source.Width) * 4 + 0] = (float)hlsa.H;
                        a[(x + y * source.Width) * 4 + 1] = (float)hlsa.S;
                        a[(x + y * source.Width) * 4 + 2] = (float)hlsa.L;
                        a[(x + y * source.Width) * 4 + 3] = (float)hlsa.A;

                    }
                }

                // unlock bitmap

                source.UnlockBits(bd);

            }

            // return float array

            return a;

        }

        /// <summary>
        /// Draw the image into itself using the given image attributes
        /// </summary>
        /// <param name="source"></param>
        /// <param name="ia"></param>
        public static void ApplyImageAttributes(Image source, ImageAttributes ia)
        {
            // get graphics

            Graphics g = Graphics.FromImage(source);

            // source over mode

            g.CompositingMode = CompositingMode.SourceCopy;

            // create source/destination rectangle

            Rectangle r = new Rectangle(0, 0, source.Width, source.Height);

            // draw

            g.DrawImage(source, r, 0, 0, r.Width, r.Height, GraphicsUnit.Pixel, ia);

            // Dispose graphics

            g.Dispose();
        }

    }


    /// <summary>
    /// A blob map is a collection of BlobIslands. They are created from images that are assumed to be binary i.e. each
    /// pixels is either white or black. The BlobMap is constructed by grouping 4np pixels that are white and marking
    /// the map with the index of the island that each pixel belongs to. Additionally the BlobMap contains a list
    /// of all islands. For each island the number island index, number of pixels, the seed location and the edge connectedness
    /// is record ( edge connectedness means does this island touch the edge of the image ).
    /// </summary>
    public class BlobMap
    {
        /// <summary>
        /// This 2d array will be the same dimensions as the original image i.e. width * height.
        /// For each pixel in the source image the map element represents to the index of the island
        /// the pixel belongs to. -1 is used to indicate the pixel does not belog to any island
        /// </summary>
        protected int[] map;
        public int[] Map
        {
            get
            {
                return this.map;
            }
        }

        /// <summary>
        /// Width of blob map
        /// </summary>
        protected int width;
        public int Width
        {
            get
            {
                return this.width;
            }

        }
        /// <summary>
        /// height of blob map
        /// </summary>
        protected int height;
        public int Height
        {
            get
            {
                return this.height;
            }

        }

        /// <summary>
        /// All the blob islands discovered in the image
        /// </summary>
        protected ArrayList islands;
        public ArrayList Islands
        {
            get
            {
                return this.islands;
            }
        }

        /// <summary>
        /// Used by certain of the recursive ( or tail end recursion removed ) algorithms that require to know if the element has
        /// already been traversed.
        /// </summary>
        private bool[] visited;

        /// <summary>
        /// Construct a new blob map from the image
        /// </summary>
        /// <param name="i"></param>
        public BlobMap(Bitmap b)
        {
            // record dimensions

            this.width = b.Width;

            this.height = b.Height;

            // create pixel map

            this.map = new int[this.Width * this.Height];

            // set all pixels to -1 initially

            for (int reset = 0; reset < this.Width * this.Height; reset++)
                map[reset] = -1;

            // creat empty collection of islands

            this.islands = new ArrayList();

            // scan image

            for (int y = 0; y < this.height; y++)
            {
                for (int x = 0; x < this.width; x++)
                {

                    // ignore pixels already processed

                    if (this.map[y * this.width + x] == -1)
                    {

                        // get pixel color

                        Color pixel = b.GetPixel(x, y);

                        // if any components not zero consider it a 'set' pixel

                        if ((pixel.R + pixel.G + pixel.B) > 0)
                        {
                            // this is the start of a new island

                            this.islands.Add(CreateIsland(x, y, this.islands.Count, b));

                        }
                    }
                }
            }

            // sort island

            this.islands.Sort(null);

            // setup visited array

            this.visited = new bool[this.width * this.height];

        }

        /// <summary>
        /// Reset all members of the visited array
        /// </summary>
        protected void ResetVisited()
        {
            for (int i = 0; i < this.width * this.height; i++)
                this.visited[i] = false;
        }


        /// <summary>
        /// Return a new bitmap with black and white swapped.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static void InvertImage(Bitmap b)
        {

            for (int y = 0; y < b.Height; y++)
            {
                for (int x = 0; x < b.Width; x++)
                {
                    Color p = b.GetPixel(x, y);

                    if ((p.R + p.G + p.B) == 0)
                        b.SetPixel(x, y, Color.FromArgb(255, 255, 255, 255));
                    else
                        b.SetPixel(x, y, Color.FromArgb(255, 0, 0, 0));
                }
            }

        }

        /// <summary>
        /// This should be used on a binary image that has been inverted. It works the same as RemoveIslands EXCEPT
        /// it will rmove all small islands EXCEPT those that are edge connected.
        /// </summary>
        /// <param name="b"></param>
        public void FillHoles(Bitmap b)
        {

            // reset visited array

            this.ResetVisited();

            // color all islands black that are not edge connected

            foreach (BlobIsland island in this.islands)
            {
                if (island.edgeConnected == false)
                {

                    // color it black

                    this.colorIslandStack.Clear();

                    // seed stack

                    this.colorIslandStack.Push(island.seedLocation);

                    // color all island pixels black

                    this.ColorIsland(island.index, Color.FromArgb(255, 0, 0, 0), b);
                }
            }

        }

        /// <summary>
        /// Remove small islands that meet certain criteria. Assumes islands are sorted largest to highest.
        /// Will always try to leave at least one island.
        /// </summary>
        /// <param name="b"></param>
        public void RemoveIslands(Bitmap b)
        {
            // try to leave one island

            if (this.islands.Count <= 1)
                return;

            // reset visited array

            this.ResetVisited();

            // iterate all island

            while (this.islands.Count > 1)
            {
                // get next smallest island

                BlobIsland island = this.islands[this.islands.Count - 1] as BlobIsland;

                // remove from islands

                this.islands.RemoveAt(this.islands.Count - 1);

                // color it black

                this.colorIslandStack.Clear();

                this.colorIslandStack.Push(island.seedLocation);

                this.ColorIsland(island.index, Color.FromArgb(255, 0, 0, 0), b);

            }
        }

        /// <summary>
        /// useful for debugging. Colors the pixels corresponding to the islands with different colors
        /// </summary>
        /// <param name="b"></param>
        public void ColorIslands(Bitmap b)
        {
            Color[] islandColors = new Color[] {
												   Color.FromKnownColor( KnownColor.Red ),
												   Color.FromKnownColor( KnownColor.Green ),
												   Color.FromKnownColor( KnownColor.Blue ),
												   Color.FromKnownColor( KnownColor.Cyan ),
												   Color.FromKnownColor( KnownColor.Yellow ),
												   Color.FromKnownColor( KnownColor.Magenta ),
												   Color.FromKnownColor( KnownColor.Orange ),
												   Color.FromKnownColor( KnownColor.Lavender ),
												   Color.FromKnownColor( KnownColor.LimeGreen ),
												   Color.FromKnownColor( KnownColor.HotPink ),
												   Color.FromKnownColor( KnownColor.SkyBlue ),
											   
			};

            int colorIndex = 0;

            // reset visited array

            this.ResetVisited();

            // iterate islands

            foreach (BlobIsland island in this.islands)
            {
                // seed the colorIsland stack

                this.colorIslandStack.Clear();

                this.colorIslandStack.Push(island.seedLocation);

                this.ColorIsland(island.index, islandColors[colorIndex], b);

                colorIndex++;

                if (colorIndex >= islandColors.Length)
                    colorIndex = 0;
            }

        }

        /// <summary>
        /// Stack used for tail-end recursion removal in ColorIsland
        /// </summary>
        private Stack colorIslandStack = new Stack();

        /// <summary>
        /// Set all the pixels in the given island to the given color
        /// </summary>
        /// <param name="island"></param>
        /// <param name="c"></param>
        protected void ColorIsland(int index, Color c, Bitmap b)
        {

            // iterate until stack empty

            while (this.colorIslandStack.Count > 0)
            {

                // get top of stack

                Point p = (Point)this.colorIslandStack.Pop();

                int x = p.X;

                int y = p.Y;

                // if out of bounds then bail

                if (x < 0)
                    break;

                if (y < 0)
                    break;

                if (x >= this.width)
                    break;

                if (y >= this.height)
                    break;

                // continue if pixel not visited

                if (this.visited[y * this.width + x] == false)
                {

                    // proceed if this pixel is the same as the given index

                    if (this.map[y * this.width + x] == index)
                    {

                        // mark as visited

                        this.visited[y * this.width + x] = true;

                        // set pixel in bitmap

                        b.SetPixel(x, y, c);

                        // pop neighbors onto stack

                        this.colorIslandStack.Push(new Point(x, y - 1));

                        this.colorIslandStack.Push(new Point(x - 1, y));

                        this.colorIslandStack.Push(new Point(x + 1, y));

                        this.colorIslandStack.Push(new Point(x, y + 1));


                    }

                }

            }



        }



        /// <summary>
        /// Create an island at the given location. Count and flag all 4np pixels
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        protected BlobIsland CreateIsland(int x, int y, int index, Bitmap b)
        {
            // create new island

            BlobIsland island = new BlobIsland();

            // set seed location

            island.seedLocation = new Point(x, y);

            // reset pixel count

            island.pixelCount = 0;

            // defaults to not edge connected until proven otherwise

            island.edgeConnected = false;

            // set index

            island.index = index;

            // discover island starting at this location

            this.FindNeighborsStack.Clear();

            this.FindNeighborsStack.Push(island.seedLocation);

            this.FindNeighbors(b, island);

            // return island

            return island;

        }

        // used to store the recursion stack for FindNeighbors

        private Stack FindNeighborsStack = new Stack();


        /// <summary>
        /// Find all the unassigned neighbors of the given pixel and update stats of the given island
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        protected void FindNeighbors(Bitmap b, BlobIsland island)
        {
            // keep iterating while there are items on the stack

            while (this.FindNeighborsStack.Count > 0)
            {
                // get top of stack

                Point p = (Point)this.FindNeighborsStack.Pop();

                // if out of bounds then ignore

                if (p.X < 0)
                    break;

                if (p.Y < 0)
                    break;

                if (p.X >= this.width)
                    break;

                if (p.Y >= this.height)
                    break;

                // continue if this pixel is not assigned to an island

                if (this.map[p.Y * this.width + p.X] == -1)
                {

                    // continue only if the pixel is not black

                    Color pixel = b.GetPixel(p.X, p.Y);

                    // if any components not zero consider it a 'set' pixel

                    if ((pixel.R + pixel.G + pixel.B) > 0)
                    {

                        // mark pixel with our index

                        this.map[p.Y * this.width + p.X] = island.index;

                        // bump pixel count

                        island.pixelCount++;

                        // if this is an edge pixel then flag island as belonging to an edge

                        if ((p.X == 0) || (p.Y == 0) || (p.X == this.width - 1) || (p.Y == this.height - 1))
                            island.edgeConnected = true;

                        // push neighbors onto stack and keep trying

                        this.FindNeighborsStack.Push(new Point(p.X, p.Y - 1));

                        this.FindNeighborsStack.Push(new Point(p.X - 1, p.Y));

                        this.FindNeighborsStack.Push(new Point(p.X + 1, p.Y));

                        this.FindNeighborsStack.Push(new Point(p.X, p.Y + 1));

                    }

                }

            }
        }
    }

    /// <summary>
    /// An individual island
    /// </summary>
    public class BlobIsland : IComparable
    {
        /// <summary>
        /// index of this island
        /// </summary>
        public int index;

        /// <summary>
        /// Number of pixels in this island
        /// </summary>
        public int pixelCount;

        /// <summary>
        /// The first pixel location we used to discover this island
        /// </summary>
        public Point seedLocation;

        /// <summary>
        /// True if this island is juxtaposed with the edge of the image 
        /// </summary>
        public bool edgeConnected;

        #region IComparable Members

        /// <summary>
        /// used to sort island
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            return (obj as BlobIsland).pixelCount - this.pixelCount;
        }

        #endregion
    }
    /// <summary>
    /// A class used to represent a gray scale image. Used for image processing. It is not directly renderable. Call GetBitmap to get something
    /// to render ( should only be used for slow/debug code )
    /// </summary>
    public class GrayMap
    {
        /// <summary>
        /// Width of gray map
        /// </summary>
        protected int width;
        public int Width
        {
            get
            {
                return this.width;
            }
        }
        /// <summary>
        /// Height of gray map
        /// </summary>
        protected int height;
        public int Height
        {
            get
            {
                return this.height;
            }
        }

        /// <summary>
        /// Our pixel data
        /// </summary>
        protected byte[] pixels;
        public byte[] Pixels
        {
            get
            {
                return this.pixels;
            }
        }

        /// <summary>
        /// not a valid constructor
        /// </summary>
        private GrayMap()
        {
        }

        /// <summary>
        /// Create a gray map of the requested size and color
        /// </summary>
        /// <param name="w"></param>
        /// <param name="h"></param>
        public GrayMap(int w, int h, byte initialValue)
        {

            // sanity check

            if ((w <= 0) || (h <= 0))
                throw new Exception("GrayMap objects must have a positive, non-zero width/height");

            // record width and height

            this.width = w;

            this.height = h;

            // create map

            this.pixels = new byte[w * h];

            // fill with initial pixel value

            for (int i = 0; i < w * h; i++)
                this.pixels[i] = initialValue;

        }

        /// <summary>
        /// Create a gray map of the requested size 
        /// </summary>
        /// <param name="w"></param>
        /// <param name="h"></param>
        public GrayMap(int w, int h)
        {

            // sanity check

            if ((w <= 0) || (h <= 0))
                throw new Exception("GrayMap objects must have a positive, non-zero width/height");

            // record width and height

            this.width = w;

            this.height = h;

            // create map

            this.pixels = new byte[w * h];

        }

        /// <summary>
        /// Return value at given location. No range checking for speed
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public byte GetPixel(int x, int y)
        {
            return this.pixels[y * this.width + x];
        }

        /// <summary>
        /// Set pixel at given location, no range checking for speed
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void SetPixel(int x, int y, byte v)
        {
            this.pixels[y * this.width + x] = v;
        }

        /// <summary>
        /// Threahold the image. Pixels less than/equal to t become 0, above t become 255
        /// </summary>
        /// <param name="t"></param>
        public void Threshold(byte t)
        {
            // create a remap table

            byte[] remap = new byte[256];

            for (int r = 0; r < 256; r++)
            {
                if (r <= t)
                    remap[r] = 0;
                else
                    remap[r] = 255;
            }

            // remap all pixels

            for (int i = 0; i < this.width * this.height; i++)
                this.pixels[i] = remap[this.pixels[i]];

        }


        /// <summary>
        /// Really only for debugging. Returns a drawable gray scale bitmap of the GrayMap. You are responsble for disposing of this image.
        /// </summary>
        /// <returns></returns>
        public Bitmap GetBitmap()
        {
            // create image

            Bitmap b = new Bitmap(this.width, this.height, PixelFormat.Format32bppPArgb);

            // set all pixels

            for (int y = 0; y < this.height; y++)
            {
                for (int x = 0; x < this.width; x++)
                {
                    byte p = this.GetPixel(x, y);

                    b.SetPixel(x, y, Color.FromArgb(255, p, p, p));
                }
            }

            // return image

            return b;
        }

    }

}
