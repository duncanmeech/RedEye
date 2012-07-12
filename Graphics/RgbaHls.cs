using System;
using System.Drawing;

namespace RedEye
{


	//
	// A class that represents a color. It is composed to Red,Green,Blue and alpha components. The Hue, Saturation,Luminance
	// components are always synchronized to the RGB value and visa versa. Alpha values are indepedant of the color
	// but are required for setting a color. 
	// Class also provides public static members for RGB/HSL conversions.
	//
	// R,G,B,A are represented as doubles in the range 0.0 ---> 1.0f and are clamped to this range
	// Hue is a double in the range 0.0---->360.0 ( degrees on a color wheel )
	// S and L are doubles in the range 0.0f ---> 1.0f and are clamped to this range.
	//
	public class RgbaHls : System.Object 
	{

		#region Properties

		// member Red, Green, Blue, Alpha, Hue, Saturation, Luminance

		private double r;			// RED Valid range: 0.0 ---> 1.0
		public double R 
		{
			get 
			{
				return r;
			}
			set 
			{
				FromRGBA( value,g,b,a );
			}
		}
		private double g;			// GREEN Valid range: 0.0 ---> 1.0
		public double G
		{
			get 
			{
				return g;
			}
			set 
			{
				FromRGBA( r,value,b,a );
			}
		}
		private double b;			// BLUE Valid range: 0.0 ---> 1.0
		public double B
		{
			get 
			{
				return b;
			}
			set 
			{
				FromRGBA( r,g,value,a );
			}
		}
		private double a;			// ALPHA Valid range: 0.0 ---> 1.0
		public double A
		{
			get 
			{
				return a;
			}
			set 
			{
				FromRGBA(r,g,b,value );
			}
		}
		private double h;			// HUE Valid range: 0.0 ---> 360.0
		public double H
		{
			get 
			{
				return h;
			}
			set 
			{
				FromHSLA( value,s,l,a );
			}
		}
		private double s;			// SATURATION Valid range: 0.0 ---> 1.0
		public double S 
		{
			get 
			{
				return s;
			}
			set 
			{
				FromHSLA( h,value,l,a );
			}
		}
		private double l;			// LUMINANCE Valid range: 0.0f ---> 1.0f
		public double L 
		{
			get 
			{
				return l;
			}
			set 
			{
				FromHSLA(h,s,value,a );
			}
		}

		#endregion

		#region constructors

		// default constructor

		public RgbaHls() 
		{
			r = g = b = a = h = s = l = 0.0;
		}

		public RgbaHls( Color c ) 
		{
			FromSystemRGBA( c.R, c.G, c.B, c.A );
		}

		public RgbaHls( RgbaHls a ) 
		{
			this.FromHSLA( a.H, a.S, a.L, a.A );
		}

		public RgbaHls( double h, double s, double l, double a ) 
		{
			this.FromHSLA( h,s,l,a );
		}

		#endregion

		#region methods
		
		//
		// Set color from RGBA values
		//
		public void FromRGBA( double rv,double gv, double bv, double av ) 
		{
			// clamp all values to correct range

			r = Math.Min( 1.0f, Math.Max( 0.0, rv ) );
			g = Math.Min( 1.0f, Math.Max( 0.0, gv ) );
			b = Math.Min( 1.0f, Math.Max( 0.0, bv ) );
			a = Math.Min( 1.0f, Math.Max( 0.0, av ) );

			// calculate new HSL values

			RgbaHls.RGB2HSL( r,g,b, ref h, ref s, ref l );

		}

		//
		// Set color from HSL and alpha values
		//
		public void FromHSLA( double hv,double sv, double lv, double av ) 
		{
			// clamp all values to correct range

			h = Math.Min( 360.0f, Math.Max( 0.0, hv ) );
			s = Math.Min( 1.0f, Math.Max( 0.0, sv ) );
			l = Math.Min( 1.0f, Math.Max( 0.0, lv ) );
			a = Math.Min( 1.0f, Math.Max( 0.0, av ) );

			// calculate new RGB values

			RgbaHls.HSL2RGB( ref r, ref g, ref b, h, s, l );

		}
		//
		// Set color from system RGBA values ( i.e. integers in the range 0..255 )
		//
		public void FromSystemRGBA( int rv, int gv, int bv, int av ) 
		{
			FromRGBA( rv / 255.0, gv / 255.0, bv / 255.0, av / 255.0f );
		}

		//
		// Set color from system RGBA values ( i.e. integers in the range 0..255 )
		//
		public void FromSystemColor( Color c ) 
		{
			FromRGBA( c.R / 255.0, c.G / 255.0, c.B / 255.0, c.A / 255.0f );
		}
		
		//
		// Set color from system HSBA values ( same as ours but uses floats and A is an integer )
		//
		public void FromSystemHSBA( float hv, float sv, float bv, int av ) 
		{
			FromHSLA( (double)hv, (double)sv, (double)bv, (double)av / 255.0 );
		}

		//
		// Return a system color based on this color
		//
		public System.Drawing.Color GetSystemColor() 
		{
			// convert to Window's style ints

			int ri,gi,bi,ai;

			ri = (int)Math.Min( 255,Math.Round( r * 255.0 ) );

			gi = (int)Math.Min( 255,Math.Round( g * 255.0 ) );

			bi = (int)Math.Min( 255,Math.Round( b * 255.0 ) );

			ai = (int)Math.Min( 255,Math.Round( a * 255.0 ) );

			return System.Drawing.Color.FromArgb( ai,ri,gi,bi );

		}

		/// <summary>
		/// shift the hue 180 degrees
		/// </summary>
		public void OppositeHue() 
		{
			this.H = this.h >= 180.0f ? this.h - 180.0f : this.h + 180.0f;
		}

		//
		// ToString returns a string representation of ourselves
		//
		public override string ToString()
		{
			// convert to Window's style ints

			int ri,gi,bi,ai;

			ri = (int)(double)(( r * 255.0 ));

			gi = (int)(double)(( g * 255.0 ));

			bi = (int)(double)(( b * 255.0 ));

			ai = (int)(double)(( a * 255.0 ));



			return string.Format( "R:{0} G:{1} B:{2} A:{3}",ri,gi,bi,ai ); 


		}


		#endregion

		#region Static members

		/// <summary>
		/// Return a new system color from the given hsla values
		/// </summary>
		/// <param name="h"></param>
		/// <param name="s"></param>
		/// <param name="l"></param>
		/// <param name="alpha"></param>
		/// <returns></returns>
		public static Color ColorFromHSLA( double h, double s, double l, double a ) 
		{
			RgbaHls c = new RgbaHls();
			
			c.FromHSLA( h,s,l,a );

			return c.GetSystemColor();
		}

		//
		// RGB to HSL conversion ( From Graphic Gems I p.448 )
		//
		public static bool RGB2HSL( double rIN,double gIN,double bIN, ref double hOUT, ref double sOUT, ref double lOUT ) 
		{
			// parameter check

			if ( rIN < 0.0f  || rIN > 1.0f || gIN < 0.0f  || gIN > 1.0f || bIN < 0.0f  || bIN > 1.0f  ) 
			{
				// force HSL to something useful but meaningless

				hOUT = sOUT = lOUT = 0.0;

				return false;
			}

			// parameters are valid so perform conversion


			double v;
			double m;
			double vm;
			double r2, g2, b2;

			v = Math.Max(rIN,gIN);
			v = Math.Max(v,bIN);
			m = Math.Min(rIN,gIN);
			m = Math.Min(m,bIN);

			if ((lOUT = (m + v) / 2.0) <= 0.0) 
				return false;
			if ((sOUT = vm = v - m) > 0.0) 
			{
				sOUT /= (lOUT <= 0.5) ? (v + m ) :
					(2.0 - v - m) ;
			} 
			else
				return false;


			r2 = (v - rIN) / vm;
			g2 = (v - gIN) / vm;
			b2 = (v - bIN) / vm;

			if (rIN == v)
				hOUT = (gIN == m ? 5.0 + b2 : 1.0 - g2);
			else 
				if (gIN == v)
				hOUT = (bIN == m ? 1.0 + r2 : 3.0 - b2);
			else
				hOUT = (rIN == m ? 3.0 + g2 : 5.0 - r2);

			hOUT /= 6;

			// convert to 0..360.0f

			hOUT = hOUT * 360.0;

			return true;

		}

		//
		// RGB to HSL conversion ( from Graphic Gems I p.448 )
		//
		public static bool HSL2RGB( ref double rOUT,ref double gOUT,ref double bOUT, double hINa, double sIN, double lIN ) 
		{
			// parameter check

			if ( hINa < 0.0  || hINa > 360.0 || sIN < 0.0  || sIN > 1.0 || lIN < 0.0  || lIN > 1.0  ) 
			{
				// force HSL to something useful but meaningless

				rOUT = gOUT = bOUT = 0.0f;

				return false;
			}

			// orignal source algorthim expected HUE in the range 0.0f ---> 1.0f so convert now

			double hIN = hINa / 360.0;

			double v;

			v = (lIN <= 0.5) ? (lIN * (1.0 + sIN)) : (lIN + sIN - lIN * sIN);
			if (v <= 0.0) 
			{
				rOUT = gOUT = bOUT = 0.0;

				return false;
			} 
			else 
			{
				double m;
				double sv;
				int sextant;
				double fract, vsf, mid1, mid2;

				m = lIN + lIN - v;
				sv = (v - m ) / v;
				hIN *= 6.0;
				sextant = (int)hIN;	
				fract = hIN - sextant;
				vsf = v * sv * fract;
				mid1 = m + vsf;
				mid2 = v - vsf;
				switch (sextant) 
				{
					case 0: rOUT = v	; gOUT = mid1	; bOUT = m		; break;
					case 1: rOUT = mid2	; gOUT = v		; bOUT = m		; break;
					case 2: rOUT = m	; gOUT = v		; bOUT = mid1	; break;
					case 3: rOUT = m	; gOUT = mid2	; bOUT = v		; break;
					case 4: rOUT = mid1	; gOUT = m		; bOUT = v		; break;
					case 5: rOUT = v	; gOUT = m		; bOUT = mid2	; break;

					// case 6 is a bug fix, for cases where 1.0 exactly was passed as Hue
					case 6: rOUT = v    ; gOUT = mid1   ; bOUT = m		; break;
				}
			}

			return true;

		}

		/// <summary>
		/// Convert an RGB value ( range 0..255 ) to CIEXYZ values
		/// </summary>
		public static void RGB_2_CIEXYZ( Color c, out double X, out double Y, out double Z ) 
		{
			double var_R = ( (double)c.R / 255.0 );        
			double var_G = ( (double)c.G / 255.0 );        
			double var_B = ( (double)c.B / 255.0 );        

			if ( var_R > 0.04045 ) 
				var_R = Math.Pow( ( ( var_R + 0.055 ) / 1.055 ) , 2.4 );
			else                   
				var_R = var_R / 12.92;

			if ( var_G > 0.04045 ) 
				var_G = Math.Pow( ( ( var_G + 0.055 ) / 1.055 ), 2.4 );
			else                   
				var_G = var_G / 12.92;

			if ( var_B > 0.04045 ) 
				var_B = Math.Pow( ( ( var_B + 0.055 ) / 1.055 ) , 2.4 );
			else                   
				var_B = var_B / 12.92;

			var_R = var_R * 100.0;
			var_G = var_G * 100.0;
			var_B = var_B * 100.0;

			//Observer. = 2°, Illuminant = D65

			X = var_R * 0.4124 + var_G * 0.3576 + var_B * 0.1805;
			Y = var_R * 0.2126 + var_G * 0.7152 + var_B * 0.0722;
			Z = var_R * 0.0193 + var_G * 0.1192 + var_B * 0.9505;

		}

		/// <summary>
		/// Convert a CIE XYZ triplet to a system color. The alpha of the color is 255
		/// </summary>
		/// <param name="X"></param>
		/// <param name="Y"></param>
		/// <param name="Z"></param>
		/// <param name="c"></param>
		public static Color CIEXYZ_2_RGB( double X, double Y, double Z  ) 
		{

			double var_X = X / 100;        //X = From 0 to ref_X
			double var_Y = Y / 100;        //Y = From 0 to ref_Y
			double var_Z = Z / 100;        //Z = From 0 to ref_Y

			double var_R = var_X *  3.2406 + var_Y * -1.5372 + var_Z * -0.4986;
			double var_G = var_X * -0.9689 + var_Y *  1.8758 + var_Z *  0.0415;
			double var_B = var_X *  0.0557 + var_Y * -0.2040 + var_Z *  1.0570;

			if ( var_R > 0.0031308 )
				var_R = 1.055 * ( Math.Pow( var_R , ( 1 / 2.4 ) ) ) - 0.055;
			else                     
				var_R = 12.92 * var_R;
			if ( var_G > 0.0031308 )
				var_G = 1.055 * ( Math.Pow( var_G , ( 1 / 2.4 ) ) ) - 0.055;
			else 
				var_G = 12.92 * var_G;
			if ( var_B > 0.0031308 ) 
				var_B = 1.055 * ( Math.Pow( var_B , ( 1 / 2.4 ) ) ) - 0.055;
			else                     
				var_B = 12.92 * var_B;

	
			return Color.FromArgb(	255,
									(byte)Math.Min( 255, Math.Max( 0, var_R * 255.0 ) ),
									(byte)Math.Min( 255, Math.Max( 0, var_G * 255.0 ) ),
									(byte)Math.Min( 255, Math.Max( 0, var_B * 255.0 ) ) );


		}

		/// <summary>
		/// Converts a CIEXYZ triple to a CIELAB triple ( Lab )
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <param name="L"></param>
		/// <param name="a"></param>
		/// <param name="b"></param>
		public static void CIEXYZ_2_CIELAB( double X, double Y, double Z, out double L, out double a, out double b ) 
		{
			double var_X = X /  95.047;
			double var_Y = Y / 100.000;
			double var_Z = Z / 108.883;

			if ( var_X > 0.008856 ) 
				var_X = Math.Pow( var_X ,( 1.0/3.0 ) );
			else                    
				var_X = ( 7.787 * var_X ) + ( 16.0 / 116.0 );

			if ( var_Y > 0.008856 ) 
				var_Y = Math.Pow( var_Y , ( 1.0/3.0 ) );
			else                    
				var_Y = ( 7.787 * var_Y ) + ( 16.0 / 116.0 );

			if ( var_Z > 0.008856 ) 
				var_Z = Math.Pow( var_Z , ( 1.0/3.0 ) );
			else                    
				var_Z = ( 7.787 * var_Z ) + ( 16.0 / 116.0 );

			L = ( 116 * var_Y ) - 16;

			a = 500 * ( var_X - var_Y );

			b = 200 * ( var_Y - var_Z );
 
		}

		/// <summary>
		/// Converts a CIE L* a* b* triple to a CIE XYZ triple
		/// </summary>
		/// <param name="CIE_L"></param>
		/// <param name="CIE_a"></param>
		/// <param name="CIE_b"></param>
		/// <param name="X"></param>
		/// <param name="Y"></param>
		/// <param name="Z"></param>
		public static void CIELAB_2_CIEXYZ( double CIE_L, double CIE_a, double CIE_b, out double X, out double Y, out double Z ) 
		{
			const double ref_X = 95.047;		// CIE illuminant D65 white point

			const double ref_Y = 100.000;

			const double ref_Z = 108.883;

			double var_Y = ( CIE_L + 16.0 ) / 116.0;

			double var_X = CIE_a / 500 + var_Y;

			double var_Z = var_Y - CIE_b / 200.0;

			if ( Math.Pow( var_Y,3.0) > 0.008856 )
				var_Y = Math.Pow( var_Y, 3 );
			else                     
				var_Y = ( var_Y - 16.0 / 116.0 ) / 7.787;

			if ( Math.Pow( var_X, 3.0 ) > 0.008856 ) 
				var_X = Math.Pow( var_X, 3.0);
			else                      
				var_X = ( var_X - 16 / 116 ) / 7.787;

			if ( Math.Pow( var_Z, 3.0 ) > 0.008856 ) 
				var_Z = Math.Pow( var_Z, 3 );
			else                      
				var_Z = ( var_Z - 16 / 116 ) / 7.787;

			X = ref_X * var_X;  
			Y = ref_Y * var_Y;  
			Z = ref_Z * var_Z;  

		}

		/// <summary>
		/// Given the a,b components of two CIE Lab colors compute and return the chromaticty distance between them.
		/// </summary>
		/// <param name="a1"></param>
		/// <param name="a2"></param>
		/// <param name="b1"></param>
		/// <param name="b2"></param>
		/// <returns></returns>
		public static double ChromaticityDistance( double a1, double a2, double b1, double b2 ) 
		{
			return Math.Sqrt( ( a2 - a1 ) * ( a2 - a1 ) + ( b2 - b1 ) * ( b2 - b1 ) );
		}


		#endregion

	}
}
