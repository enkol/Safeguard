using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Security.AccessControl;
using System.Diagnostics;

namespace Safeguard
{
    public partial class FormMain : Form
    {
        private enum CleanupState
        {
            Init,
            None,
            Initiated,
            Running,
            Finished
        }

        private enum BackupState
        {
            Idle,
            Running
        }

        [Flags()]
        private enum JobType
        {
            Backup = 0x1,
            Cleanup = 0x2
        }

        private class Work
        {
            public JobType Job;
            public string Target;

            public Work(JobType job, string target)
            {
                Job = job;
                Target = target;
            }
        }

        public class DescendingComparer : IComparer<String>
        {
            public int Compare(string x, string y)
            {
                return string.Compare(x, y) * -1;
            }
        }

        private const string _appName = "Timber and Stone.exe";
        private CleanupState _cleanupState = CleanupState.Init;
        private BackupState _backupState = BackupState.Idle;
        private ContextMenu _trayMenu = null;
        private NotifyIcon _trayIcon = null;
        private string _appPath;
        private string _savesPath;
        private string _baseFolder;
        private string _backupFolder;
        private string _saveFolder;
        private int _backupDelayInSeconds;
        private int _maxNumberOfBackups;
        private int _minTimeDiffInSeconds;
        private IDictionary<string, string> _lastBackups = null;
        private System.Windows.Forms.Timer _backupTimer = null;
        private string _backupSavegame = null;

        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            // Load settings
            _backupDelayInSeconds = Properties.Settings.Default.BackupDelayInSeconds;
            _maxNumberOfBackups = Properties.Settings.Default.MaxNumberOfBackups;
            _minTimeDiffInSeconds = Properties.Settings.Default.MinTimeDiffInSeconds;

            // validate settings
            if (_backupDelayInSeconds < 1)
            {
                MessageBox.Show("Error: Settings invalid!" + Environment.NewLine + Environment.NewLine
                    + "BackupDelayInSeconds must be greater than 1.", "Safeguard");
                Application.Exit();
            }

            if (_minTimeDiffInSeconds < 1)
            {
                MessageBox.Show("Error: Settings invalid!" + Environment.NewLine + Environment.NewLine
                    + "MinTimeDiffInSeconds must be greater than 1 and greater than BackupDelayInSeconds.", "Safeguard");
                Application.Exit();
            }

            if (_maxNumberOfBackups < 1)
            {
                MessageBox.Show("Error: Settings invalid!" + Environment.NewLine + Environment.NewLine
                    + "MaxNumberOfBackups must be greater than 1.", "Safeguard");
                Application.Exit();
            }

            // check environment
            try
            {
                _baseFolder = Directory.GetCurrentDirectory();
                //_baseFolder = Application.StartupPath;
                _appPath = Path.Combine(_baseFolder, _appName);
                _savesPath = Path.Combine(_baseFolder, @"saves\saves.sav");
                _saveFolder = Path.Combine(_baseFolder, @"saves");
                _backupFolder = Path.Combine(_baseFolder, @"Safeguard");
            }
            catch (Exception)
            {
                MessageBox.Show("Error: Failed to obtain startup directory info!", "Safeguard");
                Application.Exit();
            }
            
            if (!File.Exists(_appPath))
            {
                MessageBox.Show("Error: 'Timber and Stone' not found!" + Environment.NewLine + "Safeguard.exe has to be in the same directory as 'Timber and Stone.exe'.", "Safeguard");
                Application.Exit();
            }

            if (!Directory.Exists(_saveFolder))
            {
                MessageBox.Show("Error: 'Timber and Stone' savegame directory 'saves' not found!", "Safeguard");
                Application.Exit();
            }

            if (!File.Exists(_savesPath))
            {
                MessageBox.Show("Error: 'Timber and Stone' savegame master file 'saves\\saves.sav' not found!", "Safeguard");
                Application.Exit();
            }

            try
            {
                Directory.CreateDirectory(_backupFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:  Could not create or access directory " + _backupFolder + Environment.NewLine + Environment.NewLine +
                    "Exception was: " + ex.Message, "Safeguard");
                Application.Exit();
            }
            
            // Prepare process info
            processTnS.StartInfo.FileName = _appPath;
            processTnS.StartInfo.WorkingDirectory = _baseFolder;

            // Start process
            try
            {
                processTnS.EnableRaisingEvents = true;
                processTnS.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:  Failed to start 'Timber and Stone'!" + Environment.NewLine + Environment.NewLine +
                    "Exception was: " + ex.Message, "Safeguard");
                Application.Exit();
            }

            // Monitor savegame changes
            try
            {
                fileSystemWatcherTnS.Path = _saveFolder;
                fileSystemWatcherTnS.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:  Failed to register savegame change monitoring!" + Environment.NewLine + Environment.NewLine +
                    "Exception was: " + ex.Message, "Safeguard");
                Application.Exit();
            }

            // Since we have now started the process and are monotoring it, remember to check for necessary cleanup afterwards
            _cleanupState = CleanupState.None;

            // Create backup list
            _lastBackups = new Dictionary<string, string>();

            // Create timer
            _backupTimer = new System.Windows.Forms.Timer();
            _backupTimer.Interval = _backupDelayInSeconds * 1000;
            _backupTimer.Tick += new EventHandler(OnTimerTick);

            // Create a simple tray menu with only one item
            _trayMenu = new ContextMenu();
            _trayMenu.MenuItems.Add("Close", OnTrayMenuExit);

            // Create a tray icon
            _trayIcon = new NotifyIcon();
            _trayIcon.Text = "Safeguard (Timber and Stone)";
            _trayIcon.Icon = Safeguard.Properties.Resources.app;

            // Add menu to tray icon and show it
            _trayIcon.ContextMenu = _trayMenu;
            _trayIcon.Visible = true;
        }

        private bool AskUserForCloseConfirmation()
        {
            if (IsProcessRunning())
            {
                if (MessageBox.Show("Timber and Stone seems to be running. Safeguard will exit by itself when Timber and Stone is closed." + Environment.NewLine +
                        "Closing Safeguard now will stop background savegame backups." + Environment.NewLine + Environment.NewLine +
                        "Do you really want to close Safeguard now?", "Safeguard", MessageBoxButtons.YesNo, MessageBoxIcon.None, MessageBoxDefaultButton.Button2)
                        == System.Windows.Forms.DialogResult.Yes)
                {
                    return true;
                }
                else
                {
                    this.WindowState = FormWindowState.Minimized;
                }
            }
            else if (_cleanupState == CleanupState.Running)
            {
                if (MessageBox.Show("Cleanup is in progress. Safeguard will exit by itself when it is finished." + Environment.NewLine +
                        "Closing Safeguard now may leave spare backups on your disk." + Environment.NewLine + Environment.NewLine +
                        "Do you really want to close Safeguard now?", "Safeguard", MessageBoxButtons.YesNo, MessageBoxIcon.None, MessageBoxDefaultButton.Button2)
                        == System.Windows.Forms.DialogResult.Yes)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsProcessRunning()
        {
            IntPtr handle = IntPtr.Zero;
            try
            {
                handle = processTnS.Handle;
                if (handle != IntPtr.Zero && processTnS.HasExited)
                {
                    handle = IntPtr.Zero;
                }
            }
            catch (Exception)
            {
                // couldn't obtain handle, process seems not to be running
                handle = IntPtr.Zero;
            }
            return (handle != IntPtr.Zero);
        }

        private void OnTrayMenuExit(object sender, EventArgs e)
        {
            // User initiated close via context menu, aks for confirmation
            if (AskUserForCloseConfirmation())
            {
                _cleanupState = CleanupState.Initiated;
                this.Close();
            }
        }

        private bool HasLastSavegameBackup(string savegame)
        {
            if (savegame == null || !_lastBackups.ContainsKey(savegame))
            {
                // we didn't backuped this save game during the actual session, so we couldn't have backuped the last savegame
                return false;
            }

            // Use the saves.sav file last write time as identifier when the last save was performed by Timber and Stone
            FileInfo source = new FileInfo(_savesPath);

            // Compare to backup creation time
            DirectoryInfo info = new DirectoryInfo(_lastBackups[savegame]);
            int diff = Convert.ToInt32((source.LastWriteTime - info.CreationTime.AddSeconds(_backupDelayInSeconds * -1)).TotalSeconds);
            Debug.WriteLine("Diff = " + diff);
            if (diff >= _backupDelayInSeconds * -1 && diff <= _backupDelayInSeconds)
            {
                return true;
            }

            return false;
        }

        private bool Cleanup()
        {
            if (_cleanupState == CleanupState.Initiated)
            {
                // Backup last saved game if it may be skipped due to time difference rule and then remove oldest backups until MaxBackups number is guaranteed
                _cleanupState = CleanupState.Running;
                JobType job = JobType.Cleanup;

                // If there is a backup in the pipeline stop it's timer, we will ensure anyways to backup the last savegame, which in this case will be this one
                _backupTimer.Stop();

                // Check if we backuped the last savegame to ensure always backup the last savegame of a playsession
                if (_backupSavegame != null && !HasLastSavegameBackup(_backupSavegame))
                {
                    // Do a backup depending on settings and time frame
                    if (Properties.Settings.Default.IgnoreMinTimeDiffOnClose || !BackupRecentlyPerformed(_backupSavegame))
                    {
                        Debug.WriteLine("cleanup with backup");
                        job |= JobType.Backup;
                    }
                }

                // let's roll
                backgroundWorkerTnS.RunWorkerAsync(new Work(job, _backupSavegame));

                return true;
            }
            return false;
        }

        private void processTnS_Exited(object sender, EventArgs e)
        {
            // Timber and Stone was closed, so we close also
            Debug.WriteLine("processTnS_Exited " + _cleanupState.ToString());
            if (_cleanupState != CleanupState.Running)
            {
                _cleanupState = CleanupState.Initiated;
            }
            this.Close();
        }

        private void FormMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Release the icon resource.
            if (_trayIcon != null)
            {
                _trayIcon.Dispose();
            }

            // Release the timer
            if (_backupTimer != null)
            {
                _backupTimer.Tick -= new EventHandler(OnTimerTick);
                _backupTimer.Stop();
                _backupTimer.Dispose();
            }

            // Goodby and have a nice day :)
            Environment.Exit(0);
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Quit without cleanup, when forced to exit via task manager or windows shutdown
            if (e.CloseReason == CloseReason.TaskManagerClosing || e.CloseReason == CloseReason.WindowsShutDown)
            {
                return;
            }
            // If there is a backup running, let it finish if we are not in cleanup process
            else if (_backupState == BackupState.Running && _cleanupState == CleanupState.None)
            {
                e.Cancel = true;
            }
            // Aks for confirmation on user closing (Alt-F4, clicking "x" on form)
            else if (e.CloseReason == CloseReason.UserClosing && _cleanupState != CleanupState.Initiated && _cleanupState != CleanupState.Finished)
            {
                if (!AskUserForCloseConfirmation())
                {
                    e.Cancel = true;
                    return;
                }
                else if (_cleanupState == CleanupState.Running)
                {
                    labelStatus.Text = "Canceling cleanup...";
                    buttonCancel.Enabled = false;
                    Application.DoEvents();
                    backgroundWorkerTnS.CancelAsync();
                }
            }
            else if (_cleanupState == CleanupState.Initiated)
            {
                // Show window to notify that we are about to close
                labelStatus.Text = "Performing cleanup...";
                this.ShowInTaskbar = true;
                this.WindowState = FormWindowState.Normal;
                Application.DoEvents();

                // Stop monotoring
                fileSystemWatcherTnS.EnableRaisingEvents = false;
                processTnS.EnableRaisingEvents = false;

                // Do pending cleanup work, if necessary
                e.Cancel = this.Cleanup();
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            // User initiated close via cancle button, aks for confirmation
            if (AskUserForCloseConfirmation())
            {
                if (_cleanupState != CleanupState.Running)
                {
                    _cleanupState = CleanupState.Initiated;
                }
                this.Close();
            }
        }

        private void RunBackup()
        {
            _backupState = BackupState.Running;
            labelStatus.Text = "Running backup: " + _backupSavegame;
            backgroundWorkerTnS.RunWorkerAsync(new Work(JobType.Backup, _backupSavegame));
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            _backupTimer.Stop();

            Debug.WriteLine("Tick " + _cleanupState.ToString());

            // Only run backup if we are not involved in a cleanup
            if (_cleanupState == CleanupState.None)
            {
                RunBackup();
            }
        }

        private void backgroundWorkerTnS_DoWork(object sender, DoWorkEventArgs e)
        {
            Work work = e.Argument as Work;
            if (work == null)
            {
                e.Result = null;
                return;
            }

            Debug.WriteLine("DoWork " + work.Target);
            try 
	        {	
                if ((work.Job & JobType.Backup) == JobType.Backup && work.Target != null)
                {
                    Debug.WriteLine("DoWork Backup");
                    // Copy all files in this new/updated savegame to our backup directory

                    string destinationFolder = Path.Combine(_backupFolder, work.Target);
                    string sourceFolder = Path.Combine(_saveFolder, work.Target);
                    Directory.CreateDirectory(destinationFolder);

                    string time = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
                    destinationFolder = Path.Combine(destinationFolder, time);
                    int i = 1;
                    string folder = destinationFolder;
                    while (Directory.Exists(folder))
                    {
                        folder = destinationFolder + " (" + i++ + ")";
                    }
                    Directory.CreateDirectory(folder);
                    
                    if (Directory.Exists(sourceFolder))
                    {
                        foreach (string file in Directory.GetFiles(sourceFolder))
                        {
                            FileInfo fileInfo = new FileInfo(file);
                            string target = Path.Combine(folder, fileInfo.Name);
                            fileInfo.CopyTo(target, true);
                        }
                    }

                    // remember that we did a backup
                    _lastBackups[work.Target] = folder;
                }
                if ((work.Job & JobType.Cleanup) == JobType.Cleanup)
                {
                    Debug.WriteLine("DoWork Cleanup");
                    // Loop through all backups and delete the oldest ones if the maximum backup limit was exceeded

                    if (Directory.Exists(_backupFolder))
                    {
                        foreach (string folder in Directory.GetDirectories(_backupFolder))
                        {
                            if (backgroundWorkerTnS.CancellationPending)
                            {
                                break;
                            }
                            else
                            {
                                List<string> subFolders = new List<string>(Directory.GetDirectories(folder));
                                subFolders.Sort(new DescendingComparer());

                                for (int i = 0; i < subFolders.Count; i++ )
                                {
                                    if (backgroundWorkerTnS.CancellationPending)
                                    {
                                        break;
                                    }

                                    if (i >= _maxNumberOfBackups)
                                    {
                                        Directory.Delete(subFolders[i], true);
                                    }
                                }
                            }
                        }
                    }
                }
	        }
	        catch (Exception)
            {
            }
            e.Result = work;
        }

        private void backgroundWorkerTnS_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Work work = e.Result as Work;

            if (work == null)
            {
                labelStatus.Text = "Monotoring Timber and Stone...";
                _backupState = BackupState.Idle;
                _cleanupState = CleanupState.None;
            }
            else if ((work.Job & JobType.Cleanup) == JobType.Cleanup)
            {
                labelStatus.Text = "Closing...";
                _cleanupState = CleanupState.Finished;
                this.Close();
            }
            else //  if (work.Job == JobType.Backup)
            {
                _backupState = BackupState.Idle;
            }
        }

        private void FormMain_Resize(object sender, EventArgs e)
        {
            // Have a nice rest ;)
            if (WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void HandleSavegameChange(FileSystemEventArgs e)
        {
            // Timber and Stone savegames consist of several *.sav files. We have to wait till all have been updated and then copy them all
            // We will do this by running a timer, resetting it every time a update occurs during the timer interval and process all files
            // when the interval went trough

            string savegame = null;
            try
            {
                savegame = Path.GetDirectoryName(e.Name);
            }
            catch (Exception)
            {
                // ups, shouldn't happen. ignore it and hope the best for next time
                return;
            }

            // if not already running a backup, initiate one
            if (_backupState == BackupState.Running)
            {
                if (savegame != _backupSavegame)
                {
                    // We can only handle one backup at once, but this shouldn't happen
                    return;
                }
                else
                {
                    // just another file update in the same savegame folder, reset timer
                    _backupTimer.Stop();
                    _backupTimer.Start();
                }
            }
            else if (!String.IsNullOrEmpty(savegame) && savegame.IndexOf('\\') == -1) // we don't care about subfolders
            {
                // let's do it
                InitiateBackup(savegame);
            }
        }

        private bool BackupRecentlyPerformed(string savegame, int timeDiff = -1)
        {
            if (savegame != null && _lastBackups.ContainsKey(savegame))
            {
                try 
                {	        
		            // Check if it's about time to backup this savegame
                    DirectoryInfo info = new DirectoryInfo(_lastBackups[savegame]);
                    int diff = Convert.ToInt32((DateTime.Now - info.CreationTime).TotalSeconds);
                
                    if (timeDiff < 0)
                    {
                        timeDiff = _minTimeDiffInSeconds;
                    }

                    if (diff < timeDiff)
                    {
                        // jupp, a backup of this savegame was done within the backup time frame
                        return true;
                    }
                }
                catch (Exception)
                {
                    return false;
                }
                
            }
            return false;
        }

        private void InitiateBackup(string savegame, bool force = false, bool delayed = true)
        {
            // Always run backups in cleanup state, this will ensure to do a backup when Timber and Stone was closed
            // In any other case we will check if the last backup for this savegame was to recent to do a new one
            if (!force)
            {
                // Check if it's about time to backup this savegame
                if (BackupRecentlyPerformed(savegame))
                {
                    // nope, wait for the next attempt
                    return;
                }
            }

            // init backup and start timer
            _backupState = BackupState.Running;
            _backupSavegame = savegame;
            if (delayed)
            {
                _backupTimer.Start();
            }
            else
            {
                RunBackup();
            }
        }

        private void fileSystemWatcherTnS_Changed(object sender, FileSystemEventArgs e)
        {
            //Debug.WriteLine("Change=" + e.Name + "; " + e.ChangeType + "(" + (int)e.ChangeType + "); " + e.FullPath);

            HandleSavegameChange(e);
        }

        private void fileSystemWatcherTnS_Created(object sender, FileSystemEventArgs e)
        {
            //Debug.WriteLine("Create=" + e.Name + "; " + e.ChangeType + "(" + (int)e.ChangeType + "); " + e.FullPath);

            HandleSavegameChange(e);
        }
    }
}
