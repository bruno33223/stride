// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Controls;
using Avalonia.Platform;

namespace Stride.Editor.Avalonia.Controls;

/// <summary>
/// Native viewport control hosting an X11 or OS window handle for rendering Stride via Vulkan SwapChain.
/// Creates a solid, opaque native child window in Linux X11 to eliminate compositing hole-punch transparency.
/// </summary>
public class StrideViewportControl : NativeControlHost
{
    private IntPtr x11Display = IntPtr.Zero;
    private IntPtr x11ChildWindow = IntPtr.Zero;

    /// <summary>
    /// Gets the native window handle for Stride Vulkan SwapChain coupling.
    /// </summary>
    public IntPtr NativeHandle { get; private set; }

    /// <summary>
    /// Gets the native display handle (X11 Display* on Linux).
    /// </summary>
    public IntPtr NativeDisplay => x11Display;

    /// <summary>
    /// Event raised when the native viewport handle is initialized.
    /// </summary>
    public event Action<IntPtr>? OnViewportInitialized;

    /// <summary>
    /// Event raised when the viewport control is resized.
    /// </summary>
    public event Action<int, int>? OnViewportResized;

    /// <summary>
    /// Idiomatic event alias for <see cref="OnViewportInitialized"/>.
    /// </summary>
    public event Action<IntPtr>? ViewportInitialized
    {
        add => OnViewportInitialized += value;
        remove => OnViewportInitialized -= value;
    }

    /// <summary>
    /// Idiomatic event alias for <see cref="OnViewportResized"/>.
    /// </summary>
    public event Action<int, int>? ViewportResized
    {
        add => OnViewportResized += value;
        remove => OnViewportResized -= value;
    }

    /// <inheritdoc />
    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        if (OperatingSystem.IsLinux() && parent != null && parent.Handle != IntPtr.Zero)
        {
            try
            {
                x11Display = X11Interop.OpenDisplay(IntPtr.Zero);
                if (x11Display != IntPtr.Zero)
                {
                    int screen = X11Interop.DefaultScreen(x11Display);
                    ulong black = X11Interop.BlackPixel(x11Display, screen);
                    uint initialWidth = Math.Max(32u, (uint)Math.Max(0, (int)Bounds.Width));
                    uint initialHeight = Math.Max(32u, (uint)Math.Max(0, (int)Bounds.Height));
                    if (initialWidth <= 32 || initialHeight <= 32)
                    {
                        initialWidth = 800;
                        initialHeight = 600;
                    }

                    // Create real opaque X11 child window attached to Avalonia's parent window handle
                    x11ChildWindow = X11Interop.CreateSimpleWindow(
                        x11Display,
                        parent.Handle,
                        0,
                        0,
                        initialWidth,
                        initialHeight,
                        0,
                        black,
                        black);

                    if (x11ChildWindow != IntPtr.Zero)
                    {
                        const long eventMask = (1L << 0)  // KeyPress
                                             | (1L << 1)  // KeyRelease
                                             | (1L << 2)  // ButtonPress
                                             | (1L << 3)  // ButtonRelease
                                             | (1L << 6)  // PointerMotion
                                             | (1L << 15) // Exposure
                                             | (1L << 17);// StructureNotify

                        X11Interop.SelectInput(x11Display, x11ChildWindow, eventMask);
                        X11Interop.MapWindow(x11Display, x11ChildWindow);
                        X11Interop.Flush(x11Display);

                        NativeHandle = x11ChildWindow;
                        var platformHandle = new PlatformHandle(x11ChildWindow, "XID");
                        OnViewportInitialized?.Invoke(NativeHandle);
                        return platformHandle;
                    }
                }
            }
            catch
            {
                // Fallback to base if direct X11 creation is unavailable
            }
        }

        // Generic fallback for other platforms
        var baseHandle = base.CreateNativeControlCore(parent);
        NativeHandle = baseHandle.Handle;
        OnViewportInitialized?.Invoke(NativeHandle);
        return baseHandle;
    }

    /// <inheritdoc />
    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        try
        {
            if (OperatingSystem.IsLinux() && x11Display != IntPtr.Zero)
            {
                if (x11ChildWindow != IntPtr.Zero)
                {
                    X11Interop.DestroyWindow(x11Display, x11ChildWindow);
                    x11ChildWindow = IntPtr.Zero;
                }
                X11Interop.CloseDisplay(x11Display);
                x11Display = IntPtr.Zero;
            }

            base.DestroyNativeControlCore(control);
        }
        catch
        {
            // Graceful cleanup
        }
        finally
        {
            NativeHandle = IntPtr.Zero;
        }
    }

    /// <inheritdoc />
    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        var width = (int)Math.Max(0, e.NewSize.Width);
        var height = (int)Math.Max(0, e.NewSize.Height);

        if (OperatingSystem.IsLinux() && x11Display != IntPtr.Zero && x11ChildWindow != IntPtr.Zero)
        {
            if (width > 0 && height > 0)
            {
                X11Interop.ResizeWindow(x11Display, x11ChildWindow, (uint)width, (uint)height);
                X11Interop.Flush(x11Display);
            }
        }

        OnViewportResized?.Invoke(width, height);
    }

    [SupportedOSPlatform("linux")]
    private static class X11Interop
    {
        private const string LibX11 = "libX11.so.6";

        [DllImport(LibX11, EntryPoint = "XOpenDisplay")]
        public static extern IntPtr OpenDisplay(IntPtr display);

        [DllImport(LibX11, EntryPoint = "XCloseDisplay")]
        public static extern int CloseDisplay(IntPtr display);

        [DllImport(LibX11, EntryPoint = "XDefaultScreen")]
        public static extern int DefaultScreen(IntPtr display);

        [DllImport(LibX11, EntryPoint = "XBlackPixel")]
        public static extern ulong BlackPixel(IntPtr display, int screenNumber);

        [DllImport(LibX11, EntryPoint = "XCreateSimpleWindow")]
        public static extern IntPtr CreateSimpleWindow(
            IntPtr display,
            IntPtr parent,
            int x,
            int y,
            uint width,
            uint height,
            uint borderWidth,
            ulong border,
            ulong background);

        [DllImport(LibX11, EntryPoint = "XMapWindow")]
        public static extern int MapWindow(IntPtr display, IntPtr window);

        [DllImport(LibX11, EntryPoint = "XDestroyWindow")]
        public static extern int DestroyWindow(IntPtr display, IntPtr window);

        [DllImport(LibX11, EntryPoint = "XResizeWindow")]
        public static extern int ResizeWindow(IntPtr display, IntPtr window, uint width, uint height);

        [DllImport(LibX11, EntryPoint = "XSelectInput")]
        public static extern int SelectInput(IntPtr display, IntPtr window, long eventMask);

        [DllImport(LibX11, EntryPoint = "XFlush")]
        public static extern int Flush(IntPtr display);
    }
}

