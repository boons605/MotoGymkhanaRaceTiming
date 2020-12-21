namespace MGRDTesting
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripComboBox1 = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.statusLbx = new System.Windows.Forms.ListBox();
            this.macTbx = new System.Windows.Forms.TextBox();
            this.AllowedDevicesLbx = new System.Windows.Forms.ListBox();
            this.UpdateAllowedDevicesBtn = new System.Windows.Forms.Button();
            this.updateDetectedDevicesBtn = new System.Windows.Forms.Button();
            this.detectedDevicesLbx = new System.Windows.Forms.ListBox();
            this.closestDeviceLbl = new System.Windows.Forms.Label();
            this.updateClosestDeviceBtn = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1029, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripComboBox1,
            this.toolStripButton1});
            this.toolStrip1.Location = new System.Drawing.Point(0, 24);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1029, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripComboBox1
            // 
            this.toolStripComboBox1.Name = "toolStripComboBox1";
            this.toolStripComboBox1.Size = new System.Drawing.Size(121, 25);
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton1.Text = "toolStripButton1";
            this.toolStripButton1.Click += new System.EventHandler(this.toolStripButton1_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(170, 65);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(71, 30);
            this.button1.TabIndex = 2;
            this.button1.Text = "Add";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(251, 65);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(71, 30);
            this.button2.TabIndex = 3;
            this.button2.Text = "Remove";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // statusLbx
            // 
            this.statusLbx.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.statusLbx.FormattingEnabled = true;
            this.statusLbx.Location = new System.Drawing.Point(12, 562);
            this.statusLbx.Name = "statusLbx";
            this.statusLbx.Size = new System.Drawing.Size(1007, 95);
            this.statusLbx.TabIndex = 4;
            // 
            // macTbx
            // 
            this.macTbx.Location = new System.Drawing.Point(13, 67);
            this.macTbx.Name = "macTbx";
            this.macTbx.Size = new System.Drawing.Size(147, 20);
            this.macTbx.TabIndex = 5;
            // 
            // AllowedDevicesLbx
            // 
            this.AllowedDevicesLbx.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.AllowedDevicesLbx.FormattingEnabled = true;
            this.AllowedDevicesLbx.Location = new System.Drawing.Point(388, 65);
            this.AllowedDevicesLbx.Name = "AllowedDevicesLbx";
            this.AllowedDevicesLbx.Size = new System.Drawing.Size(296, 446);
            this.AllowedDevicesLbx.TabIndex = 6;
            // 
            // UpdateAllowedDevicesBtn
            // 
            this.UpdateAllowedDevicesBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.UpdateAllowedDevicesBtn.Location = new System.Drawing.Point(383, 527);
            this.UpdateAllowedDevicesBtn.Name = "UpdateAllowedDevicesBtn";
            this.UpdateAllowedDevicesBtn.Size = new System.Drawing.Size(301, 30);
            this.UpdateAllowedDevicesBtn.TabIndex = 7;
            this.UpdateAllowedDevicesBtn.Text = "Update";
            this.UpdateAllowedDevicesBtn.UseVisualStyleBackColor = true;
            this.UpdateAllowedDevicesBtn.Click += new System.EventHandler(this.UpdateAllowedDevicesBtn_Click);
            // 
            // updateDetectedDevicesBtn
            // 
            this.updateDetectedDevicesBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.updateDetectedDevicesBtn.Location = new System.Drawing.Point(703, 528);
            this.updateDetectedDevicesBtn.Name = "updateDetectedDevicesBtn";
            this.updateDetectedDevicesBtn.Size = new System.Drawing.Size(314, 30);
            this.updateDetectedDevicesBtn.TabIndex = 9;
            this.updateDetectedDevicesBtn.Text = "Update";
            this.updateDetectedDevicesBtn.UseVisualStyleBackColor = true;
            this.updateDetectedDevicesBtn.Click += new System.EventHandler(this.updateDetectedDevicesBtn_Click);
            // 
            // detectedDevicesLbx
            // 
            this.detectedDevicesLbx.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.detectedDevicesLbx.FormattingEnabled = true;
            this.detectedDevicesLbx.Location = new System.Drawing.Point(710, 65);
            this.detectedDevicesLbx.Name = "detectedDevicesLbx";
            this.detectedDevicesLbx.Size = new System.Drawing.Size(309, 446);
            this.detectedDevicesLbx.TabIndex = 8;
            // 
            // closestDeviceLbl
            // 
            this.closestDeviceLbl.AutoSize = true;
            this.closestDeviceLbl.Location = new System.Drawing.Point(13, 200);
            this.closestDeviceLbl.Name = "closestDeviceLbl";
            this.closestDeviceLbl.Size = new System.Drawing.Size(95, 13);
            this.closestDeviceLbl.TabIndex = 10;
            this.closestDeviceLbl.Text = "Not detected yet...";
            this.closestDeviceLbl.Click += new System.EventHandler(this.closestDeviceLbl_Click);
            // 
            // updateClosestDeviceBtn
            // 
            this.updateClosestDeviceBtn.Location = new System.Drawing.Point(13, 216);
            this.updateClosestDeviceBtn.Name = "updateClosestDeviceBtn";
            this.updateClosestDeviceBtn.Size = new System.Drawing.Size(71, 30);
            this.updateClosestDeviceBtn.TabIndex = 11;
            this.updateClosestDeviceBtn.Text = "Update";
            this.updateClosestDeviceBtn.UseVisualStyleBackColor = true;
            this.updateClosestDeviceBtn.Click += new System.EventHandler(this.updateClosestDeviceBtn_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(170, 134);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(71, 30);
            this.button3.TabIndex = 12;
            this.button3.Text = "AddFile";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1029, 676);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.updateClosestDeviceBtn);
            this.Controls.Add(this.closestDeviceLbl);
            this.Controls.Add(this.updateDetectedDevicesBtn);
            this.Controls.Add(this.detectedDevicesLbx);
            this.Controls.Add(this.UpdateAllowedDevicesBtn);
            this.Controls.Add(this.AllowedDevicesLbx);
            this.Controls.Add(this.macTbx);
            this.Controls.Add(this.statusLbx);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripComboBox toolStripComboBox1;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.ListBox statusLbx;
        private System.Windows.Forms.TextBox macTbx;
        private System.Windows.Forms.ListBox AllowedDevicesLbx;
        private System.Windows.Forms.Button UpdateAllowedDevicesBtn;
        private System.Windows.Forms.Button updateDetectedDevicesBtn;
        private System.Windows.Forms.ListBox detectedDevicesLbx;
        private System.Windows.Forms.Label closestDeviceLbl;
        private System.Windows.Forms.Button updateClosestDeviceBtn;
        private System.Windows.Forms.Button button3;
    }
}

