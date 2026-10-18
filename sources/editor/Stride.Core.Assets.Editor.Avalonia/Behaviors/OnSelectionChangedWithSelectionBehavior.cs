// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Xaml.Interactivity;

namespace Stride.Core.Assets.Editor.Avalonia.Behaviors
{
    public class OnSelectionChangedWithSelectionBehavior : Behavior<SelectingItemsControl>
    {
        public static readonly StyledProperty<ICommand?> CommandProperty =
            AvaloniaProperty.Register<OnSelectionChangedWithSelectionBehavior, ICommand?>(nameof(Command));

        public static readonly StyledProperty<object?> CommandParameterProperty =
            AvaloniaProperty.Register<OnSelectionChangedWithSelectionBehavior, object?>(nameof(CommandParameter));

        public ICommand? Command
        {
            get => GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.SelectionChanged += OnSelectionChanged;
            }
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.SelectionChanged -= OnSelectionChanged;
            }
            base.OnDetaching();
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            OnEvent();
        }

        public virtual void OnEvent()
        {
            if (AssociatedObject?.SelectedItem != null)
            {
                var cmd = Command;
                var param = CommandParameter;
                if (cmd != null && cmd.CanExecute(param))
                {
                    cmd.Execute(param);
                }
            }
        }
    }
}
