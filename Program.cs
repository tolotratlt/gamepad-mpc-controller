using System;
using System.Threading;
using System.Windows.Forms;

namespace GamepadMpcController
{
    static class Program
    {
        public static MainForm FormRef;

        [STAThread]
        static void Main()
        {
            // Mutex pour empêcher les doubles instances
            bool createdNew = false;
            using (var mutex = new Mutex(true, "GamepadMpcController_SingleInstance", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show(
                        "Gamepad MPC Controller is already running.",
                        "Already Running",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
        }
    }
}
