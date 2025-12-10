namespace GamepadMpcController
{
    public class AxisRule
    {
        public string Name;
        public string AxisName;   // "X", "Y", "Z", "Rx", "Ry", "Rz"
        public int Threshold = 48000;
        public bool TriggerAbove = true;

        public bool IsActivated(GamepadState s)
        {
            int value = 0;

            switch (AxisName)
            {
                case "X": value = s.X; break;
                case "Y": value = s.Y; break;
                case "Z": value = s.Z; break;
                case "Rx": value = s.Rx; break;
                case "Ry": value = s.Ry; break;
                case "Rz": value = s.Rz; break;
            }

            if (TriggerAbove)
                return value > Threshold;
            else
                return value < Threshold;
        }
    }
}
