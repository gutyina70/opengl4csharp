using SDL2;
using System.Collections.Generic;

namespace OpenGL.Platform
{
    public class StateDictionary<T>
    {
        private Dictionary<T, bool> storage;

        public StateDictionary()
        {
            storage = new Dictionary<T, bool>();
        }

        public bool this[T key]
        {
            get
            {
                if(!storage.ContainsKey(key))
                {
                    storage.Add(key, false);
                }

                return storage[key];
            }
            set
            {
                storage[key] = value;
            }
        }
    }
    public static class InputState
    {
        public static int MouseX { get; internal set; }
        public static int MouseY { get; internal set; }
        public static int MouseWheel { get; internal set; }
        public static StateDictionary<MouseButton> MouseButtons { get; }
        public static StateDictionary<SDL.SDL_Keycode> Keys { get; }

        static InputState()
        {
            MouseX = 0;
            MouseY = 0;
            MouseWheel = 0;
            MouseButtons = new StateDictionary<MouseButton>();
            Keys = new StateDictionary<SDL.SDL_Keycode>();
        }
    }
}
