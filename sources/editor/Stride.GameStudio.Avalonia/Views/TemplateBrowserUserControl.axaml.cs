// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;

namespace Stride.GameStudio.Avalonia.Views;

public partial class TemplateBrowserUserControl : UserControl
{
    public static readonly StyledProperty<IEnumerable?> HierarchyItemsSourceProperty =
        AvaloniaProperty.Register<TemplateBrowserUserControl, IEnumerable?>(nameof(HierarchyItemsSource));

    public static readonly StyledProperty<object?> SelectedHierarchyItemProperty =
        AvaloniaProperty.Register<TemplateBrowserUserControl, object?>(
            nameof(SelectedHierarchyItem),
            defaultBindingMode: BindingMode.TwoWay);

    public event EventHandler? TemplateConfirmed;

    public TemplateBrowserUserControl()
    {
        InitializeComponent();
    }

    public IEnumerable? HierarchyItemsSource
    {
        get => GetValue(HierarchyItemsSourceProperty);
        set => SetValue(HierarchyItemsSourceProperty, value);
    }

    public object? SelectedHierarchyItem
    {
        get => GetValue(SelectedHierarchyItemProperty);
        set => SetValue(SelectedHierarchyItemProperty, value);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is TemplateDescriptionCollectionViewModel vm)
        {
            if (HierarchyItemsSource is null)
            {
                HierarchyItemsSource = vm.RootGroups;
            }
            if (SelectedHierarchyItem is null)
            {
                SelectedHierarchyItem = vm.SelectedGroup;
            }
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedHierarchyItemProperty && DataContext is TemplateDescriptionCollectionViewModel vm)
        {
            if (change.NewValue is TemplateDescriptionGroupViewModel group && vm.SelectedGroup != group)
            {
                vm.SelectedGroup = group;
            }
        }
    }

    private void TemplatesListBox_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        OnConfirm();
    }

    private void TemplatesListBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OnConfirm();
            e.Handled = true;
        }
    }

    private void OnConfirm()
    {
        if (DataContext is TemplateDescriptionCollectionViewModel vm && vm.SelectedTemplate is not null)
        {
            TemplateConfirmed?.Invoke(this, EventArgs.Empty);
        }
    }
}
