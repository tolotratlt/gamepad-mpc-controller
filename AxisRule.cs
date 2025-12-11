namespace GamepadMpcController
{
    public class AxisRule
    {
        public string Name;

        // "X", "Y", "Z", "Rx", "Ry", "Rz"
        public string AxisName;

        // Ce threshold est ignoré si auto-threshold détecte XInput trigger
        public int Threshold = 15000;

        public bool TriggerAbove = true;

        // Pour éviter le spam → edge-trigger
        private bool wasActive = false;

        public bool IsActivated(GamepadState s)
        {
            int value = 0;

            // Sélection de l'axe
            if (AxisName == "X") value = s.X;
            else if (AxisName == "Y") value = s.Y;
            else if (AxisName == "Z") value = s.Z;
            else if (AxisName == "Rx") value = s.Rx;
            else if (AxisName == "Ry") value = s.Ry;   // peut être LT (0-255)
            else if (AxisName == "Rz") value = s.Rz;   // peut être RT (0-255)

            // Détection automatique XInput trigger
            int effectiveThreshold = Threshold;

            // XInput triggers → 0 à 255
            if (AxisName == "Ry" || AxisName == "Rz")
            {
                if (value >= 0 && value <= 255)
                {
                    // On est sur un trigger XInput → seuil automatique beaucoup plus bas
                    effectiveThreshold = 30;
                }
            }

            // Détection automatique pour sticks (XInput ou DirectInput)
            else if (AxisName == "X" || AxisName == "Y" ||
                     AxisName == "Z" || AxisName == "Rx")
            {
                // Valeurs DirectInput, XInput stick → grande amplitude
                if (value >= -32768 && value <= 32767)
                {
                    effectiveThreshold = Threshold <= 255 ? 15000 : Threshold;
                }
            }

            // activation selon le seuil
            bool active = TriggerAbove ? (value > effectiveThreshold)
                                       : (value < effectiveThreshold);

            // edge-trigger : action déclenchée seulement lors d'une transition
            if (active && !wasActive)
            {
                wasActive = true;
                return true;
            }

            if (!active)
                wasActive = false;

            return false;
        }
    }
}
