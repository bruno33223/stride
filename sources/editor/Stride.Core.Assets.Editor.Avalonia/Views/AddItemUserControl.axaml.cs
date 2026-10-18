// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Views
{
    /// <summary>
    /// Interaction logic for AddItemUserControl.axaml
    /// </summary>
    public partial class AddItemUserControl : UserControl
    {
        public const double TemplateListWidth = 500.0;

        public static readonly StyledProperty<AddItemTemplateCollectionViewModel?> TemplateCollectionProperty =
            AvaloniaProperty.Register<AddItemUserControl, AddItemTemplateCollectionViewModel?>(nameof(TemplateCollection));

        public static readonly StyledProperty<ICommand?> AddItemCommandProperty =
            AvaloniaProperty.Register<AddItemUserControl, ICommand?>(nameof(AddItemCommand));

        public static readonly StyledProperty<ICommand?> SelectFilesToCreateItemCommandProperty =
            AvaloniaProperty.Register<AddItemUserControl, ICommand?>(nameof(SelectFilesToCreateItemCommand));

        public AddItemUserControl()
        {
            InitializeComponent();
            Loaded += ControlLoaded;
        }

        public AddItemTemplateCollectionViewModel? TemplateCollection
        {
            get => GetValue(TemplateCollectionProperty);
            set => SetValue(TemplateCollectionProperty, value);
        }

        public ICommand? AddItemCommand
        {
            get => GetValue(AddItemCommandProperty);
            set => SetValue(AddItemCommandProperty, value);
        }

        public ICommand? SelectFilesToCreateItemCommand
        {
            get => GetValue(SelectFilesToCreateItemCommandProperty);
            set => SetValue(SelectFilesToCreateItemCommandProperty, value);
        }

        private void ControlLoaded(object? sender, RoutedEventArgs e)
        {
            if (TemplateListBox != null)
                TemplateListBox.SelectedIndex = -1;

            if (SearchBox != null)
                SearchBox.Text = string.Empty;

            if (GroupList != null)
                GroupList.SelectedIndex = -1;
        }

        private void TemplateListBox_OnDoubleTapped(object? sender, TappedEventArgs e)
        {
            ConfirmSelection();
        }

        private void TemplateListBox_OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ConfirmSelection();
                e.Handled = true;
            }
        }

        private void ConfirmSelection()
        {
            if (TemplateCollection?.SelectedTemplate != null)
            {
                if (AddItemCommand?.CanExecute(TemplateCollection.SelectedTemplate) == true)
                {
                    AddItemCommand.Execute(TemplateCollection.SelectedTemplate);
                }
            }
        }
    }
}
