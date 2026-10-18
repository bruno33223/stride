// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Stride.Core.Assets.Editor.Avalonia.Controls
{
    /// <summary>
    /// Stride generic Avalonia data grid. Left extensible for future development on a generic datagrid.
    /// </summary>
    public class DataGridEx : DataGrid
    {
        /// <summary>
        /// Identifies the <see cref="CanUserSearch"/> styled property.
        /// </summary>
        public static readonly StyledProperty<bool> CanUserSearchProperty =
            AvaloniaProperty.Register<DataGridEx, bool>(nameof(CanUserSearch), defaultValue: true);

        /// <summary>
        /// Identifies the <see cref="SearchText"/> styled property.
        /// </summary>
        public static readonly StyledProperty<string?> SearchTextProperty =
            AvaloniaProperty.Register<DataGridEx, string?>(nameof(SearchText), defaultValue: null);

        /// <summary>
        /// Gets or sets whether text search navigation is enabled.
        /// </summary>
        public bool CanUserSearch
        {
            get => GetValue(CanUserSearchProperty);
            set => SetValue(CanUserSearchProperty, value);
        }

        /// <summary>
        /// Gets or sets the search text used for prefix matching.
        /// </summary>
        public string? SearchText
        {
            get => GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataGridEx"/> class.
        /// </summary>
        public DataGridEx()
        {
        }

        /// <summary>
        /// Attempts to select the first item matching the given text prefix.
        /// </summary>
        /// <param name="prefix">The prefix to search for.</param>
        /// <returns>True if a match was found and selected; otherwise false.</returns>
        public bool SelectItemByPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix) || ItemsSource == null)
                return false;

            int index = 0;
            foreach (var item in ItemsSource)
            {
                if (item != null && item.ToString()?.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase) == true)
                {
                    SelectedIndex = index;
                    ScrollIntoView(item, null);
                    return true;
                }
                index++;
            }
            return false;
        }

        /// <inheritdoc/>
        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);

            if (!CanUserSearch || string.IsNullOrEmpty(e.Text))
                return;

            var wrapper = TextSearchWrapper.EnsureInstance(this);
            wrapper.AddCharToPrefix(e.Text);
        }
    }

    /// <summary>
    /// Helper class providing keyboard text navigation and search capabilities across ItemsControl items.
    /// </summary>
    internal sealed class TextSearchWrapper
    {
        private static readonly Dictionary<Control, TextSearchWrapper> Instances = new();

        private readonly Control control;
        private readonly List<string> charsEntered = new();
        private DateTime lastInputTime = DateTime.MinValue;
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1.0);

        public bool IsActive => charsEntered.Count > 0 && (DateTime.UtcNow - lastInputTime) < Timeout;

        public int MatchedItemIndex { get; set; } = -1;

        public string Prefix => string.Concat(charsEntered);

        public List<string> CharsEntered => charsEntered;

        private TextSearchWrapper(Control control)
        {
            this.control = control;
        }

        public static TextSearchWrapper EnsureInstance(Control control)
        {
            if (!Instances.TryGetValue(control, out var wrapper))
            {
                wrapper = new TextSearchWrapper(control);
                Instances[control] = wrapper;
            }
            return wrapper;
        }

        public static CultureInfo GetCulture(AvaloniaObject element)
        {
            return CultureInfo.CurrentCulture;
        }

        public void AddCharToPrefix(string nextChar)
        {
            if (DateTime.UtcNow - lastInputTime > Timeout)
            {
                charsEntered.Clear();
            }

            charsEntered.Add(nextChar);
            lastInputTime = DateTime.UtcNow;

            if (control is DataGridEx grid)
            {
                grid.SelectItemByPrefix(Prefix);
            }
        }

        public void ResetTimeout()
        {
            charsEntered.Clear();
            lastInputTime = DateTime.MinValue;
        }
    }
}
