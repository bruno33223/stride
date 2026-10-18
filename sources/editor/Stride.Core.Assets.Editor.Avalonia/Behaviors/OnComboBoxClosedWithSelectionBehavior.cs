// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace Stride.Core.Assets.Editor.Avalonia.Behaviors
{
    public class OnComboBoxClosedWithSelectionBehavior : Behavior<ComboBox>
    {
        public static readonly StyledProperty<ICommand?> CommandProperty =
            AvaloniaProperty.Register<OnComboBoxClosedWithSelectionBehavior, ICommand?>(nameof(Command));

        public static readonly StyledProperty<object?> CommandParameterProperty =
            AvaloniaProperty.Register<OnComboBoxClosedWithSelectionBehavior, object?>(nameof(CommandParameter));


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
                AssociatedObject.PropertyChanged += OnComboBoxPropertyChanged;
            }
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.PropertyChanged -= OnComboBoxPropertyChanged;
            }
            base.OnDetaching();
        }

        private void OnComboBoxPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ComboBox.IsDropDownOpenProperty)
            {
                bool isOpen = e.GetNewValue<bool>();
                bool wasOpen = e.GetOldValue<bool>();
                if (wasOpen && !isOpen)
                {
                    OnEvent();
                }
            }
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
