namespace Safeguard
{
    partial class FormMain
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.labelStatus = new System.Windows.Forms.Label();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.processTnS = new System.Diagnostics.Process();
            this.fileSystemWatcherTnS = new System.IO.FileSystemWatcher();
            this.backgroundWorkerTnS = new System.ComponentModel.BackgroundWorker();
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcherTnS)).BeginInit();
            this.SuspendLayout();
            // 
            // labelStatus
            // 
            this.labelStatus.AutoSize = true;
            this.labelStatus.Location = new System.Drawing.Point(12, 9);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(156, 13);
            this.labelStatus.TabIndex = 0;
            this.labelStatus.Text = "Monotoring Timber and Stone...";
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.Location = new System.Drawing.Point(205, 29);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 1;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // processTnS
            // 
            this.processTnS.StartInfo.Domain = "";
            this.processTnS.StartInfo.LoadUserProfile = false;
            this.processTnS.StartInfo.Password = null;
            this.processTnS.StartInfo.StandardErrorEncoding = null;
            this.processTnS.StartInfo.StandardOutputEncoding = null;
            this.processTnS.StartInfo.UserName = "";
            this.processTnS.StartInfo.UseShellExecute = false;
            this.processTnS.SynchronizingObject = this;
            this.processTnS.Exited += new System.EventHandler(this.processTnS_Exited);
            // 
            // fileSystemWatcherTnS
            // 
            this.fileSystemWatcherTnS.Filter = "*.sav";
            this.fileSystemWatcherTnS.IncludeSubdirectories = true;
            this.fileSystemWatcherTnS.NotifyFilter = System.IO.NotifyFilters.LastWrite;
            this.fileSystemWatcherTnS.SynchronizingObject = this;
            this.fileSystemWatcherTnS.Changed += new System.IO.FileSystemEventHandler(this.fileSystemWatcherTnS_Changed);
            this.fileSystemWatcherTnS.Created += new System.IO.FileSystemEventHandler(this.fileSystemWatcherTnS_Created);
            // 
            // backgroundWorkerTnS
            // 
            this.backgroundWorkerTnS.WorkerSupportsCancellation = true;
            this.backgroundWorkerTnS.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorkerTnS_DoWork);
            this.backgroundWorkerTnS.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorkerTnS_RunWorkerCompleted);
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 64);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.labelStatus);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "FormMain";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Safeguard (Timber and Stone)";
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormMain_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FormMain_FormClosed);
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.Resize += new System.EventHandler(this.FormMain_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcherTnS)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Button buttonCancel;
        private System.Diagnostics.Process processTnS;
        private System.IO.FileSystemWatcher fileSystemWatcherTnS;
        private System.ComponentModel.BackgroundWorker backgroundWorkerTnS;
    }
}

