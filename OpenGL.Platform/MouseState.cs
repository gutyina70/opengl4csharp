using System.Collections.Generic;

namespace OpenGL.Platform
{
    public static class MouseState
    {
        public static int X { get; internal set; }
        public static int Y { get; internal set; }
        public static Dictionary<MouseButton, bool> Buttons { get; internal set; }
        public static int Wheel { get; internal set; }

        static MouseState()
        {
            X = 0;
            Y = 0;
            Buttons = new Dictionary<MouseButton, bool>();
            Buttons.Add(MouseButton.Left, false);
            Buttons.Add(MouseButton.Middle, false);
            Buttons.Add(MouseButton.Right, false);
            Wheel = 0;
        }
    }
}
