using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using OpenCvSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FaceVerifier
{
    public class FaceVerifier
    {
        private const int cameraNumber = 0;
        private const string subscriptionKey = "";
        private const string apiRoot = "https://westeurope.api.cognitive.microsoft.com/face/v1.0";
        private const string referenceImagePath = "./myFace.jpg";

        private static FaceServiceClient faceClient = new FaceServiceClient(subscriptionKey, apiRoot);
        private static Guid myFaceID = new Guid();
        private static Guid? recognizedFaceID = new Guid();

        public FaceVerifier()
        {
            using (Stream myFaceImage = new FileStream(referenceImagePath, FileMode.Open))
            {
                var myFace = faceClient.DetectAsync(myFaceImage, returnFaceId: true).Result;
                if (myFace.Length > 0)
                    myFaceID = myFace[0].FaceId;
            }
        }

        public async Task<double> VerifyFace()
        {
            var frame = GetCameraImage();

            var recognizedFace = await faceClient.DetectAsync(frame.ToMemoryStream(".jpg"), returnFaceId: true);
            recognizedFaceID = recognizedFace.Length > 0 ? recognizedFace[0].FaceId : (Guid?)null;

            VerifyResult result = new VerifyResult() { Confidence = 0 };

            if (recognizedFaceID != null)
                result = await faceClient.VerifyAsync(myFaceID, (Guid)recognizedFaceID);

            return result.Confidence;
        }

        private static Mat GetCameraImage()
        {
            VideoCapture _reader = new VideoCapture(cameraNumber);
            Mat image = new Mat();
            _reader.Read(image);
            return image;
        }
    }
}
