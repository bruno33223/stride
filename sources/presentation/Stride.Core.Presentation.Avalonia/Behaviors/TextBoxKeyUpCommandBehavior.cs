// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace Stride.Core.Presentation.Avalonia.Behaviors;

public class TextBoxKeyUpCommandBehavior : StyledElementBehavior<TextBox>
{
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<TextBoxKeyUpCommandBehavior, ICommand?>(nameof(Command));

    public static readonly StyledProperty<Key> KeyProperty =
        AvaloniaProperty.Register<TextBoxKeyUpCommandBehavior, Key>(nameof(Key), Key.Enter);

    public static readonly StyledProperty<object?> CloseTargetProperty =
        AvaloniaProperty.Register<TextBoxKeyUpCommandBehavior, object?>(nameof(CloseTarget));

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public Key Key
    {
        get => GetValue(KeyProperty);
        set => SetValue(KeyProperty, value);
    }

    public object? CloseTarget
    {
        get => GetValue(CloseTargetProperty);
        set => SetValue(CloseTargetProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is not null)
        {
            AssociatedObject.KeyUp += OnKeyUp;
        }
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.KeyUp -= OnKeyUp;
        }
        base.OnDetaching();
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key || AssociatedObject is null)
            return;

        var text = AssociatedObject.Text;
        if (Command is not null && Command.CanExecute(text))
        {
            Command.Execute(text);
            e.Handled = true;

            if (CloseTarget is ToggleButton tb)
            {
                tb.IsChecked = false;
            }
            else if (CloseTarget is Popup popup)
            {
                popup.IsOpen = false;
            }
        }
    }
}
