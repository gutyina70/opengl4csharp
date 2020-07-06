using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using SDL2;

namespace OpenGL.Platform
{
    public static class Window
    {
        #region Properties
        /// <summary>
        /// Gets the current width of the SDL window in pixels.
        /// </summary>
        public static int Width { get; private set; }

        /// <summary>
        /// Gets the current height of the SDL window in pixels.
        /// </summary>
        public static int Height { get; private set; }

        private static bool fullscreen = false;
        /// <summary>
        /// Gets the current fullscreen state of the SDL window.
        /// </summary>
        public static bool Fullscreen
        {
            get { return fullscreen; }
            set
            {
                fullscreen = value;
                SDL.SDL_SetWindowFullscreen(window, Convert.ToUInt32(value));
            }
        }

        /// <summary>
        /// Gets the current vertical sync state of the SDL window.
        /// </summary>
        
        /// <summary>
        /// The main thread ID, which is the thread ID that the OpenGL context was created on.
        /// This is the thread ID that must be used for all future OpenGL calls.
        /// </summary>
        public static int MainThreadID { get; private set; }
        public static bool VerticalSync { get; private set; }

        public static bool Open = false;
        #endregion

        private static bool relativeMouseMode = false;

        public static bool RelativeMouseMode
        {
            get { return relativeMouseMode; }
            set
            {
                relativeMouseMode = value;
                SDL.SDL_SetRelativeMouseMode(value ? SDL.SDL_bool.SDL_TRUE : SDL.SDL_bool.SDL_FALSE);
            }
        }

        public static bool Maximized
        {
            get
            {
                return Convert.ToBoolean(SDL.SDL_GetWindowFlags(window) >> 7 & 1);
            }
            set
            {
                SDL.SDL_MaximizeWindow(window);
            }
        }

        #region Create SDL Window and OpenGL Context
        private static IntPtr window, glContext;
        public static IntPtr WindowID { get { return window; } }
        public static IntPtr GLContextID { get { return glContext; } }

        public enum ErrorCode
        {
            Success = 1,
            AlreadyInitialized,
            CouldNotCreateWindow,
            CouldNotCreateContext,
            WindowWasNotInitialized
        }

        /// <summary>
        /// Creates an OpenGL context and associated Window via the
        /// cross-platform SDL library.  Will clear the screen to black
        /// as quickly as possible by calling glClearColor and glClear.
        /// </summary>
        /// <param name="title"></param>
        public static ErrorCode CreateWindow(string title, int width, int height, bool fullscreen = false)
        {
            // check if a window already exists
            if (window != IntPtr.Zero || glContext != IntPtr.Zero)
            {
                return ErrorCode.AlreadyInitialized;
            }

            // initialize SDL and set a few defaults for the OpenGL context
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, 24);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_ALPHA_SIZE, 8);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_STENCIL_SIZE, 8);

            // capture the rendering thread ID
            MainThreadID = System.Threading.Thread.CurrentThread.ManagedThreadId;

            // create the window which should be able to have a valid OpenGL context and is resizable
            var flags = SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE;
            if (fullscreen) flags |= SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN;
            window = SDL.SDL_CreateWindow(title, SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED, width, height, flags);

            if (window == IntPtr.Zero)
            {
                return ErrorCode.CouldNotCreateWindow;
            }

            return CreateContextFromWindow(window, fullscreen);
        }

        /// <summary>
        /// Creates an OpenGL context in a valid SDL window.
        /// </summary>
        /// <param name="window">The valid SDL window.</param>
        /// <param name="fullscreen">True if the window is already in fullscreen mode.</param>
        public static ErrorCode CreateContextFromWindow(IntPtr window, bool fullscreen = false)
        {
            if (window == IntPtr.Zero)
            {
                return ErrorCode.WindowWasNotInitialized;
            }

            int width, height;
            SDL.SDL_GetWindowSize(window, out width, out height);

            Width = width;
            Height = height;
            Fullscreen = fullscreen;

            // create a valid OpenGL context within the newly created window
            glContext = SDL.SDL_GL_CreateContext(window);
            if (glContext == IntPtr.Zero)
            {
                return ErrorCode.CouldNotCreateContext;
            }

            // initialize the screen to black as soon as possible
            Gl.ClearColor(0f, 0f, 0f, 1f);
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            SwapBuffers();

            Open = true;

            return ErrorCode.Success;
        }
        #endregion

        #region Swap Buffers
        /// <summary>
        /// Swap the OpenGL buffer and bring the back buffer to the screen.
        /// </summary>
        public static void SwapBuffers()
        {
            SDL.SDL_GL_SwapWindow(window);
        }
        #endregion

        #region Apply Preferences
        public static void SetScreenMode(Compatibility.ScreenResolution screen, bool fullscreen)
        {
            if (fullscreen)
            {
                // we need to switch to windowed mode, then set size, and then fullscreen
                // simply setting the displaymode doesn't update the resolution until
                // the window loses focus and is then refocused
                SDL.SDL_SetWindowFullscreen(window, 0);
                SDL.SDL_SetWindowSize(window, screen.width, screen.height);

                SDL.SDL_SetWindowFullscreen(window, 1);
            }
            else
            {
                SDL.SDL_SetWindowFullscreen(window, 0);
                SDL.SDL_SetWindowSize(window, screen.width, screen.height);
                SDL.SDL_SetWindowPosition(window, SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED);
            }

            OnReshape(screen.width, screen.height);
            Fullscreen = fullscreen;
        }

        public static void ApplyVerticalSync(bool verticalSync)
        {
            // set the swap interval (v-sync)
            if (Compatibility.IsWindows())
            {
                IntPtr address = Gl.GetAddress("wglSwapIntervalEXT");
                if (address != IntPtr.Zero && address != (IntPtr)1 && address != (IntPtr)2)
                    NativeMethods.wglSwapInterval = Marshal.GetDelegateForFunctionPointer<NativeMethods.wglSwapIntervalEXT>(address);
                NativeMethods.wglSwapInterval?.Invoke(verticalSync ? 1 : 0);

                VerticalSync = verticalSync;
            }
            else VerticalSync = false;
        }
        #endregion

        #region Event Handling
        private static SDL.SDL_Event sdlEvent;

        public delegate void OnMouseWheelDelegate(uint wheel, int direction, int x, int y);

        public static OnMouseWheelDelegate OnMouseWheel { get; set; }

        private static byte[] mouseState = new byte[256];

        public static void HandleEvents()
        {
            while (SDL.SDL_PollEvent(out sdlEvent) != 0 && window != IntPtr.Zero)
            {
                switch (sdlEvent.type)
                {
                    case SDL.SDL_EventType.SDL_KEYDOWN:
                        Input.KeyDownInvoke(sdlEvent);
                        break;
                    case SDL.SDL_EventType.SDL_KEYUP:
                        Input.KeyUpInvoke(sdlEvent);
                        break;
                    case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                        Input.MouseDownInvoke(sdlEvent);
                        break;
                    case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                        Input.MouseUpInvoke(sdlEvent);
                        break;
                    case SDL.SDL_EventType.SDL_MOUSEMOTION:
                        if(relativeMouseMode)
                        {
                            Input.MouseMotionInvoke(sdlEvent);
                        }
                        else
                        {
                            Input.MouseMoveInvoke(sdlEvent);
                        }
                        break;
                    case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                        Input.MouseWheelInvoke(sdlEvent);
                        break;
                    case SDL.SDL_EventType.SDL_WINDOWEVENT:
                        switch (sdlEvent.window.windowEvent)
                        {
                            case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED:
                                OnReshape(sdlEvent.window.data1, sdlEvent.window.data2);
                                break;
                            case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE:
                                OnClose();
                                break;
                            case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MINIMIZED:
                                // stop rendering the scene
                                break;
                            case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESTORED:
                            case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_EXPOSED:
                                // stop rendering the scene
                                break;
                        }
                        break;
                }
            }
            Input.MouseRepeatInvoke();
            Input.KeyRepeatInvoke();
        }
        #endregion

        #region OnReshape and OnClose
        public static List<Action> OnReshapeCallbacks = new List<Action>();
        public static List<Action> OnCloseCallbacks = new List<Action>();

        public static void OnReshape(int width, int height)
        {
            // for whatever reason, SDL does not give accurate sizes in its event when windowed,
            // so we just need to query the window size when in windowed mode
            if (!Fullscreen)
                SDL.SDL_GetWindowSize(window, out width, out height);

            if (width % 2 == 1) width--;
            if (height % 2 == 1) height--;

            Width = width;
            Height = height;

            foreach (var callback in OnReshapeCallbacks) callback();
        }

        public static void OnClose()
        {
            foreach (var callback in OnCloseCallbacks) callback();

            SDL.SDL_GL_DeleteContext(glContext);
            SDL.SDL_DestroyWindow(window);
            SDL.SDL_Quit();

            Open = false;
        }
        #endregion

        #region Mouse Callbacks
        public static void ShowCursor(bool cursor)
        {
            SDL.SDL_ShowCursor(cursor ? 1 : 0);
        }

        public static void WarpPointer(int x, int y)
        {
            NativeMethods.CGSetLocalEventsDelegateOSIndependent(0.0);
            SDL.SDL_WarpMouseInWindow(window, x, y);
            NativeMethods.CGSetLocalEventsDelegateOSIndependent(0.25);
        }
        #endregion
    }
}
