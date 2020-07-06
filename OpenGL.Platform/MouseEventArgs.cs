using System;

namespace OpenGL.Platform
{
    public enum SpecialKey
    {
        Alt, Control, Shift
    };

    /// <summary>Enumeration holding the mouse button values.</summary>
    public enum MouseButton : int
    {
        /// <summary>The left mouse button is a valid chord modifier for input.</summary>
        Left = 1,
        /// <summary>The right mouse button is a valid chord modifier for input.</summary>
        Right = 3,
        /// <summary>The middle mouse button is a valid chord modifier for input.</summary>
        Middle = 2
    }
}
