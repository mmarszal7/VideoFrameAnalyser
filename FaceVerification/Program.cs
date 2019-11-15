using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FaceVerifier
{
    public static class Program
    {
        private const int detectionInterval = 10000;
        private const int maxFailures = 2;

        private static int lockTimer = 0;
        private static bool paused = false;
        private static bool warningShown = false;

        private static readonly SessionManager SessionManager = new SessionManager();
        private static readonly FaceVerifier FaceVerifier = new FaceVerifier();
        private static readonly NotifyIcon TrayIcon = new NotifyIcon();

        public static void Main(string[] args)
        {
            var timer = new System.Timers.Timer() { Interval = detectionInterval };
            timer.Elapsed += new System.Timers.ElapsedEventHandler(VerifyFace);
            timer.Start();

            SetupTrayIcon();
            Application.Run();
        }

        private static async void VerifyFace(object s, EventArgs e)
        {
            if (paused || IsScreenLocked()) return;

            var confidence = await FaceVerifier.VerifyFace();
            if (confidence < 0.5)
            {
                lockTimer++;
                await ShowWarning();

                if (lockTimer > maxFailures)
                {
                    SessionManager.LockWorkStation();
                    lockTimer = 0;
                }
            }
            else
            {
                lockTimer = 0;
            }

            TrayIcon.Text = confidence.ToString();
        }

        private static bool IsScreenLocked()
        {
            var lockProcess = Process.GetProcessesByName("lockapp");

            if (lockProcess.Length == 0)
            {
                SessionManager.LockWorkStation();
            }

            return lockProcess.First().Threads[0].WaitReason != ThreadWaitReason.Suspended;
        }

        private static async Task ShowWarning()
        {
            await Task.Run(() =>
            {
                if (warningShown) return;

                warningShown = true;
                if (MessageBox.Show(new Form() { TopMost = true }, "...", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning) == DialogResult.OK)
                {
                    warningShown = false;
                }
            });
        }

        private static void SetupTrayIcon()
        {
            ContextMenu TrayMenu = new ContextMenu();
            TrayMenu.MenuItems.Add("Pause", (s, e) =>
            {
                paused = !paused;
                ((MenuItem) s).Checked = paused;
            });

            TrayIcon.ContextMenu = TrayMenu;
            TrayIcon.Icon = new Icon(SystemIcons.Question, 40, 40);
            TrayIcon.Visible = true;
        }
    }
}
