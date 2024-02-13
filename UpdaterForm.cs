﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Octokit;

namespace LunaUpdater
{
    public partial class UpdaterForm : Form
    {
        // Sets the window to be foreground
        [DllImport("User32")]
        private static extern int SetForegroundWindow(IntPtr hwnd);

        // Activate or minimize a window
        [DllImport("User32.DLL")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_RESTORE = 9;

        readonly Release release_;
        readonly Updater updater_;
        string tempFilePath_;

        public UpdaterForm(Updater updater, Release release)
        {
            InitializeComponent();

            release_ = release;
            updater_ = updater;
            labelUpdate.Text = "New version available: " + release_.TagName + "\nDo you want to update?";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (tempFilePath_ != null)
            {
                File.Delete(tempFilePath_);
            }
        }

        private void buttonIgnore_Click(object sender, EventArgs e)
        {
            Close();
        }

        static void BringSelfToForeGround()
        {
            Process[] procList = Process.GetProcessesByName("LunaUpdater");
            if (procList.Length == 0)
            {
                return;
            }

            ShowWindow(procList[0].MainWindowHandle, SW_RESTORE);
            SetForegroundWindow(procList[0].MainWindowHandle);
        }

        private async void buttonUpdate_Click(object sender, EventArgs e)
        {
            if (tempFilePath_ == null)
            {
                buttonUpdate.Enabled = false;
                buttonIgnore.Enabled = false;
                labelUpdate.Text = $"Downloading an update '{release_.TagName}'...";
                tempFilePath_ = await updater_.DownloadLatestRelease(release_);
                labelUpdate.Text = $"Downloaded '{release_.TagName}' successfully!\nDo you want to install?";
                buttonUpdate.Text = "Install";
                buttonUpdate.Enabled = true;
                buttonIgnore.Enabled = true;
                BringSelfToForeGround();
            }
            else
            {
                buttonUpdate.Enabled = false;
                buttonIgnore.Enabled = false;
                labelUpdate.Text = $"Installing '{release_.TagName}'...";
                await Task.Run(() =>
                {
                    using (ZipArchive archive = ZipFile.OpenRead(tempFilePath_))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            if (entry.Name == "Project64.exe")
                            {
                                entry.ExtractToFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, entry.Name), true);
                            }
                        }
                    }
                });
                //await Task.Run(() => { ZipFile.ExtractToDirectory(tempFilePath_, AppDomain.CurrentDomain.BaseDirectory); });
                MessageBox.Show("The update has been installed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Process.Start("Project64.exe");
                Close();
            }
        }
    }
}
