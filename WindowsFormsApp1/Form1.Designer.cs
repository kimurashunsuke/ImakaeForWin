namespace WindowsFormsApp1
{
    partial class Form1
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.btnGetThreads = new System.Windows.Forms.Button();
            this.btnGetRes = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // btnGetThreads
            // 
            this.btnGetThreads.Location = new System.Drawing.Point(87, 64);
            this.btnGetThreads.Name = "btnGetThreads";
            this.btnGetThreads.Size = new System.Drawing.Size(75, 23);
            this.btnGetThreads.TabIndex = 0;
            this.btnGetThreads.Text = "Click this";
            this.btnGetThreads.UseVisualStyleBackColor = true;
            this.btnGetThreads.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnGetRes
            // 
            this.btnGetRes.Location = new System.Drawing.Point(169, 63);
            this.btnGetRes.Name = "btnGetRes";
            this.btnGetRes.Size = new System.Drawing.Size(75, 23);
            this.btnGetRes.TabIndex = 3;
            this.btnGetRes.Text = "GetRes";
            this.btnGetRes.UseVisualStyleBackColor = true;
            this.btnGetRes.Click += new System.EventHandler(this.btnGetRes_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToResizeColumns = false;
            this.dataGridView1.AllowUserToResizeRows = false;
            this.dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(87, 106);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.RowTemplate.Height = 21;
            this.dataGridView1.Size = new System.Drawing.Size(814, 471);
            this.dataGridView1.TabIndex = 4;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(913, 627);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.btnGetRes);
            this.Controls.Add(this.btnGetThreads);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnGetThreads;
        private System.Windows.Forms.Button btnGetRes;
        private System.Windows.Forms.DataGridView dataGridView1;
    }
}

