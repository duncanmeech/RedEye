using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace RedEye
{
    /// <summary>
    /// Maintains a system ColorMatrix and extended its functionality
    /// </summary>
    public class XColorMatrix : ICloneable
    {


        /// <summary>
        /// These members are used by calls the to RotateHue member. They are initialized on demand only 
        /// </summary>
        static private bool initialized = false;

        static private XColorMatrix preHue = new XColorMatrix();

        static private XColorMatrix postHue = new XColorMatrix();

        // The luminance weight factors for the RGB color space.
        // These values are actually preferable to the better known factors of
        // Y = 0.30R + 0.59G + 0.11B, the formula which is used in color television technique.

        static readonly float kLUMR = 0.3086f;
        static readonly float kLUMG = 0.6094f;
        static readonly float kLUMB = 0.0820f;


        /// <summary>
        /// The system color matrix we wrap
        /// </summary>
        protected ColorMatrix cm = new ColorMatrix();
        public ColorMatrix ColorMatrix
        {
            get
            {
                return this.cm;
            }

        }

        /// <summary>
        /// Basic constructor
        /// </summary>
        public XColorMatrix()
        {
            // ensure at identity

            this.MakeIdentity();
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="other"></param>
        public XColorMatrix(XColorMatrix other)
        {
            // copy from other

            for (int r = 0; r < 5; r++)
                for (int c = 0; c < 5; c++)
                    this.cm[r, c] = other.ColorMatrix[r, c];
        }

        /// <summary>
        /// Copy constructor ( from a system ColorMatrix )
        /// </summary>
        /// <param name="other"></param>
        public XColorMatrix(ColorMatrix other)
        {
            // copy from other

            for (int r = 0; r < 5; r++)
                for (int c = 0; c < 5; c++)
                    this.cm[r, c] = other[r, c];
        }

        /// <summary>
        /// Resets the matrix to identity
        /// </summary>
        public void MakeIdentity()
        {
            // reset all to zero

            for (int r = 0; r < 5; r++)
                for (int c = 0; c < 5; c++)
                    this.cm[r, c] = 0;

            // put a 1 in all scaling elements

            for (int i = 0; i < 5; i++)
                this.cm[i, i] = 1;


        }

        /// <summary>
        /// Rotate the color by specifying the indices in the matrix to recieve the rotation value
        /// phi is the angle -180 -> +180
        /// </summary>
        /// <param name="phi"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="order"></param>

        public void RotateColor(float phi, int x, int y, MatrixOrder order)
        {
            // convert degrees to radians

            phi *= (float)Math.PI / 180.0f;

            // get matrix to multiply by

            XColorMatrix m = new XColorMatrix();

            m.cm[x, x] = m.cm[y, y] = (float)Math.Cos(phi);

            float s = (float)Math.Sin(phi);

            m.cm[y, x] = s;
            m.cm[x, y] = -s;

            Multiply(m, order);
        }

        /// <summary>
        /// Rotate around red axis
        /// </summary>
        /// <param name="a"></param>
        /// <param name="order"></param>
        public void RotateRed(float a, MatrixOrder order)
        {
            this.RotateColor(a, 2, 1, order);
        }

        /// <summary>
        /// Rotate around green axis
        /// </summary>
        /// <param name="a"></param>
        /// <param name="order"></param>
        public void RotateGreen(float a, MatrixOrder order)
        {
            this.RotateColor(a, 0, 2, order);
        }

        /// <summary>
        /// Rotate around blue axis
        /// </summary>
        /// <param name="a"></param>
        /// <param name="order"></param>
        public void RotateBlue(float a, MatrixOrder order)
        {
            this.RotateColor(a, 1, 0, order);
        }

        /// <summary>
        /// Multiply given matrix by this using specified matrix order
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="order"></param>
        public void Multiply(XColorMatrix other, MatrixOrder order)
        {

            // if matrix order == Append then its other * this 
            // otherwise its this * other

            float[,] temp = new float[5, 5];

            if (order == MatrixOrder.Prepend)
            {
                for (int y = 0; y < 5; y++)
                    for (int x = 0; x < 5; x++)
                    {
                        float t = 0;
                        for (int i = 0; i < 5; i++)
                            t += other.ColorMatrix[y, i] * this.ColorMatrix[i, x];
                        temp[y, x] = t;
                    }

            }
            else
            {
                for (int y = 0; y < 5; y++)
                    for (int x = 0; x < 5; x++)
                    {
                        float t = 0;
                        for (int i = 0; i < 5; i++)
                            t += this.ColorMatrix[y, i] * other.ColorMatrix[i, x];
                        temp[y, x] = t;
                    }
            }

            for (int y = 0; y < 5; y++)
                for (int x = 0; x < 5; x++)
                    this.cm[y, x] = temp[y, x];


        }


        /// <summary>
        /// Multiply the vector ( assumed to be 4 floats ) by the matrix
        /// </summary>
        /// <param name="v"></param>
        public void TransformVector(float[] v)
        {
            float[] temp = new float[4];

            for (int x = 0; x < 4; x++)
            {
                temp[x] = this.cm[4, x];

                for (int y = 0; y < 4; y++)
                    temp[x] += v[y] * this.cm[y, x];
            }
            for (int x = 0; x < 4; x++)
                v[x] = temp[x];

        }

        /// <summary>
        /// Shear a color
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y1"></param>
        /// <param name="d1"></param>
        /// <param name="y2"></param>
        /// <param name="d2"></param>
        /// <param name="order"></param>
        public void ShearColor(int x, int y1, float d1, int y2, float d2, MatrixOrder order)
        {
            XColorMatrix m = new XColorMatrix();

            m.cm[y1, x] = d1;
            m.cm[y2, x] = d2;

            Multiply(m, order);
        }

        /// <summary>
        /// Shear the red plane
        /// </summary>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        /// <param name="order"></param>
        public void ShearRed(float green, float blue, MatrixOrder order)
        {
            this.ShearColor(0, 1, green, 2, blue, order);
        }

        /// <summary>
        /// Shear the green plane
        /// </summary>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        /// <param name="order"></param>
        public void ShearGreen(float red, float blue, MatrixOrder order)
        {
            this.ShearColor(1, 0, red, 2, blue, order);
        }

        /// <summary>
        /// Rotate the hue of the colors
        /// </summary>
        /// <param name="phi"></param>
        public void RotateHue(float phi)
        {
            // ensure we are initialized for hue rotations

            XColorMatrix.InitHue();

            // Rotate the grey vector to the blue axis.

            Multiply(XColorMatrix.preHue, MatrixOrder.Append);

            // Rotate around the blue axis

            RotateBlue(phi, MatrixOrder.Append);

            // compose with this

            Multiply(XColorMatrix.postHue, MatrixOrder.Append);
        }

        /// <summary>
        /// Prepare the preHue and postHue matricies to make hue rotation faster
        /// </summary>
        static public void InitHue()
        {
            const float greenRotation = 35.0f;

            //	const REAL greenRotation = 39.182655f;

            // NOTE: theoretically, greenRotation should have the value of 39.182655 degrees,
            // being the angle for which the sine is 1/(sqrt(3)), and the cosine is sqrt(2/3).
            // However, I found that using a slightly smaller angle works better.
            // In particular, the greys in the image are not visibly affected with the smaller
            // angle, while they deviate a little bit with the theoretical value.
            // An explanation escapes me for now.
            // If you rather stick with the theory, change the comments in the previous lines.


            if (XColorMatrix.initialized == false)
            {
                XColorMatrix.initialized = true;

                // Rotating the hue of an image is a rather convoluted task, involving several matrix
                // multiplications. For efficiency, we prepare two static matrices.
                // This is by far the most complicated part of this class. For the background
                // theory, refer to the sgi-sites mentioned at the top of this file.

                // Prepare the preHue matrix.
                // Rotate the grey vector in the green plane.

                preHue.RotateRed(45.0f, MatrixOrder.Prepend);

                // Next, rotate it again in the green plane, so it coincides with the blue axis.

                preHue.RotateGreen(-greenRotation, MatrixOrder.Append);

                // Hue rotations keep the color luminations constant, so that only the hues change
                // visible. To accomplish that, we shear the blue plane.

                float[] lum = new float[] { kLUMR, kLUMG, kLUMB, 1.0f };

                // Transform the luminance vector.

                preHue.TransformVector(lum);

                // Calculate the shear factors for red and green.

                float red = lum[0] / lum[2];

                float green = lum[1] / lum[2];

                // Shear the blue plane.

                preHue.ShearBlue(red, green, MatrixOrder.Append);

                // Prepare the postHue matrix. This holds the opposite transformations of the
                // preHue matrix. In fact, postHue is the inversion of preHue.

                postHue.ShearBlue(-red, -green, MatrixOrder.Prepend);

                postHue.RotateGreen(greenRotation, MatrixOrder.Append);

                postHue.RotateRed(-45.0f, MatrixOrder.Append);

            }
        }

        /// <summary>
        /// Shear the green plane
        /// </summary>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        /// <param name="order"></param>
        public void ShearBlue(float red, float green, MatrixOrder order)
        {
            this.ShearColor(2, 0, red, 1, green, order);
        }

        /// <summary>
        /// Set the saturation levels of ColorMatrix. 0..3.0f are useful values with 1.0f being identity
        /// </summary>
        /// <param name="saturation"></param>
        /// <param name="order"></param>
        public void SetSaturation(float saturation, MatrixOrder order)
        {
            // For the theory behind this, see the web sites at the top of this file.
            // In short: if saturation is 1.0f, m becomes the identity matrix, and this matrix is
            // unchanged. If saturation is 0.0f, each color is scaled by it's luminance weight.
            float satCompl = 1.0f - saturation;
            float satComplR = kLUMR * satCompl;
            float satComplG = kLUMG * satCompl;
            float satComplB = kLUMB * satCompl;

            XColorMatrix m = new XColorMatrix();

            m.cm[0, 0] = satComplR + saturation; m.cm[0, 1] = satComplR; m.cm[0, 2] = satComplR; m.cm[0, 3] = 0; m.cm[0, 4] = 0;

            m.cm[1, 0] = satComplG; m.cm[1, 1] = satComplG + saturation; m.cm[1, 2] = satComplG; m.cm[1, 3] = 0; m.cm[1, 4] = 0;

            m.cm[2, 0] = satComplB; m.cm[2, 1] = satComplB; m.cm[2, 2] = satComplB + saturation; m.cm[2, 3] = 0; m.cm[2, 4] = 0;

            m.cm[3, 0] = 0; m.cm[3, 1] = 0; m.cm[3, 2] = 0; m.cm[3, 3] = 1; m.cm[3, 4] = 0;

            m.cm[4, 0] = 0; m.cm[4, 1] = 0; m.cm[4, 2] = 0; m.cm[4, 3] = 0; m.cm[4, 4] = 1;

            Multiply(m, order);
        }

        /// <summary>
        /// Set ourselves to values that will converts an RGB(A) pixel format image to YUV(A) image.
        /// The alpha values should be unchanged.
        /// </summary>
        public void RGBtoYUV()
        {

            float[][] v = new float[][] {

											new float[] { 0.5f		, 0.3086f	, -0.1681f	, 0		, 0 },
											new float[] { -0.4407f	, 0.6094f	, -0.3391f	, 0		, 0 },
											new float[] { -0.0593f	, 0.082f	, 0.5f		, 0		, 0 },
											new float[] { 0			, 0			, 0			, 1		, 0 },
											new float[] { 0.5f		, 0			, 0.5f		, 0		, 1 }

									  };

            this.cm = new ColorMatrix(v);


        }

        /// <summary>
        /// Set ourselves to values that will converts an YUV(A) pixel format image to RGB(A) image.
        /// The alpha values should be unchanged.
        /// </summary>
        public void YUVtoRGB()
        {

            float[][] v = new float[][] {

											new float[] { 1.383f	, -0.7002f	, 0			, 0		, 0,},
											new float[] { 1			, 1			, 1			, 0		, 0,},
											new float[] { 0			, -0.247f	, 1.836f	, 0		, 0,},
											new float[] { 0			, 0			, 0			, 1		, 0,},
											new float[] { -0.6914f	, 0.4736f	, -0.918f	, 0		, 1 }

										};

            this.cm = new ColorMatrix(v);


        }

        #region ICloneable Members

        /// <summary>
        /// Create a clone of us
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return new XColorMatrix(this);
        }

        #endregion

    }
}
