using SharpDX.DirectInput;
using System;

namespace GamepadMpcController
{
    public class GamepadManager
    {
        private readonly DirectInput directInput;
        private Joystick joystick;
        private DateTime lastReconnectAttempt = DateTime.MinValue;

        public GamepadManager()
        {
            directInput = new DirectInput();
            Initialize();
        }

        private void Initialize()
        {
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
            foreach (var device in directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AttachedOnly))
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
            foreach (var device in directInput.GetDevices(DeviceType.ControlDevice, DeviceEnumerationFlags.AttachedOnly))
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

            Initialize();
        }
    }
}
