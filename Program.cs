using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace RedEye
{
    static class Program
    {
        /// <summary>
        /// Starts the Windows Forms demo, or writes the processed embedded
        /// sample image when invoked as: RedEye.exe --output <png-path>.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "--output")
            {
                using (Bitmap source = new Bitmap(Properties.Resources.redeye))
                using (Bitmap result = new RedEyeTool().ProcessImage(source))
                {
                    if (result == null)
                    {
                        throw new InvalidOperationException(
                            "Red-eye processing did not produce an image.");
                    }

                    result.Save(args[1], ImageFormat.Png);
                }
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
