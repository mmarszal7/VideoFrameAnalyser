using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.Win32;
using OpenCvSharp;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BasicConsoleSample
{
    public static class Program
    {
        private const int detectionInterval = 10000;
        private const int cameraNumber = 0;
        private const string subscriptionKey = "";
        private const string apiRoot = "https://westeurope.api.cognitive.microsoft.com/face/v1.0";
        private const string referenceImagePath = "./myFace.jpg";

        private static bool Paused = false;
        private static int lockTimer = 0;
        private static int maxFailures = 2;
        private static FaceServiceClient faceClient = new FaceServiceClient(subscriptionKey, apiRoot);

        private static Guid myFaceID = new Guid();
        private static Guid? recognizedFaceID = new Guid();

        public static void Main(string[] args)
        {
            using (Stream myFaceImage = new FileStream(referenceImagePath, FileMode.Open))
            {
                var myFace = faceClient.DetectAsync(myFaceImage, returnFaceId: true).Result;
                if (myFace.Length > 0)
                    myFaceID = myFace[0].FaceId;
            }

            var timer = new System.Timers.Timer() { Interval = detectionInterval };
            timer.Elapsed += new System.Timers.ElapsedEventHandler(VerifyFace);
            timer.Start();

            SystemEvents.SessionSwitch += SessionSwitched;
            SetupTrayIcon();
        }

        private static async void VerifyFace(object s, EventArgs e)
        {
            if (Paused) return;

            var frame = GetCameraImage();

            var recognizedFace = await faceClient.DetectAsync(frame.ToMemoryStream(".jpg"), returnFaceId: true);
            recognizedFaceID = recognizedFace.Length > 0 ? recognizedFace[0].FaceId : (Guid?)null;

            VerifyResult result = new VerifyResult() { Confidence = 0 };

            if (recognizedFaceID != null)
                result = await faceClient.VerifyAsync(myFaceID, (Guid)recognizedFaceID);

            Console.WriteLine((result.Confidence > 0.5 ? "Ok" : "Locking...") + " - " + result.Confidence);

            if (result.Confidence < 0.5)
            {
                lockTimer++;

                if (lockTimer == 1)
                    await Task.Run(() => MessageBox.Show(new Form() { TopMost = true }, "...", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning));

                if (lockTimer > maxFailures)
                {
                    LockWorkStation();
                    Paused = true;
                    lockTimer = 0;
                }
            }
            else
            {
                lockTimer = 0;
            }
        }

        private static Mat GetCameraImage()
        {
            VideoCapture _reader = new VideoCapture(cameraNumber);
            Mat image = new Mat();
            _reader.Read(image);
            return image;
        }

        [DllImport("user32.dll")]
        private static extern bool LockWorkStation();

        private static void SessionSwitched(object s, SessionSwitchEventArgs e) =>
           Paused = e.Reason == SessionSwitchReason.SessionLock ? true : false;

        private static void SetupTrayIcon()
        {
            NotifyIcon trayIcon = new NotifyIcon();
            trayIcon.Text = "Face Detector";
            trayIcon.Icon = new Icon(SystemIcons.Question, 40, 40);

            ContextMenu trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Running", (s, e) =>
            {
                trayIcon.ContextMenu.MenuItems[0].Checked = !trayIcon.ContextMenu.MenuItems[0].Checked;
                Paused = !Paused;
            });
            trayMenu.MenuItems[0].Checked = true;

            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;
            Application.Run();
        }
    }
}
