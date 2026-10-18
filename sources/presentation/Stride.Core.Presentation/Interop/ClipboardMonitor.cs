// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Core.Presentation.Interop
{
    /// <summary>
    /// Enables registration of listeners to clipboard change events across platforms.
    /// </summary>
    public static class ClipboardMonitor
    {
        /// <summary>
        /// Raised when the clipboard content has changed.
        /// </summary>
        public static event EventHandler<EventArgs>? ClipboardTextChanged;

        /// <summary>
        /// Manually triggers the <see cref="ClipboardTextChanged"/> event.
        /// </summary>
        public static void NotifyClipboardTextChanged(object? sender = null)
        {
            ClipboardTextChanged?.Invoke(sender, EventArgs.Empty);
        }
    }
}
