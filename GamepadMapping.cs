using System.Collections.Generic;

namespace GamepadMpcController
{
    public class GamepadMapping
    {
        public MappingRule PlayPause;
        public MappingRule SeekForward;
        public MappingRule SeekBackward;
        public MappingRule Fullscreen;
        public MappingRule Next;
        public MappingRule Previous;
        public MappingRule VolumeUp;
        public MappingRule VolumeDown;
        public AxisRule SeekForwardAxis;
        public AxisRule SeekBackwardAxis;

        public POVRule VolumeUpPOV;
        public POVRule VolumeDownPOV;
        public POVRule SeekForwardPOV;
        public POVRule SeekBackwardPOV;

        public MappingRule StopAndMinimize;

        public static GamepadMapping CreateDefault()
        {
            return new GamepadMapping
            {
                PlayPause = new MappingRule { Name = "Play Pause", ButtonIndex = 0 },
                Next = new MappingRule { Name = "Next", ButtonIndex = 1 },
                Previous = new MappingRule { Name = "Previous", ButtonIndex = 2 },
                VolumeUp = new MappingRule { Name = "Volume Up", ButtonIndex = 3 },
                VolumeDown = new MappingRule { Name = "Volume Down", ButtonIndex = 4 },
                SeekForward = new MappingRule { Name = "Seek Forward", ButtonIndex = 5 },
                SeekBackward = new MappingRule { Name = "Seek Backward", ButtonIndex = 6 },
                Fullscreen = new MappingRule { Name = "Fullscreen", ButtonIndex = 7 },
                SeekForwardAxis = new AxisRule
                {
                    Name = "SeekForward Axis",
                    AxisName = "X",
                    Threshold = 48000,
                    TriggerAbove = true
                },

                SeekBackwardAxis = new AxisRule
                {
                    Name = "SeekBackward Axis",
                    AxisName = "X",
                    Threshold = 18000,
                    TriggerAbove = false
                },
                VolumeUpPOV = new POVRule
                {
                    Name = "Volume Up (POV)",
                    Direction = 0
                },

                VolumeDownPOV = new POVRule
                {
                    Name = "Volume Down (POV)",
                    Direction = 18000
                },

                SeekForwardPOV = new POVRule
                {
                    Name = "Seek Forward (POV)",
                    Direction = 9000
                },

                SeekBackwardPOV = new POVRule
                {
                    Name = "Seek Backward (POV)",
                    Direction = 27000
                },
                StopAndMinimize = new MappingRule { Name = "Stop and minimize", ButtonIndex = -1 },
            };
        }

        public IEnumerable<MappingEntry> CreateEntries()
        {
            yield return new MappingEntry { ActionName = PlayPause.Name, Rule = PlayPause };
            yield return new MappingEntry { ActionName = Next.Name, Rule = Next };
            yield return new MappingEntry { ActionName = Previous.Name, Rule = Previous };
            yield return new MappingEntry { ActionName = VolumeUp.Name, Rule = VolumeUp };
            yield return new MappingEntry { ActionName = VolumeDown.Name, Rule = VolumeDown };
            yield return new MappingEntry { ActionName = SeekForward.Name, Rule = SeekForward };
            yield return new MappingEntry { ActionName = SeekBackward.Name, Rule = SeekBackward };
            yield return new MappingEntry { ActionName = Fullscreen.Name, Rule = Fullscreen };
            yield return new MappingEntry { ActionName = SeekForwardAxis.Name, AxisRule = SeekForwardAxis };
            yield return new MappingEntry { ActionName = SeekBackwardAxis.Name, AxisRule = SeekBackwardAxis };

            yield return new MappingEntry { ActionName = VolumeUpPOV.Name, PovRule = VolumeUpPOV };
            yield return new MappingEntry { ActionName = VolumeDownPOV.Name, PovRule = VolumeDownPOV };
            yield return new MappingEntry { ActionName = SeekForwardPOV.Name, PovRule = SeekForwardPOV };
            yield return new MappingEntry { ActionName = SeekBackwardPOV.Name, PovRule = SeekBackwardPOV };

            yield return new MappingEntry { ActionName = StopAndMinimize.Name, Rule = StopAndMinimize };

        }
    }

    public class MappingRule
    {
        public string Name;
        public int ButtonIndex;

        public bool IsPressed(GamepadState state)
        {
            if (state == null || state.Buttons == null)
                return false;

            if (ButtonIndex < 0 || ButtonIndex >= state.Buttons.Length)
                return false;

            return state.Buttons[ButtonIndex];
        }
    }
}
