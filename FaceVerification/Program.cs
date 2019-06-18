using System;
using System.Drawing;
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
            if (paused || SessionManager.ScreenLocked) return;

            var confidence = await FaceVerifier.VerifyFace();
            if (confidence < 0.5)
            {
                lockTimer++;

                if (lockTimer == 1)
                    await Task.Run(() => MessageBox.Show(new Form() { TopMost = true }, "...", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning));

                if (lockTimer > maxFailures)
                {
                    SessionManager.LockWorkStation();
                    SessionManager.ScreenLocked = true;
                    lockTimer = 0;
                }
            }
            else
            {
                lockTimer = 0;
            }

            TrayIcon.Text = confidence.ToString();
        }

        private static void SetupTrayIcon()
        {
            ContextMenu TrayMenu = new ContextMenu();
            TrayMenu.MenuItems.Add("Pause", (s, e) =>
            {
                paused = !paused;
                (s as MenuItem).Checked = paused;
            });

            TrayIcon.ContextMenu = TrayMenu;
            TrayIcon.Icon = new Icon(SystemIcons.Question, 40, 40);
            TrayIcon.Visible = true;
        }
    }
}
