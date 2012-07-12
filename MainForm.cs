using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using RedEye.Properties;

namespace RedEye
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Process the source image and display on load
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            // get source image 

            Bitmap b = RedEye.Properties.Resources.redeye;

            // get new image with red eye removed

            RedEyeTool ret = new RedEyeTool();

            b = ret.ProcessImage(b);

            // display

            this.modifiedImage.Image = b;
        }
    }
}
