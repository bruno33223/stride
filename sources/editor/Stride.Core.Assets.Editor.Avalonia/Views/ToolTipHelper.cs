// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Components.Status;

namespace Stride.Core.Assets.Editor.Avalonia.Views
{
    /// <summary>
    /// Helper class providing attached status bar synchronization properties for tooltips on Avalonia controls.
    /// </summary>
    public static class ToolTipHelper
    {
        /// <summary>
        /// Identifies the <see cref="Status"/> attached property.
        /// </summary>
        public static readonly AttachedProperty<StatusViewModel?> StatusProperty =
            AvaloniaProperty.RegisterAttached<Control, StatusViewModel?>("Status", typeof(ToolTipHelper), defaultValue: null);

        /// <summary>
        /// Identifies the <see cref="StatusToken"/> attached property.
        /// </summary>
        public static readonly AttachedProperty<int> StatusTokenProperty =
            AvaloniaProperty.RegisterAttached<Control, int>("StatusToken", typeof(ToolTipHelper), defaultValue: -1);

        static ToolTipHelper()
        {
            StatusProperty.Changed.AddClassHandler<Control>((control, args) => OnStatusChanged(control, args));
        }

        /// <summary>
        /// Gets the <see cref="StatusViewModel"/> attached to the specified control.
        /// </summary>
        public static StatusViewModel? GetStatus(Control element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return element.GetValue(StatusProperty);
        }

        /// <summary>
        /// Sets the <see cref="StatusViewModel"/> attached to the specified control.
        /// </summary>
        public static void SetStatus(Control element, StatusViewModel? value)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            element.SetValue(StatusProperty, value);
        }

        /// <summary>
        /// Gets the current status token associated with the control.
        /// </summary>
        public static int GetStatusToken(Control element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return element.GetValue(StatusTokenProperty);
        }

        /// <summary>
        /// Sets the current status token associated with the control.
        /// </summary>
        public static void SetStatusToken(Control element, int value)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            element.SetValue(StatusTokenProperty, value);
        }

        private static void OnStatusChanged(Control element, AvaloniaPropertyChangedEventArgs args)
        {
            if (element == null)
                return;

            element.PropertyChanged -= ElementOnPropertyChanged;
            element.PropertyChanged += ElementOnPropertyChanged;
        }

        private static void ElementOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (sender is not Control element)
                return;

            if (e.Property == ToolTip.IsOpenProperty)
            {
                if (ToolTip.GetIsOpen(element))
                {
                    PushToolTipStatus(element);
                }
                else
                {
                    PopToolTipStatus(element);
                }
            }
        }

        /// <summary>
        /// Pushes the tooltip text of the specified element to its attached status view model.
        /// </summary>
        public static void PushToolTipStatus(Control element)
        {
            if (element == null)
                return;

            var status = GetStatus(element);
            var tip = ToolTip.GetTip(element);
            var text = GetTooltipText(tip);
            if (status == null || string.IsNullOrEmpty(text))
                return;

            var token = GetStatusToken(element);
            if (token >= 0)
            {
                status.PopStatus(token);
            }

            token = status.PushStatus(text);
            SetStatusToken(element, token);
        }

        /// <summary>
        /// Pops any active status associated with the tooltip of the specified element.
        /// </summary>
        public static void PopToolTipStatus(Control element)
        {
            if (element == null)
                return;

            var status = GetStatus(element);
            var token = GetStatusToken(element);
            if (status != null && token >= 0)
            {
                status.PopStatus(token);
                SetStatusToken(element, -1);
            }
        }

        /// <summary>
        /// Extracts tooltip text representation from an object or visual.
        /// </summary>
        public static string? GetTooltipText(object? obj)
        {
            if (obj is string str)
                return str;

            if (obj is TextBlock txt)
                return txt.Text;

            if (obj is Visual visual)
            {
                var descendantText = visual.FindDescendantOfType<TextBlock>();
                if (descendantText != null)
                    return descendantText.Text;
            }

            return obj?.ToString();
        }
    }
}
