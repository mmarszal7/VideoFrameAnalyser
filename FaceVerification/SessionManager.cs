using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace FaceVerifier
{
    public class SessionManager
    {
        public bool ScreenLocked { get; set; }

        public SessionManager()
        {
            SystemEvents.SessionSwitch += SessionSwitched;
        }

        [DllImport("user32.dll")]
        public static extern bool LockWorkStation();

        private void SessionSwitched(object s, SessionSwitchEventArgs e)
        {
            ScreenLocked = e.Reason == SessionSwitchReason.SessionLock ? true : false;
        }
    }
}
