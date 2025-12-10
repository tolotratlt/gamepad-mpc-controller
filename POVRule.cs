using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamepadMpcController
{
    public class POVRule
    {
        public string Name;
        public int Direction;
        // 0 = haut, 9000 = droite, 18000 = bas, 27000 = gauche

        public bool IsActivated(GamepadState s)
        {
            if (s.POV == -1)
                return false;

            return s.POV == Direction;
        }
    }
}

