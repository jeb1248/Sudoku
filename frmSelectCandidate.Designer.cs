namespace YAS
{
    partial class FrmSelectCandidate
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
            this.lv1 = new System.Windows.Forms.ListView();
            this.lv2 = new System.Windows.Forms.ListView();
            this.SuspendLayout();
            // 
            // lv1
            // 
            this.lv1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lv1.Font = new System.Drawing.Font("Comic Sans MS", 16.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lv1.HideSelection = false;
            this.lv1.Location = new System.Drawing.Point(0, 0);
            this.lv1.Name = "lv1";
            this.lv1.Scrollable = false;
            this.lv1.Size = new System.Drawing.Size(390, 348);
            this.lv1.TabIndex = 0;
            this.lv1.UseCompatibleStateImageBehavior = false;
            this.lv1.Click += new System.EventHandler(this.Lv1_Click);
            this.lv1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Lv1_KeyPress);
            // 
            // lv2
            // 
            this.lv2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lv2.Font = new System.Drawing.Font("Comic Sans MS", 16.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lv2.HideSelection = false;
            this.lv2.Location = new System.Drawing.Point(0, 345);
            this.lv2.Name = "lv2";
            this.lv2.Scrollable = false;
            this.lv2.Size = new System.Drawing.Size(390, 348);
            this.lv2.TabIndex = 4;
            this.lv2.UseCompatibleStateImageBehavior = false;
            this.lv2.Click += new System.EventHandler(this.Lv2_Click);
            this.lv2.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Lv2_KeyPress);
            // 
            // FrmSelectCandidate
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(16F, 31F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(991, 969);
            this.Controls.Add(this.lv2);
            this.Controls.Add(this.lv1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Location = new System.Drawing.Point(700, 700);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "FrmSelectCandidate";
            this.Text = "FrmSelectCandidate";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView lv1;
        private System.Windows.Forms.ListView lv2;
    }
}