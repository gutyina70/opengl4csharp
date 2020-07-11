using SDL2;
using System;

namespace OpenGL.Platform
{
    public static class Input
    {
        public delegate void MouseDownEventHandler(MouseButton button, int x, int y);
        public delegate void MouseUpEventHandler(MouseButton button, int x, int y);
        public delegate void MouseMoveEventHandler(int x, int y);
        public delegate void MouseMotionEventHandler(int dx, int dy);
        public delegate void MouseWheelEventHandler(int scroll);
        public delegate void MouseRepeatEventHandler(float time, StateDictionary<MouseButton> buttons, int x, int y);

        public delegate void KeyDownEventHandler(SDL.SDL_Scancode key);
        public delegate void KeyUpEventHandler(SDL.SDL_Scancode key);
        public delegate void KeyRepeatEventHandler(float time, StateDictionary<SDL.SDL_Scancode> keys);

        #region Events
        public static event MouseDownEventHandler MouseDown;
        public static event MouseUpEventHandler MouseUp;
        public static event MouseMoveEventHandler MouseMove;
        public static event MouseMotionEventHandler MouseMotion;
        public static event MouseWheelEventHandler MouseWheel;
        public static event MouseRepeatEventHandler MouseRepeat;

        public static event KeyDownEventHandler KeyDown;
        public static event KeyUpEventHandler KeyUp;
        public static event KeyRepeatEventHandler KeyRepeat;
        #endregion

        #region Methods
        internal static void MouseDownInvoke(SDL.SDL_Event e)
        {
            MouseButton button = (MouseButton)e.button.button;
            InputState.MouseButtons[button] = true;
            MouseDown?.Invoke(button, e.button.x, e.button.y);
        }

        internal static void MouseUpInvoke(SDL.SDL_Event e)
        {
            MouseButton button = (MouseButton)e.button.button;
            InputState.MouseButtons[button] = false;
            MouseUp?.Invoke(button, e.button.x, e.button.y);
        }

        internal static void MouseMoveInvoke(SDL.SDL_Event e)
        {
            int x = e.motion.x;
            int y = e.motion.y;
            InputState.MouseX = x;
            InputState.MouseY = y;
            MouseMove?.Invoke(x, y);
        }

        internal static void MouseMotionInvoke(SDL.SDL_Event e)
        {
            int x = e.motion.xrel;
            int y = e.motion.yrel;
            InputState.MouseX += x;
            InputState.MouseY += y;
            MouseMotion?.Invoke(x, y);
        }

        internal static void MouseWheelInvoke(SDL.SDL_Event e)
        {
            int wheel = e.wheel.y;
            InputState.MouseWheel += wheel;
            MouseWheel?.Invoke(wheel);
        }

        internal static void MouseRepeatInvoke()
        {
            MouseRepeat?.Invoke(Time.DeltaTime, InputState.MouseButtons, InputState.MouseX, InputState.MouseY);
        }


        internal static void KeyDownInvoke(SDL.SDL_Event e)
        {
            SDL.SDL_Scancode key = e.key.keysym.scancode;
            InputState.Keys[key] = true;
            KeyDown?.Invoke(key);
        }

        internal static void KeyUpInvoke(SDL.SDL_Event e)
        {
            SDL.SDL_Scancode key = e.key.keysym.scancode;
            InputState.Keys[key] = false;
            KeyUp?.Invoke(key);
        }

        internal static void KeyRepeatInvoke()
        {
            KeyRepeat?.Invoke(Time.DeltaTime, InputState.Keys);
        }
        #endregion
    }
}
