// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Presentation.Commands;

namespace Stride.GameStudio.Avalonia.Views;

public partial class NotificationWindow : Window
{
    private DispatcherTimer? autoDismissTimer;

    private int currentVerticalOffsetPx;

    public static readonly StyledProperty<string> CaptionProperty =
        AvaloniaProperty.Register<NotificationWindow, string>(nameof(Caption), string.Empty);

    public string Caption
    {
        get => GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<NotificationWindow, string>(nameof(Message), string.Empty);

    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public static readonly StyledProperty<string> ActionButtonTextProperty =
        AvaloniaProperty.Register<NotificationWindow, string>(nameof(ActionButtonText), "Action");

    public string ActionButtonText
    {
        get => GetValue(ActionButtonTextProperty);
        set => SetValue(ActionButtonTextProperty, value);
    }

    public static readonly StyledProperty<ICommandBase?> ActionCommandProperty =
        AvaloniaProperty.Register<NotificationWindow, ICommandBase?>(nameof(ActionCommand));

    public ICommandBase? ActionCommand
    {
        get => GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public static readonly StyledProperty<bool> HasActionCommandProperty =
        AvaloniaProperty.Register<NotificationWindow, bool>(nameof(HasActionCommand), false);

    public bool HasActionCommand
    {
        get => GetValue(HasActionCommandProperty);
        private set => SetValue(HasActionCommandProperty, value);
    }

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<NotificationWindow, object?>(nameof(CommandParameter));

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public TimeSpan AutoDismissDuration { get; set; } = TimeSpan.FromSeconds(5);

    static NotificationWindow()
    {
        ActionCommandProperty.Changed.AddClassHandler<NotificationWindow, ICommandBase?>((w, e) =>
        {
            w.HasActionCommand = e.NewValue.GetValueOrDefault() is not null;
        });
    }

    public NotificationWindow()
    {
        InitializeComponent();
        DataContext = this;

        Opened += (_, _) => SetPosition(currentVerticalOffsetPx);
        SizeChanged += (_, _) => SetPosition(currentVerticalOffsetPx);
    }

    public NotificationWindow(
        string caption,
        string message,
        ICommandBase? command = null,
        object? commandParameter = null,
        TimeSpan? autoDismissDuration = null,
        string? actionButtonText = null)
        : this()
    {
        Caption = caption;
        Message = message;
        ActionCommand = command;
        CommandParameter = commandParameter;
        if (!string.IsNullOrEmpty(actionButtonText))
        {
            ActionButtonText = actionButtonText;
        }

        AutoDismissDuration = autoDismissDuration ?? TimeSpan.FromSeconds(5);
        StartAutoDismissTimer();
    }

    private void StartAutoDismissTimer()
    {
        if (AutoDismissDuration <= TimeSpan.Zero) return;

        autoDismissTimer?.Stop();
        autoDismissTimer = new DispatcherTimer { Interval = AutoDismissDuration };
        autoDismissTimer.Tick += (_, _) =>
        {
            autoDismissTimer.Stop();
            Close();
        };
        autoDismissTimer.Start();
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        autoDismissTimer?.Stop();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (autoDismissTimer is not null && AutoDismissDuration > TimeSpan.Zero)
        {
            autoDismissTimer.Start();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            autoDismissTimer?.Stop();
            Close();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    private void OnClick_Close(object? sender, RoutedEventArgs e)
    {
        autoDismissTimer?.Stop();
        Close();
    }

    private void OnClick_Action(object? sender, RoutedEventArgs e)
    {
        autoDismissTimer?.Stop();
        if (ActionCommand != null && ActionCommand.CanExecute(CommandParameter))
        {
            ActionCommand.Execute(CommandParameter);
        }
        Close();
    }

    public void SetPosition(int verticalOffsetPx = 0)
    {
        currentVerticalOffsetPx = verticalOffsetPx;

        var targetScreen = (DialogService.MainWindow is not null ? Screens.ScreenFromWindow(DialogService.MainWindow) : null)
                           ?? Screens.Primary;

        if (targetScreen is null) return;

        var scaling = targetScreen.Scaling;
        var workingArea = targetScreen.WorkingArea;

        var widthDip = Bounds.Width > 0 ? Bounds.Width : (!double.IsNaN(Width) ? Width : 350);
        var heightDip = Bounds.Height > 0 ? Bounds.Height : (!double.IsNaN(Height) ? Height : 120);

        var widthPx = (int)Math.Ceiling(widthDip * scaling);
        var heightPx = (int)Math.Ceiling(heightDip * scaling);

        const int marginDip = 20;
        var marginPx = (int)(marginDip * scaling);

        var x = workingArea.X + workingArea.Width - widthPx - marginPx;
        var y = workingArea.Y + workingArea.Height - heightPx - marginPx - currentVerticalOffsetPx;

        Position = new PixelPoint(x, y);
    }
}
