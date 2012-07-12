namespace RedEye
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.modifiedImage = new System.Windows.Forms.PictureBox();
            this.sourceImage = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.modifiedImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sourceImage)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Before";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(190, 16);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(29, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "After";
            // 
            // label3
            // 
            this.label3.BackColor = System.Drawing.Color.White;
            this.label3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label3.Location = new System.Drawing.Point(13, 213);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(333, 179);
            this.label3.TabIndex = 4;
            this.label3.Text = resources.GetString("label3.Text");
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // modifiedImage
            // 
            this.modifiedImage.BackColor = System.Drawing.Color.White;
            this.modifiedImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.modifiedImage.Location = new System.Drawing.Point(190, 39);
            this.modifiedImage.Name = "modifiedImage";
            this.modifiedImage.Size = new System.Drawing.Size(156, 154);
            this.modifiedImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.modifiedImage.TabIndex = 2;
            this.modifiedImage.TabStop = false;
            // 
            // sourceImage
            // 
            this.sourceImage.BackColor = System.Drawing.Color.White;
            this.sourceImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.sourceImage.Image = global::RedEye.Properties.Resources.redeye;
            this.sourceImage.Location = new System.Drawing.Point(13, 39);
            this.sourceImage.Name = "sourceImage";
            this.sourceImage.Size = new System.Drawing.Size(156, 154);
            this.sourceImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.sourceImage.TabIndex = 0;
            this.sourceImage.TabStop = false;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(361, 407);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.modifiedImage);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.sourceImage);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.Text = "Red Eye Test Harness";
            ((System.ComponentModel.ISupportInitialize)(this.modifiedImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sourceImage)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox sourceImage;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.PictureBox modifiedImage;
        private System.Windows.Forms.Label label3;
    }
}

