using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace RedEye
{
	/// <summary>
	/// Removes red eye from the given image
	/// </summary>
	public class RedEyeTool
	{

		#region Red Eye Image Processing Algorithms

		/// <summary>
		/// Create a new image based on the given sub image with the candidate red eye removed
		/// </summary>
		/// <param name="b"></param>
		public Bitmap ProcessImage( Bitmap b ) 
		{

			// temporary images used to remove red eye

			Bitmap rednessMask=null, rednessThreshold=null, falseColor=null, noSmallIslands=null, noHoles=null, blured=null;

			// the image to return 

			Bitmap newImage = null;

			try 
			{

				// get a CIEMap of the image and the min/max CIE L values for the image

				double minL, maxL;

				CIELab[] cieMap = this.GetCIELABImage( b, out minL, out maxL );

				// get redness mask image 

				rednessMask = this.GetMaskImage( cieMap, b.Width, b.Height );

				// get a threshold of the redness mask

				rednessThreshold = this.Threshold( rednessMask ); 

				// create a blob map from the threasholded image

				BlobMap blobmap = new BlobMap( rednessThreshold );

				// create a false color copy of the thresholded images

				falseColor = (Bitmap)rednessThreshold.Clone();

				blobmap.ColorIslands( falseColor );

				// remove all islands meeting a certain size criteria

				noSmallIslands = (Bitmap)rednessThreshold.Clone();

				blobmap.RemoveIslands( noSmallIslands );

				// create a version with the holes filled

				noHoles = this.FillHoles( noSmallIslands );

				// get a guassian blurred version of the final mask

				float[] kernel = ImageUtility.GetGaussianBlurKernel( 3, 15 );

				blured = ImageUtility.Convolve( noHoles, kernel, 3 );

				// create new image

				newImage = this.GetNewImage(	cieMap,								// CIE Lab image of the source
												minL, maxL,							// min/max CIE L* values in the map
												blured );							// the final mask to be used

				// return new image

				return newImage;

			}
			catch ( Exception ) 
			{
			}
			finally 
			{
				// cleanup any temporaries used


				if ( rednessMask != null ) rednessMask.Dispose();
				if ( rednessThreshold != null ) rednessThreshold.Dispose();
				if ( falseColor != null ) falseColor.Dispose();
				if ( noSmallIslands != null ) noSmallIslands.Dispose();
				if ( noHoles != null ) noHoles.Dispose();
				if ( blured != null ) blured.Dispose();


			}

			// return new image

			return newImage;

		}

		/// <summary>
		/// Fill the holes in the binary image
		/// </summary>
		/// <param name="b"></param>
		protected Bitmap FillHoles( Bitmap b ) 
		{
			// clone and invert the bitmap ( which turns holes into islands ! )

			Bitmap nb = new Bitmap( b );

			// invert it

			BlobMap.InvertImage( nb );

			// create a temporary blob map which will capture the former holes as islands

			BlobMap bm = new BlobMap( nb );

			// fill holes 

			bm.FillHoles( nb );

			// invert image with holes filled again !

			BlobMap.InvertImage( nb );

			// return the image with the holes fill

			return nb;
			
		}

		/// <summary>
		/// Return a bitmap that is a thresholded version of the given. The ultimate threashold value is a matter of
		/// emperical testing. 175 is used.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		protected Bitmap Threshold( Bitmap s ) 
		{
			// first threshold to the magic number

			Bitmap b = new Bitmap( s );

			ImageUtility.ThresholdImage( b, 175.0f / 255.0f );

			float[] kernel = ImageUtility.GetGaussianBlurKernel( 3, 15 );

			Bitmap b1 = ImageUtility.Convolve( b, kernel, 3 );

			b.Dispose();

			ImageUtility.ThresholdImage( b1, 1.0f / 255.0f );

			// return new image

			return b1;

		}

		/// <summary>
		/// There is the 'typical' red eye color recorded in CIE Lab color space;
		/// </summary>
		const double kL_REDEYE = 42;
		const double kA_REDEYE = 62;
		const double kB_REDEYE = 30;

		/// <summary>
		/// Return and a CIE Lab color mapping of the source image. Alpha is ignored. Also returns the
		/// min/max CIE L value found in the image.
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public CIELab[] GetCIELABImage( Bitmap source, out double minL, out double maxL ) 
		{
			// init L limits

			minL = double.MaxValue;

			maxL = double.MinValue;

			// create map

			CIELab[] map = new CIELab[ source.Width * source.Height ];

			// iterate pixels

			for( int y = 0 ; y < source.Height ; y++ ) 
			{
				for( int x = 0 ; x < source.Width ; x++ ) 
				{
					// get CIE XYZ mapping for pixel and then convert this to CIE Lab

					double CIE_X, CIE_Y, CIE_Z;

					RgbaHls.RGB_2_CIEXYZ( source.GetPixel( x,y ), out CIE_X, out CIE_Y, out CIE_Z );

					double CIE_L, CIE_a, CIE_b;

					RgbaHls.CIEXYZ_2_CIELAB( CIE_X, CIE_Y, CIE_Z, out CIE_L, out CIE_a, out CIE_b );

					// insert into map

					map[ y * source.Width + x ] = new CIELab( CIE_L, CIE_a, CIE_b );

					// update L limits

					minL = Math.Min( minL, CIE_L );

					maxL = Math.Max( maxL, CIE_L );

				}
			}

			// return mapping

			return map;
		}

		/// <summary>
		/// Return a Bitmap created from the given CIELab color values
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public Bitmap GetBitmapFromCIELABImage( CIELab[] s, int width, int height ) 
		{

			// create map

			Bitmap b = new Bitmap( width, height, PixelFormat.Format32bppPArgb );

			// iterate pixels

			for( int y = 0 ; y < height ; y++ ) 
			{
				for( int x = 0 ; x < width ; x++ ) 
				{
					// get CIE XYZ mapping for pixel and then convert this to CIE Lab

					double CIE_X, CIE_Y, CIE_Z;

					CIELab cie = s[ y * width + x ];

					RgbaHls.CIELAB_2_CIEXYZ( cie.L, cie.a, cie.b, out CIE_X, out CIE_Y, out CIE_Z );

					// convert back to RGB

					Color c = RgbaHls.CIEXYZ_2_RGB( CIE_X, CIE_Y, CIE_Z );

					// insert into bitmap

					b.SetPixel( x, y, c );

				}
			}

			// return bitmap

			return b;
		}

		/// <summary>
		/// Given a CIELab array ( obtained with GetCIELABImage ) this calculate the chromaticity distance of
		/// each pixel fro the 'typical' red eye CIELAB color. Also returns the Min/Max chromaticity distances found
		/// </summary>
		/// <returns></returns>
		public double[] GetChromaticityDistanceMap( CIELab[] map, int width, int height, out double minDistance, out double maxDistance ) 
		{
			// initialize min/max

			minDistance = double.MaxValue;

			maxDistance = double.MinValue;

			// create return array

			double[] cdMap = new double[ width * height ];

			// iterate pixels

			for( int y = 0 ; y < height ; y++ ) 
			{
				for( int x = 0 ; x < width ; x++ ) 
				{
					// get distance for this pixel

					double c = RgbaHls.ChromaticityDistance( map[ y * width + x ].a, kA_REDEYE, map[ y * width + x ].b, kB_REDEYE );

					// assign to map

					cdMap[ y * width + x ] = c;

					// update min/max

					minDistance = Math.Min( minDistance, c );

					maxDistance = Math.Max( maxDistance, c );

				}
			}

			// return chromaticty map

			return cdMap;
		}

		/// <summary>
		/// Returns a bitmap of gray levels where the 'red-ness' of the original pixel determines the
		/// black/white value. Black pixels are farthest from red and white pixels are closest.
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public Bitmap GetMaskImage( CIELab[] cieMap, int width, int height ) 
		{

			// get chromaticty distance map and min/max chromaticty distances

			double minCD, maxCD;

			double[] cdMap = GetChromaticityDistanceMap( cieMap, width, height, out minCD, out maxCD ); 

			// create a black image of the same size

			Bitmap mask = ImageUtility.CreateImage( width, height, PixelFormat.Format32bppPArgb, Color.FromArgb(255,0,0,0) );

			// process each pixel

			for( int y = 0 ; y < height ; y++ ) 
			{
				for( int x = 0 ; x < width ; x++ ) 
				{

					// Mask value =  
					//
					//							    maxCD - chromaticty distance [x,y]
					//				Round( 255 *  ------------------------------------- )
					//								          maxCD - minCD
					//

					double temp = ( maxCD - cdMap[ y * width + x ] ) / ( maxCD - minCD );

					int maskValue = (int)Math.Min( 255.0, Math.Max( 0.0, Math.Round( 255.0 * temp ) ) );

					// set RGBA values

					mask.SetPixel( x, y, Color.FromArgb( 255, maskValue, maskValue, maskValue ) );
  
				}
			}

			// return mask image

			return mask;
		}

		/// <summary>
		/// Create a new ( red eye removed ) image.
		/// </summary>
		/// <returns></returns>
		public Bitmap GetNewImage( CIELab[] cieMap, double minL, double maxL, Bitmap mask ) 
		{

			// create a set of target color which are the original CIE Lab colors set to gray scales by setting a* and b* to zero and stretching the L* to
			// the dynamic range of the image. This formula is used

			//
			//	L  =                 maxL
			//			 ----------------------------  * ( L - minL )
			//			         maxL - minL
			//
			//  					a = 0
			//					    b = 0
			//

			CIELab[] t = new CIELab[ cieMap.Length ];

			double r = maxL / ( maxL - minL );

			for( int y = 0 ; y < mask.Height ; y++ ) 
			{
				for( int x = 0 ; x < mask.Width ; x++ ) 
				{
				   t[ y * mask.Width + x ] = new CIELab( r * ( cieMap[ y * mask.Width + x ].L - minL ) , 0, 0 );
				}
			}

			// now calculate a new CIE Lab color for pixel using:
			//
			// pixel = t[i,j] * mask[i,j] + ( cieMap[i,j] * ( 1 - mask[i,j] ) ) 
			//

			for( int j = 0 ; j < mask.Height ; j++ ) 
			{
				for( int i = 0 ; i < mask.Width ; i++ ) 
				{

					// get normalized mask value for this pixel	( any of the RGB components will do, they should all be the same )

					double m = (double)(mask.GetPixel( i, j ).R) / 255.0;

					// get original CIE Lab from ciemap

					CIELab original = cieMap[ j * mask.Width + i ];

					CIELab target = t[ j * mask.Width + i ];

					// calculate new L* a* b* values using formula above

					double L = target.L * m + original.L * ( 1 - m );

					double a = target.a * m + original.a * ( 1 - m );

					double b = target.b * m + original.b * ( 1 - m );
 
					// reuse target array to hold new value for pixel ( in CIE Lab space )

					t[ j * mask.Width + i ] = new CIELab( L, a, b );

				}
			}



			// return new image

			return GetBitmapFromCIELABImage( t, mask.Width, mask.Height );
		}

		#endregion

	}

	/// <summary>
	/// Simple CIE Lab color structure
	/// </summary>
	public class CIELab 
	{
		public double L;
		public double a;
		public double b;

		public CIELab( double CIE_L, double CIE_a, double CIE_b ) 
		{
			this.L = CIE_L;

			this.a = CIE_a;

			this.b = CIE_b;
		}
	}
}
