using SharpDX.DirectInput;
using SharpDX.XInput;
using System;

namespace GamepadMpcController
{
    public class GamepadManager
    {
        private readonly DirectInput directInput;
        private Joystick joystick;
        private DateTime lastReconnectAttempt = DateTime.MinValue;
        private Controller xinputController;
        private bool hasXInput;

        private const int DEADZONE = 8000;
        public GamepadManager()
        {
            directInput = new DirectInput();
            Initialize();
        }

        private void Initialize()
        {
            xinputController = new Controller(UserIndex.One);
            hasXInput = xinputController.IsConnected;

            if (hasXInput)
            {
                joystick = null;
                return;
            }

            joystick = null;

            // 1. Gamepad standard
            foreach (var device in directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly))
            {
                try
                {
                    joystick = new Joystick(directInput, device.InstanceGuid);
                    joystick.Acquire();
                    return;
                }
                catch { }
            }

            // 2. Joysticks génériques (DualSense arrive souvent ici)
            foreach (var device in directInput.GetDevices(SharpDX.DirectInput.DeviceType.Joystick, DeviceEnumerationFlags.AttachedOnly))
            {
                try
                {
                    joystick = new Joystick(directInput, device.InstanceGuid);
                    joystick.Acquire();
                    return;
                }
                catch { }
            }

            // 3. Fallback supplémentaire (certaines manettes BT se déclarent en "ControlDevice")
            foreach (var device in directInput.GetDevices(SharpDX.DirectInput.DeviceType.ControlDevice, DeviceEnumerationFlags.AttachedOnly))
            {
                try
                {
                    joystick = new Joystick(directInput, device.InstanceGuid);
                    joystick.Acquire();
                    return;
                }
                catch { }
            }

            joystick = null;
        }


        public GamepadState GetState()
        {
            // XINPUT FIRST
            if (hasXInput && xinputController != null)
            {
                try
                {
                    var state = xinputController.GetState();
                    return ConvertXInputToState(state);
                }
                catch
                {
                    hasXInput = false;
                    xinputController = null;
                }
            }

            // DIRECTINPUT fallback
            // Si aucune manette active → tenter de reconnecter
            if (joystick == null)
            {
                TryReconnect();
                return null;
            }

            try
            {
                // Certaines manettes DirectInput perdent l'acquisition → reacquire
                try
                {
                    joystick.Poll();
                }
                catch
                {
                    try { joystick.Acquire(); } catch { }
                }

                var s = joystick.GetCurrentState();

                if (s == null)
                    return null;

                return new GamepadState
                {
                    Buttons = s.Buttons,
                    X = s.X,
                    Y = s.Y,
                    Z = s.Z,
                    Rx = s.RotationX,
                    Ry = s.RotationY,
                    Rz = s.RotationZ,
                    POV = (s.PointOfViewControllers != null && s.PointOfViewControllers.Length > 0)
                        ? s.PointOfViewControllers[0]
                        : -1
                };
            }
            catch
            {
                // Déconnexion physique, BT sleep, USB drop, etc
                joystick = null;
                return null;
            }
        }


        private void TryReconnect()
        {
            // Tentative toutes les 2 secondes max
            if ((DateTime.Now - lastReconnectAttempt).TotalSeconds < 2)
                return;

            lastReconnectAttempt = DateTime.Now;

            xinputController = new Controller(UserIndex.One);
            hasXInput = xinputController.IsConnected;

            if (hasXInput)
            {
                joystick = null;
                return;
            }


            Initialize();
        }

        private GamepadState ConvertXInputToState(State s)
        {
            var gp = s.Gamepad;

            int lx = Math.Abs(gp.LeftThumbX) < DEADZONE ? 0 : gp.LeftThumbX;
            int ly = Math.Abs(gp.LeftThumbY) < DEADZONE ? 0 : gp.LeftThumbY;

            int rx = Math.Abs(gp.RightThumbX) < DEADZONE ? 0 : gp.RightThumbX;
            int ry = Math.Abs(gp.RightThumbY) < DEADZONE ? 0 : gp.RightThumbY;

            bool[] buttons = new bool[16];

            buttons[0] = (gp.Buttons & GamepadButtonFlags.A) != 0;
            buttons[1] = (gp.Buttons & GamepadButtonFlags.B) != 0;
            buttons[2] = (gp.Buttons & GamepadButtonFlags.X) != 0;
            buttons[3] = (gp.Buttons & GamepadButtonFlags.Y) != 0;

            buttons[4] = (gp.Buttons & GamepadButtonFlags.LeftShoulder) != 0;
            buttons[5] = (gp.Buttons & GamepadButtonFlags.RightShoulder) != 0;

            buttons[6] = (gp.Buttons & GamepadButtonFlags.Back) != 0;
            buttons[7] = (gp.Buttons & GamepadButtonFlags.Start) != 0;

            buttons[8] = (gp.Buttons & GamepadButtonFlags.LeftThumb) != 0;
            buttons[9] = (gp.Buttons & GamepadButtonFlags.RightThumb) != 0;

            int pov = -1;
            if ((gp.Buttons & GamepadButtonFlags.DPadUp) != 0) pov = 0;
            else if ((gp.Buttons & GamepadButtonFlags.DPadRight) != 0) pov = 9000;
            else if ((gp.Buttons & GamepadButtonFlags.DPadDown) != 0) pov = 18000;
            else if ((gp.Buttons & GamepadButtonFlags.DPadLeft) != 0) pov = 27000;

            return new GamepadState
            {
                Buttons = buttons,

                // LEFT STICK
                X = lx,
                Y = ly,

                // RIGHT STICK
                Z = rx,
                Rx = ry,

                // TRIGGERS
                Ry = gp.LeftTrigger,
                Rz = gp.RightTrigger,

                POV = pov
            };
        }
    }
}
