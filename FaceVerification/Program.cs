using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using VideoFrameAnalyzer;

namespace BasicConsoleSample
{
    public static class Program
    {
        private static bool Running = true;

        public static void Main()
        {
            const int detectionInterval = 10000;
            const int cameraNumber = 0;
            const string subscriptionKey = "";
            const string apiRoot = "https://westeurope.api.cognitive.microsoft.com/face/v1.0";
            const string referenceImagePath = "./myFace.jpg";

            FrameGrabber<Face[]> grabber = new FrameGrabber<Face[]>();
            FaceServiceClient faceClient = new FaceServiceClient(subscriptionKey, apiRoot);

            Guid myFaceID = new Guid();
            Guid? recognizedFaceID = new Guid();
            int lockTimer = 0;

            using (Stream myFaceImage = new FileStream(referenceImagePath, FileMode.Open))
            {
                var myFace = faceClient.DetectAsync(myFaceImage, returnFaceId: true).Result;
                if (myFace.Length > 0)
                    myFaceID = myFace[0].FaceId;
            }

            grabber.AnalysisFunction = async frame =>
            {
                if (!Running) return null;

                var recognizedFace = await faceClient.DetectAsync(frame.Image.ToMemoryStream(".jpg"), returnFaceId: true);
                recognizedFaceID = recognizedFace.Length > 0 ? recognizedFace[0].FaceId : (Guid?)null;

                return recognizedFace;
            };

            grabber.NewResultAvailable += async (s, e) =>
            {
                if (!Running) return;

                lockTimer++;
                VerifyResult result = new VerifyResult() { Confidence = 0 };

                if (recognizedFaceID != null)
                    result = await faceClient.VerifyAsync(myFaceID, (Guid)recognizedFaceID);

                Console.WriteLine((result.Confidence > 0.5 ? "Ok" : "Locking...") + " - " + result.Confidence);

                if (result.Confidence < 0.5 && lockTimer == 2)
                    await Task.Run(() => MessageBox.Show("", "Face verifier", MessageBoxButtons.OK, MessageBoxIcon.Warning));

                if (result.Confidence < 0.5 && lockTimer > 2)
                {
                    LockWorkStation();
                    lockTimer = 0;
                }
            };

            grabber.TriggerAnalysisOnInterval(TimeSpan.FromMilliseconds(detectionInterval));

            grabber.StartProcessingCameraAsync(cameraNumber).Wait();

            SetupTrayIcon();

            Console.ReadKey();

            grabber.StopProcessingAsync().Wait();
        }

        [DllImport("user32.dll")]
        private static extern bool LockWorkStation();

        private static void SetupTrayIcon()
        {
            NotifyIcon trayIcon = new NotifyIcon();
            trayIcon.Text = "Face Detector";
            trayIcon.Icon = new Icon(SystemIcons.Question, 40, 40);

            ContextMenu trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Running", (s, e) =>
            {
                trayIcon.ContextMenu.MenuItems[0].Checked = !trayIcon.ContextMenu.MenuItems[0].Checked;
                Running = !Running;
            });
            trayMenu.MenuItems[0].Checked = true;

            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;
            Application.Run();
        }
    }
}
