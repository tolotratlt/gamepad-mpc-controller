using System;
using System.Runtime.InteropServices;

namespace GamepadMpcController
{
    public class MpcController
    {
        private const int WM_COMMAND = 0x0111;

        private const int CMD_PLAYPAUSE = 889;
        private const int CMD_SEEKFORWARD = 900;
        private const int CMD_SEEKBACKWARD = 899;
        private const int CMD_FULLSCREEN = 830;
        private const int CMD_NEXT = 922;
        private const int CMD_PREV = 921;
        private const int CMD_VOLUP = 907;
        private const int CMD_VOLDOWN = 908;
        private const int CMD_STOP = 890;

        private const int SW_MINIMIZE = 6;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);


        private IntPtr GetMpcHandle()
        {
            // Toutes les classes possibles MPC-HC et MPC-BE
            string[] possibleClasses = new[]
            {
                // MPC-HC modern
                "MediaPlayerClassicW64",
                "MediaPlayerClassicW",

                // MPC-HC legacy
                "MediaPlayerClassic",
                "Media Player Classic",

                // MPC-BE
                "MPC-BE",
                "MPC-BE x64"
            };

            IntPtr hwnd = IntPtr.Zero;

            // On teste toutes les classes possibles
            foreach (var cls in possibleClasses)
            {
                hwnd = FindWindow(cls, null);
                if (hwnd != IntPtr.Zero)
                    return hwnd;
            }

            // fallback: rechercher par titre si classe inconnue
            hwnd = FindWindow(null, "Media Player Classic");
            if (hwnd != IntPtr.Zero)
                return hwnd;

            hwnd = FindWindow(null, "MPC-BE");
            if (hwnd != IntPtr.Zero)
                return hwnd;

            return IntPtr.Zero;
        }


        public void PlayPause()
        {
            Send(CMD_PLAYPAUSE);
        }

        public void SeekForward()
        {
            Send(CMD_SEEKFORWARD);
        }

        public void SeekBackward()
        {
            Send(CMD_SEEKBACKWARD);
        }

        public void Fullscreen()
        {
            Send(CMD_FULLSCREEN);
        }

        public void Next()
        {
            Send(CMD_NEXT);
        }

        public void Previous()
        {
            Send(CMD_PREV);
        }

        public void VolumeUp()
        {
            Send(CMD_VOLUP);
        }

        public void VolumeDown()
        {
            Send(CMD_VOLDOWN);
        }

        public void Stop()
        {
            Send(CMD_STOP);
        }

        public void StopAndMinimize()
        {
            var hwnd = GetMpcHandle();
            if (hwnd == IntPtr.Zero)
                return;

            SendMessage(hwnd, WM_COMMAND, CMD_STOP, 0);
            ShowWindow(hwnd, SW_MINIMIZE);
        }

        private void Send(int commandId)
        {
            var hwnd = GetMpcHandle();
            if (hwnd != IntPtr.Zero)
            {
                SendMessage(hwnd, WM_COMMAND, commandId, 0);
            }
        }
    }
}
