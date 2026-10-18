// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Stride.Core.Assets.Diagnostics;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;

namespace Stride.Core.Assets.Editor.Avalonia.Controls
{
    /// <summary>
    /// This control displays a collection of <see cref="ILogMessage"/> in a grid.
    /// </summary>
    [TemplatePart("PART_LogGridView", typeof(DataGridEx))]
    public class GridLogViewer : TemplatedControl
    {
        /// <summary>
        /// The <see cref="DataGridEx"/> used to display log messages.
        /// </summary>
        private DataGridEx? logGridView;

        /// <summary>
        /// Identifies the <see cref="FilteredLogMessages"/> direct property.
        /// </summary>
        public static readonly DirectProperty<GridLogViewer, ObservableList<ILogMessage>> FilteredLogMessagesProperty =
            AvaloniaProperty.RegisterDirect<GridLogViewer, ObservableList<ILogMessage>>(
                nameof(FilteredLogMessages),
                o => o.FilteredLogMessages);

        /// <summary>
        /// Identifies the <see cref="LogMessages"/> styled property.
        /// </summary>
        public static readonly StyledProperty<ObservableList<ILogMessage>?> LogMessagesProperty =
            AvaloniaProperty.Register<GridLogViewer, ObservableList<ILogMessage>?>(nameof(LogMessages));

        /// <summary>
        /// Identifies the <see cref="IsToolBarVisible"/> styled property.
        /// </summary>
        public static readonly StyledProperty<bool> IsToolBarVisibleProperty =
            AvaloniaProperty.Register<GridLogViewer, bool>(nameof(IsToolBarVisible), defaultValue: true);

        /// <summary>
        /// Identifies the <see cref="ShowDebugMessages"/> styled property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowDebugMessagesProperty =
            AvaloniaProperty.Register<GridLogViewer, bool>(nameof(ShowDebugMessages), defaultValue: true);

        /// <summary>
        /// Identifies the <see cref="ShowVerboseMessages"/> styled property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowVerboseMessagesProperty =
            AvaloniaProperty.Register<GridLogViewer, bool>(nameof(ShowVerboseMessages), defaultValue: true);

        /// <summary>
        /// Identifies the <see cref="ShowInfoMessages"/> styled property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowInfoMessagesProperty =
            AvaloniaProperty.Register<GridLogViewer, bool>(nameof(ShowInfoMessages), defaultValue: true);

        /// <summary>
        /// Identifies the <see cref="ShowWarningMessages"/> styled property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowWarningMessagesProperty =
            AvaloniaProperty.Register<GridLogViewer, bool>(nameof(ShowWarningMessages), defaultValue: true);

        /// <summary>
        /// Identifies the <see cref="ShowErrorMessages"/> styled property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowErrorMessagesProperty =
            AvaloniaProperty.Register<GridLogViewer, bool>(nameof(ShowErrorMessages), defaultValue: true);

        /// <summary>
        /// Identifies the <see cref="ShowFatalMessages"/> styled property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowFatalMessagesProperty =
            AvaloniaProperty.Register<GridLogViewer, bool>(nameof(ShowFatalMessages), defaultValue: true);

        /// <summary>
        /// Identifies the <see cref="ShowStacktrace"/> styled property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowStacktraceProperty =
            AvaloniaProperty.Register<GridLogViewer, bool>(nameof(ShowStacktrace), defaultValue: false);

        /// <summary>
        /// Identifies the <see cref="SearchText"/> styled property.
        /// </summary>
        public static readonly StyledProperty<string?> SearchTextProperty =
            AvaloniaProperty.Register<GridLogViewer, string?>(nameof(SearchText), defaultValue: null);

        /// <summary>
        /// Identifies the <see cref="Session"/> styled property.
        /// </summary>
        public static readonly StyledProperty<SessionViewModel?> SessionProperty =
            AvaloniaProperty.Register<GridLogViewer, SessionViewModel?>(nameof(Session));

        /// <summary>
        /// Gets the command to clear all log messages.
        /// </summary>
        public ICommand ClearLogCommand { get; }

        /// <summary>
        /// Gets the command to reset message severity filters to their defaults.
        /// </summary>
        public ICommand ResetFiltersCommand { get; }

        static GridLogViewer()
        {
            LogMessagesProperty.Changed.AddClassHandler<GridLogViewer>((x, e) => x.OnLogMessagesPropertyChanged(e));
            ShowDebugMessagesProperty.Changed.AddClassHandler<GridLogViewer>((x, _) => x.ApplyFilters());
            ShowVerboseMessagesProperty.Changed.AddClassHandler<GridLogViewer>((x, _) => x.ApplyFilters());
            ShowInfoMessagesProperty.Changed.AddClassHandler<GridLogViewer>((x, _) => x.ApplyFilters());
            ShowWarningMessagesProperty.Changed.AddClassHandler<GridLogViewer>((x, _) => x.ApplyFilters());
            ShowErrorMessagesProperty.Changed.AddClassHandler<GridLogViewer>((x, _) => x.ApplyFilters());
            ShowFatalMessagesProperty.Changed.AddClassHandler<GridLogViewer>((x, _) => x.ApplyFilters());
            ShowStacktraceProperty.Changed.AddClassHandler<GridLogViewer>((x, _) => x.ApplyFilters());
            SearchTextProperty.Changed.AddClassHandler<GridLogViewer>((x, _) => x.ApplyFilters());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridLogViewer"/> class.
        /// </summary>
        public GridLogViewer()
        {
            ClearLogCommand = new SimpleRelayCommand(() => LogMessages?.Clear());
            ResetFiltersCommand = new SimpleRelayCommand(() =>
            {
                ShowDebugMessages = true;
                ShowVerboseMessages = true;
                ShowInfoMessages = true;
                ShowWarningMessages = true;
                ShowErrorMessages = true;
                ShowFatalMessages = true;
                SearchText = null;
            });
        }

        /// <summary>
        /// Gets or sets the collection of <see cref="ILogMessage"/> to display.
        /// </summary>
        public ObservableList<ILogMessage>? LogMessages
        {
            get => GetValue(LogMessagesProperty);
            set => SetValue(LogMessagesProperty, value);
        }

        /// <summary>
        /// Gets the filtered collection of log messages presented to the user.
        /// </summary>
        public ObservableList<ILogMessage> FilteredLogMessages { get; } = new ObservableList<ILogMessage>();

        /// <summary>
        /// Gets or sets whether the tool bar should be visible.
        /// </summary>
        public bool IsToolBarVisible
        {
            get => GetValue(IsToolBarVisibleProperty);
            set => SetValue(IsToolBarVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the log viewer should display debug messages.
        /// </summary>
        public bool ShowDebugMessages
        {
            get => GetValue(ShowDebugMessagesProperty);
            set => SetValue(ShowDebugMessagesProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the log viewer should display verbose messages.
        /// </summary>
        public bool ShowVerboseMessages
        {
            get => GetValue(ShowVerboseMessagesProperty);
            set => SetValue(ShowVerboseMessagesProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the log viewer should display info messages.
        /// </summary>
        public bool ShowInfoMessages
        {
            get => GetValue(ShowInfoMessagesProperty);
            set => SetValue(ShowInfoMessagesProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the log viewer should display warning messages.
        /// </summary>
        public bool ShowWarningMessages
        {
            get => GetValue(ShowWarningMessagesProperty);
            set => SetValue(ShowWarningMessagesProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the log viewer should display error messages.
        /// </summary>
        public bool ShowErrorMessages
        {
            get => GetValue(ShowErrorMessagesProperty);
            set => SetValue(ShowErrorMessagesProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the log viewer should display fatal messages.
        /// </summary>
        public bool ShowFatalMessages
        {
            get => GetValue(ShowFatalMessagesProperty);
            set => SetValue(ShowFatalMessagesProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the exception stack trace should be shown.
        /// </summary>
        public bool ShowStacktrace
        {
            get => GetValue(ShowStacktraceProperty);
            set => SetValue(ShowStacktraceProperty, value);
        }

        /// <summary>
        /// Gets or sets the text filter used to narrow displayed messages.
        /// </summary>
        public string? SearchText
        {
            get => GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        /// <summary>
        /// Gets or sets the session to use to select an asset related to a log message.
        /// </summary>
        public SessionViewModel? Session
        {
            get => GetValue(SessionProperty);
            set => SetValue(SessionProperty, value);
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            logGridView = e.NameScope.Find("PART_LogGridView") as DataGridEx;
            if (logGridView != null)
            {
                logGridView.ItemsSource = FilteredLogMessages;
                logGridView.DoubleTapped += GridDoubleTapped;
            }

            ApplyFilters();
        }

        private void GridDoubleTapped(object? sender, RoutedEventArgs e)
        {
            if (Session == null || logGridView == null)
                return;

            if (logGridView.SelectedItem is AssetSerializableLogMessage logMessage && !string.IsNullOrEmpty(logMessage.AssetUrl))
            {
                var asset = Session.GetAssetById(logMessage.AssetId);
                if (asset != null)
                {
                    Session.ActiveAssetView?.SelectAssetCommand?.Execute(asset);
                }
            }

            if (logGridView.SelectedItem is AssetLogMessage assetLogMessage && assetLogMessage.AssetReference != null)
            {
                var asset = Session.GetAssetById(assetLogMessage.AssetReference.Id);
                if (asset != null)
                {
                    Session.ActiveAssetView?.SelectAssetCommand?.Execute(asset);
                }
            }
        }

        private void OnLogMessagesPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.OldValue is ObservableList<ILogMessage> prevList)
            {
                prevList.CollectionChanged -= OnLogMessagesCollectionChanged;
            }
            if (e.NewValue is ObservableList<ILogMessage> newList)
            {
                newList.CollectionChanged += OnLogMessagesCollectionChanged;
            }
            ApplyFilters();
        }

        private void OnLogMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (LogMessages == null)
                return;

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                FilteredLogMessages.Clear();
            }
            else
            {
                bool refreshFilter = false;
                if (e.OldItems != null)
                    refreshFilter = e.OldItems.OfType<ILogMessage>().Any(IsMessageVisible);

                if (!refreshFilter && e.NewItems != null)
                    refreshFilter = e.NewItems.OfType<ILogMessage>().Any(IsMessageVisible);

                if (refreshFilter)
                    ApplyFilters();
            }
        }

        private void ApplyFilters()
        {
            if (LogMessages == null)
            {
                FilteredLogMessages.Clear();
                return;
            }

            FilteredLogMessages.Clear();
            FilteredLogMessages.AddRange(LogMessages.Where(IsMessageVisible));
        }

        private bool IsMessageVisible(ILogMessage x)
        {
            bool severityMatch = (x.IsDebug() && ShowDebugMessages)
                || (x.IsError() && ShowErrorMessages)
                || (x.IsFatal() && ShowFatalMessages)
                || (x.IsInfo() && ShowInfoMessages)
                || (x.IsVerbose() && ShowVerboseMessages)
                || (x.IsWarning() && ShowWarningMessages);

            if (!severityMatch)
                return false;

            if (!string.IsNullOrEmpty(SearchText))
            {
                var text = x.Text ?? string.Empty;
                var module = x.Module ?? string.Empty;
                return text.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
                    || module.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase);
            }

            return true;
        }

        private sealed class SimpleRelayCommand : ICommand
        {
            private readonly Action execute;
            public SimpleRelayCommand(Action execute) => this.execute = execute;
            public bool CanExecute(object? parameter) => true;
            public void Execute(object? parameter) => execute();
            public event EventHandler? CanExecuteChanged { add { } remove { } }
        }
    }
}
