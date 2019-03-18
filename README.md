# Analysing Video Frames with [Azure Cognitive Services](https://westus.dev.cognitive.microsoft.com/docs/services/563879b61984550e40cbbe8d/operations/563879b61984550f30395236) and [OpenCVSharp](https://github.com/shimat/opencvsharp)

Code in FaceVerification project allows you to automatically lock your computer when it can not recognize your face.

## How to run:

1. **FaceVerification Console App** - fill in information in const variables in Program.cs
2. **WPF app** - just Build & Run and paste Azure subscription information in settings tab

## Running face verification console app as a service:
Run cmd as admin and paste:
sc CREATE "Face Verifier" binpath= ".\FaceVerification\bin\Debug\FaceVerifier.exe"

OR

1. Download NSSM
2. Install your sevice with `nssm.exe install [serviceName]`
3. Locate your executable from GUI
