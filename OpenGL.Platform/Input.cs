using SDL2;
using System;
using System.Collections.Generic;

namespace OpenGL.Platform
{
    /// <summary>
    /// An Event stores information about how to call the contained
    /// kEvent delegate.  An Event is for both Mouse and Keyboard
    /// callback.  A mouse uses mEvent, a keyboard uses kEvent.
    /// </summary>
    public class Event
    {
        #region Properties
        /// <summary>The keyboard event delegate.</summary>
        public delegate void KeyEvent(char c, bool state);

        /// <summary>The keyboard event delegate.</summary>
        /// <param name="Time">Seconds since the last frame.</param>
        public delegate void RepeatEvent(float Time);

        /// <summary>A keyEvent delegate which can be called by a keyDown or keyUp event.</summary>
        public KeyEvent Call { get; private set; }

        /// <summary>A repeatEvent delegate which can be called by UpdateKeys(float Time).</summary>
        public RepeatEvent Repeat { get; private set; }
        #endregion

        #region Methods
        /// <summary>Creates a KeyEvent delegate from an Action or lambda expression.</summary>
        /// <param name="Event">The Event to call on a key down event.</param>
        public Event(Action Event)
        {
            this.Call = (key, state) =>
            {
                if (state) Event();
            };
        }

        /// <summary>Standard constructor for a keyboard event.</summary>
        /// <param name="Event">The Event to call on a keyDown event.</param>
        public Event(KeyEvent Event)
        {
            this.Call = Event;
        }

        /// <summary>Standard constructor for a mouse mouse event.</summary>
        /// <param name="Event">The Event to call every frame update.</param>
        public Event(RepeatEvent Event)
        {
            this.Repeat = Event;
        }
        #endregion
    }

    public static class Input
    {
        #region Constructor
        /// <summary>Constructor of Input for first time instantiation</summary>
        static Input()
        {
            keys = new List<char>();
            subqueue = new Stack<Event[]>();
            subqueue.Push(new Event[256]);

            // since SDL Keycodes can be 40 or 1073741903
            // map every SDL Keycode to a char starting from 0, incremented by 1
            // for converting to char and easy array indexing
            sdlKeyMap = new Dictionary<SDL.SDL_Keycode, char>();
            char i = (char)0;
            foreach(SDL.SDL_Keycode keyCode in Enum.GetValues(typeof(SDL.SDL_Keycode)))
            {
                sdlKeyMap[keyCode] = i++;
            }

            keysRaw = new List<char>();
            subqueueRaw = new Stack<Event[]>();
            subqueueRaw.Push(new Event[sdlKeyMap.Count]);
        }
        #endregion

        #region Variables
        public static List<char> keys;                                 // a list of keys that are down
        public static List<char> keysRaw;                              // a list of raw keys that are down
        private static Stack<Event[]> subqueue;                        // a stack of events, the topmost being the current key bindings
        private static Stack<Event[]> subqueueRaw;                     // a stack of events, the topmost being the current raw key bindings
        public static Dictionary<SDL.SDL_Keycode, char> sdlKeyMap;     // SDL Keyscodes mapped to a char
        #endregion

        #region Events
        public static event Action<MouseButton, int, int> MouseDown;
        public static event Action<MouseButton, int, int> MouseUp;
        public static event Action<int, int> MouseMove;
        public static event Action<int, int> MouseMotion;
        public static event Action<int> MouseWheel;
        public static event Action<StateDictionary<MouseButton>, int, int> MouseRepeat;

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
            MouseRepeat?.Invoke(InputState.MouseButtons, InputState.MouseX, InputState.MouseY);
        }

        //internal static void MouseRepeatInvoke(SDL.SDL_Event e)
        //{
        //    MouseMotion?.Invoke(new MouseEventArgs(e.motion.x, e.motion.y, (MouseButton)e.button.button, Convert.ToBoolean(e.button.state)));
        //}
        #endregion

        #region Properties
        /// <summary>
        /// The active key bindings (on the topmost of the keybinding stack).
        /// </summary>
        public static Event[] KeyBindings
        {
            get { lock (subqueue) return subqueue.Peek(); }
        }

        /// <summary>
        /// The active key raw bindings (on the topmost of the keybinding stack).
        /// </summary>
        public static Event[] KeyBindingsRaw
        {
            get { lock(subqueueRaw) return subqueueRaw.Peek(); }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Adds a key that has been pressed to the list of keys that are currently held down.
        /// </summary>
        /// <param name="key">The key that was pressed.</param>
        /// <param name="shift">True if the shift key is currently pressed.</param>
        /// <param name="ctrl">True if the control key is currently pressed.</param>
        /// <param name="alt">True if the alt key is currently pressed.</param>
        public static void AddKey(char key, bool shift, bool ctrl, bool alt)
        {
            char mkey = (char)(key & 0x3f);             // snap the key under 64 so we don't accidentally trigger an alt/ctrl event
            if (key > 63)                               // if >63 then it is a letter, apply ctrl+alt+shift
            {
                mkey = (char)((mkey & 0x1f) |           // first keep only the information relevant to the character
                    ((shift ? 0x00 : 0x20) |            // apply uppercase modifier
                    (alt ? 0x80 : 0x00) |               // apply alt modifier
                    (ctrl ? 0xc0 : 0x00) |              // apply control modifier
                    ((!ctrl && !alt) ? 0x40 : 0x00)));  // apply lowercase modifier (if it was nothing else)
            }
            else if (shift)
            {
                switch (key)
                {
                    case '`': mkey = '~'; break;
                    case '1': mkey = '!'; break;
                    case '2': mkey = '@'; break;
                    case '3': mkey = '#'; break;
                    case '4': mkey = '$'; break;
                    case '5': mkey = '%'; break;
                    case '6': mkey = '^'; break;
                    case '7': mkey = '&'; break;
                    case '8': mkey = '*'; break;
                    case '9': mkey = '('; break;
                    case '0': mkey = ')'; break;
                    case '-': mkey = '_'; break;
                    case '=': mkey = '+'; break;
                    case '[': mkey = '{'; break;
                    case ']': mkey = '}'; break;
                    case '\\': mkey = '|'; break;
                    case ';': mkey = ':'; break;
                    case '\'': mkey = '"'; break;
                    case ',': mkey = '<'; break;
                    case '.': mkey = '>'; break;
                    case '/': mkey = '?'; break;
                }
            }
            if (KeyBindings[mkey] != null && KeyBindings[mkey].Call != null)    // keybindings performs a lock of subqueue, ensuring thread safety
            {
                KeyBindings[mkey].Call(mkey, true);     // event is called immediately
            }
            lock (keys) if (!keys.Contains(mkey)) keys.Add(mkey);
        }

        /// <summary>
        /// Adds a raw key that has been pressed to the list of keys that are currently held down.
        /// </summary>
        /// <param name="key">The key that was pressed.</param>
        public static void AddKeyRaw(SDL.SDL_Keycode key)
        {
            char keyRaw = sdlKeyMap[key];
            if(KeyBindingsRaw[keyRaw] != null && KeyBindingsRaw[keyRaw].Call != null)    // keybindings performs a lock of subqueue, ensuring thread safety
            {
                KeyBindingsRaw[keyRaw].Call(keyRaw, true);     // event is called immediately
            }
            lock(keysRaw)
                if(!keysRaw.Contains(keyRaw))
                    keysRaw.Add(keyRaw);
        }

        /// <summary>
        /// Removes a key if a keyup event has been fired.  Stops repeatable events.
        /// </summary>
        /// <param name="key">The key is no longer being pressed.</param>
        public static void RemoveKey(char key)
        {
            // call a keyup if a key event is registered
            if (KeyBindings[key] != null && KeyBindings[key].Call != null) KeyBindings[key].Call(key, false);

            // For clarity, I've unwrapped the loop that was originally here, since this is important
            // Sometimes the user will release ctrl/shift/alt while still holding the key down
            // so it's important to check all of these combinations on a keyup event.
            if (key < 63 && keys.Contains(key)) keys.Remove(key);
            else
            {
                key = (char)(key & 0x1f);
                // Remove upper-key
                if (keys.Contains((char)(key | 0x40))) keys.Remove((char)(key | 0x40));
                // Remove lower-key
                if (keys.Contains((char)(key | 0x60))) keys.Remove((char)(key | 0x60));
                // Remove alt-upper-key
                if (keys.Contains((char)(key | 0x80))) keys.Remove((char)(key | 0x80));
                // Remove alt-lower-key
                if (keys.Contains((char)(key | 0xa0))) keys.Remove((char)(key | 0xa0));
                // Remove ctl-upper-key
                if (keys.Contains((char)(key | 0xc0))) keys.Remove((char)(key | 0xc0));
                // Remove ctl-lower-key
                if (keys.Contains((char)(key | 0xe0))) keys.Remove((char)(key | 0xe0));
            }
        }

        /// <summary>
        /// Removes a raw key if a keyup event has been fired.  Stops repeatable events.
        /// </summary>
        /// <param name="key">The key is no longer being pressed.</param>
        public static void RemoveKeyRaw(SDL.SDL_Keycode key)
        {
            char keyRaw = sdlKeyMap[key];
            // call a keyup if a key event is registered
            if(KeyBindingsRaw[keyRaw] != null && KeyBindingsRaw[keyRaw].Call != null)
                KeyBindingsRaw[keyRaw].Call(keyRaw, false);
            keysRaw.Remove(keyRaw);
        }

        /// <summary>
        /// Determines whether a key is pressed or not.
        /// </summary>
        /// <param name="key"></param>
        public static bool IsKeyDown(char key)
        {
            return keys.Contains(key);
        }

        /// <summary>
        /// Determines whether a raw key is pressed or not.
        /// </summary>
        /// <param name="key"></param>
        public static bool IsKeyDownRaw(SDL.SDL_Keycode key)
        {
            return keysRaw.Contains(sdlKeyMap[key]);
        }

        /// <summary>
        /// Push a new key binding set onto the stack.
        /// </summary>
        public static void PushKeyBindings()
        {
            lock (subqueue)
            {
                subqueue.Push(new Event[256]);
            }
        }

        /// <summary>
        /// Push a new raw key binding set onto the stack.
        /// </summary>
        public static void PushKeyBindingsRaw()
        {
            lock(subqueueRaw)
            {
                subqueueRaw.Push(new Event[256]);
            }
        }

        /// <summary>
        /// Pop a key binding set off the stack (restoring the buried key bindings).
        /// </summary>
        public static void PopKeyBindings()
        {
            lock (subqueue)
            {
                if (subqueue.Count > 1) subqueue.Pop();
                else throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Pop a raw key binding set off the stack (restoring the buried key bindings).
        /// </summary>
        public static void PopKeyBindingsRaw()
        {
            lock(subqueueRaw)
            {
                if(subqueueRaw.Count > 1)
                    subqueueRaw.Pop();
                else
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Subscribe an event to a keystroke (specified by the char occupying that key).
        /// </summary>
        public static void Subscribe(char Key, Event Event)
        {
            KeyBindings[Key] = Event;
        }

        /// <summary>
        /// Subscribe an event to an sdl keycode (specified by the char occupying that key).
        /// </summary>
        public static void SubscribeRaw(SDL.SDL_Keycode Key, Event Event)
        {
            char keyRaw = sdlKeyMap[Key];
            KeyBindingsRaw[keyRaw] = Event;
        }

        /// <summary>
        /// Subscribe an action to a keystroke (specified by the char occupying that key).
        /// </summary>
        public static void Subscribe(char Key, Action Event)
        {
            KeyBindings[Key] = new Event(Event);
        }

        /// <summary>
        /// Subscribe an action to an sdl keycode (specified by the char occupying that key).
        /// </summary>
        public static void SubscribeRaw(SDL.SDL_Keycode Key, Action Event)
        {
            char normalizedKey = sdlKeyMap[Key];
            KeyBindingsRaw[normalizedKey] = new Event(Event);
        }

        /// <summary>
        /// Subscribes one event to all keys on the keyboard (except special keys such as escape).
        /// </summary>
        public static void SubscribeAll(Event Event)
        {
            for (int i = 32; i < 127; i++)
                Subscribe((char)i, Event);
        }

        /// <summary>
        /// Subscribes one event to all keys on the keyboard (including special keys such as escape and modifier keys).
        /// </summary>
        public static void SubscribeAllRaw(Event Event)
        {
            foreach(SDL.SDL_Keycode key in sdlKeyMap.Keys)
                SubscribeRaw(key, Event);
        }

        /// <summary>
        /// Subscribes an event to a keystroke + special key.
        /// </summary>
        /// <remarks>The special key must be helt down prior to the keystroke to fire one of these events.</remarks>
        public static void SubscribeChord(char Key, SpecialKey Special, Event Event)
        {
            Key = (char)(Key & 0x7f);
            // Alt operates over the range from 0x80-0x9f
            if (Special == SpecialKey.Alt) Key = (char)(Key & 0x3f | 0x80);
            // Control operates over the range from 0xc0-0xff
            else if (Special == SpecialKey.Control) Key = (char)(Key | 0xc0);

            Subscribe(Key, Event);
        }

        /// <summary>
        /// Subscribes an action to a keystroke + special key.
        /// </summary>
        /// <remarks>The special key must be helt down prior to the keystroke to fire one of these events.</remarks>
        public static void SubscribeChord(char Key, SpecialKey Special, Action Event)
        {
            SubscribeChord(Key, Special, new Event(Event));
        }

        /// <summary>
        /// Updates all of the key events and raw key events that are repeatable.
        /// </summary>
        /// <param name="Time">The time since the last UpdateKeys call.</param>
        public static void Update()
        {
            // Update all of the event which are repeatable
            lock (keys)
            {
                for (int i = 0; i < keys.Count; i++)
                    if (KeyBindings[keys[i]] != null && KeyBindings[keys[i]].Repeat != null)
                        KeyBindings[keys[i]].Repeat(Time.DeltaTime);
            }

            lock(keysRaw)
            {
                for(int i = 0; i < keysRaw.Count; i++)
                    if(KeyBindingsRaw[keysRaw[i]] != null && KeyBindingsRaw[keysRaw[i]].Repeat != null)
                        KeyBindingsRaw[keysRaw[i]].Repeat(Time.DeltaTime);
            }

            MouseRepeatInvoke();
        }
        #endregion
    }
}
