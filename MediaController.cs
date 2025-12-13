using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GamepadMpcController
{
    public class MediaController
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

        private bool focusedOnce = false; // fix VLC bug on first launch

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

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

        private IntPtr GetVlcHandle()
        {
            // Classes utilisées par VLC
            string[] possibleClasses =
            {
                "Qt5QWindowIcon",
                "QWidget",              // VLC interface Qt5
                "vlc",                  // certains builds Windows
                "VLC media player"      // fallback titre
            };

            foreach (var cls in possibleClasses)
            {
                IntPtr hwnd = FindWindow(cls, null);
                if (hwnd != IntPtr.Zero)
                    return hwnd;
            }

            // Fallback par titre exact
            IntPtr byTitle = FindWindow(null, "VLC media player");
            if (byTitle != IntPtr.Zero)
                return byTitle;

            return IntPtr.Zero;
        }

        public void PlayPause()
        {
            IntPtr mpc = GetMpcHandle();
            if (mpc != IntPtr.Zero)
            {
                Send(CMD_PLAYPAUSE);
                return;
            }

            IntPtr vlc = GetVlcHandle();
            if (vlc != IntPtr.Zero)
            {
                if (!focusedOnce)
                {
                    ForceFocus(vlc); // éviter le bug VLC au premier lancement
                    focusedOnce = true;
                }
                
                SendKeyTo(vlc,Keys.Space);
            }
        }

        public void SeekForward()
        {
            IntPtr mpc = GetMpcHandle();
            if (mpc != IntPtr.Zero)
            {
                Send(CMD_SEEKFORWARD);
                return;
            }

            IntPtr vlc = GetVlcHandle();
            if (vlc != IntPtr.Zero)
            {
                // VLC: CTRL + RIGHT = +10s
                SendComboKey(vlc, Keys.ControlKey, Keys.Right);
            }
        }

        public void SeekBackward()
        {
            IntPtr mpc = GetMpcHandle();
            if (mpc != IntPtr.Zero)
            {
                Send(CMD_SEEKBACKWARD);
                return;
            }

            IntPtr vlc = GetVlcHandle();
            if (vlc != IntPtr.Zero)
            {
                SendComboKey(vlc, Keys.ControlKey, Keys.Left);
            }
        }

        public void Fullscreen()
        {
            IntPtr mpc = GetMpcHandle();
            if (mpc != IntPtr.Zero)
            {
                Send(CMD_FULLSCREEN);
                return;
            }

            IntPtr vlc = GetVlcHandle();
            if (vlc != IntPtr.Zero)
            {
                SendKeyTo(vlc, Keys.F);
            }
        }

        public void Next()
        {
            // Priorité MPC
            IntPtr mpc = GetMpcHandle();
            if (mpc != IntPtr.Zero)
            {
                Send(CMD_NEXT);
                return;
            }

            // Sinon VLC : touche 'N' (Next)
            IntPtr vlc = GetVlcHandle();
            if (vlc != IntPtr.Zero)
            {
                SendKeyTo(vlc, Keys.N);
            }
        }

        public void Previous()
        {
            // MPC prioritaire
            IntPtr mpc = GetMpcHandle();
            if (mpc != IntPtr.Zero)
            {
                Send(CMD_PREV);
                return;
            }

            // VLC
            IntPtr vlc = GetVlcHandle();
            if (vlc != IntPtr.Zero)
            {
                // 1. Touche 'P'
                SendKeyTo(vlc, Keys.P);

                // Petite pause nécessaire pour certains lecteurs VLC
                System.Threading.Thread.Sleep(30);

                // 2. Ctrl + P
                SendComboKey(vlc, Keys.ControlKey, Keys.P);

                System.Threading.Thread.Sleep(30);

                // 3. Shift + P
                SendComboKey(vlc, Keys.ShiftKey, Keys.P);

                return;
            }
        }

        public void VolumeUp()
        {
            // Priorité MPC
            IntPtr mpc = GetMpcHandle();
            if (mpc != IntPtr.Zero)
            {
                Send(CMD_VOLUP);
                return;
            }

            // Sinon VLC : touche 'P' (Previous)
            IntPtr vlc = GetVlcHandle();
            if (vlc != IntPtr.Zero)
            {
                SendKeyTo(vlc, Keys.Up);
            }
        }

        public void VolumeDown()
        {
            IntPtr mpc = GetMpcHandle();
            if (mpc != IntPtr.Zero)
            {
                Send(CMD_VOLDOWN);
                return;
            }

            IntPtr vlc = GetVlcHandle();
            if (vlc != IntPtr.Zero)
            {
                SendKeyTo(vlc, Keys.Down);
            }
        }

        public void Stop()
        {
            // Priorité MPC
            IntPtr mpc = GetMpcHandle();
            if (mpc != IntPtr.Zero)
            {
                Send(CMD_STOP);
                return;
            }

            // Sinon VLC : touche 'S' (Stop)
            IntPtr vlc = GetVlcHandle();
            if (vlc != IntPtr.Zero)
            {
                SendKeyTo(vlc, Keys.S);
                return;
            }
        }

        public void StopAndMinimize()
        {
            // Priorité MPC
            IntPtr mpc = GetMpcHandle();
            if (mpc != IntPtr.Zero)
            {
                Send(CMD_STOP);
                ShowWindow(mpc, SW_MINIMIZE);
                return;
            }

            // Sinon VLC : Stop puis minimise la fenêtre
            IntPtr vlc = GetVlcHandle();
            if (vlc != IntPtr.Zero)
            {
                SendKeyTo(vlc, Keys.S);
                // petite pause pour laisser VLC traiter le stop avant de minimiser
                System.Threading.Thread.Sleep(40);
                ShowWindow(vlc, SW_MINIMIZE);
                return;
            }
        }

        private void Send(int commandId)
        {
            var hwnd = GetMpcHandle();
            //if (hwnd != IntPtr.Zero)
            //{
            //    SendMessage(hwnd, WM_COMMAND, commandId, 0);
            //}
            if (hwnd == IntPtr.Zero)
                return;

            // Empêche l'envoi de commandes avant que MPC soit vraiment lancé
            // (élimine le bug "MPC démarre au lancement de Gamepad Controller")
            const int GWL_STYLE = -16;
            int style = GetWindowLong(hwnd, GWL_STYLE);
            if (style == 0)
                return;

            SendMessage(hwnd, WM_COMMAND, commandId, 0);
        }
       

        private void SendKeyTo(IntPtr hwnd, Keys k)
        {
            const int WM_KEYDOWN = 0x0100;
            const int WM_KEYUP = 0x0101;

            // Assurer que VLC reçoit la touche            
            System.Threading.Thread.Sleep(50); // petite pause pour fiabiliser

            SendMessage(hwnd, WM_KEYDOWN, (int)k, 0);
            SendMessage(hwnd, WM_KEYUP, (int)k, 0);
        }

        private void SendComboKey(IntPtr hwnd, Keys modifier, Keys key)
        {
            const int WM_KEYDOWN = 0x0100;
            const int WM_KEYUP = 0x0101;

            SendMessage(hwnd, WM_KEYDOWN, (int)modifier, 0);
            SendMessage(hwnd, WM_KEYDOWN, (int)key, 0);
            SendMessage(hwnd, WM_KEYUP, (int)key, 0);
            SendMessage(hwnd, WM_KEYUP, (int)modifier, 0);
        }

        private void ForceFocus(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                return;

            // Amener la fenêtre au premier plan
            BringWindowToTop(hwnd);
            SetForegroundWindow(hwnd);

            // Laisser Qt respirer
            System.Threading.Thread.Sleep(80);

            // Récupérer la taille de la fenêtre
            if (!GetWindowRect(hwnd, out RECT rect))
                return;

            // Calcul du centre de la fenêtre
            int centerX = rect.Left + (rect.Right - rect.Left) / 2;
            int centerY = rect.Top + (rect.Bottom - rect.Top) / 2;

            // Déplacer la souris
            SetCursorPos(centerX, centerY);

            // Simuler clic gauche
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);

            // Pause finale pour garantir le focus clavier
            System.Threading.Thread.Sleep(80);
        }

    }
}
