// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Behaviors
{
    public class PropertyViewFilteringBehavior : Behavior<Control>
    {
        public static readonly StyledProperty<string?> FilterTokenProperty =
            AvaloniaProperty.Register<PropertyViewFilteringBehavior, string?>(nameof(FilterToken));

        public static readonly StyledProperty<Predicate<object>?> FilterPredicateProperty =
            AvaloniaProperty.Register<PropertyViewFilteringBehavior, Predicate<object>?>(nameof(FilterPredicate));

        static PropertyViewFilteringBehavior()
        {
            FilterTokenProperty.Changed.AddClassHandler<PropertyViewFilteringBehavior, string?>((b, e) =>
                b.OnFilterTokenChanged(e.NewValue.GetValueOrDefault()));
        }

        public string? FilterToken
        {
            get => GetValue(FilterTokenProperty);
            set => SetValue(FilterTokenProperty, value);
        }

        public Predicate<object>? FilterPredicate
        {
            get => GetValue(FilterPredicateProperty);
            set => SetValue(FilterPredicateProperty, value);
        }

        private void OnFilterTokenChanged(string? token)
        {
            if (!string.IsNullOrWhiteSpace(token))
                FilterPredicate = x => x is NodeViewModel node && Match(node, token);
            else
                FilterPredicate = null;
        }

        public static bool Match(NodeViewModel node, string token)
        {
            return node.DisplayName.IndexOf(token, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                   node.Children.Any(x => Match(x, token));
        }
    }
}
