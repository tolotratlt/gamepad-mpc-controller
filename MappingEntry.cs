using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamepadMpcController
{
    public class MappingEntry
    {
        public string ActionName { get; set; }
        public MappingRule Rule { get; set; }
        public AxisRule AxisRule { get; set; }
        public POVRule PovRule { get; set; }

        // Utilisé par la colonne ButtonIndex
        public int ButtonIndex
        {
            get { return Rule != null ? Rule.ButtonIndex : -1; }
            set { if (Rule != null) Rule.ButtonIndex = value; }
        }

        // Utilisé par la colonne Axe
        public string AxisName
        {
            get { return AxisRule != null ? AxisRule.AxisName : ""; }
            set { if (AxisRule != null) AxisRule.AxisName = value; }
        }

        public string POVDisplay
        {
            get
            {
                if (PovRule == null) return "";
                return PovRule.Direction.ToString();
            }
            set
            {
                if (PovRule != null && int.TryParse(value, out int v))
                    PovRule.Direction = v;
            }
        }
    }
}
