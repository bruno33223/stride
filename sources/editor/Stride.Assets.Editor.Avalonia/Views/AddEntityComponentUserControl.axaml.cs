// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;

namespace Stride.Assets.Editor.Avalonia.Views;

public partial class AddEntityComponentUserControl : UserControl
{
    public const double ComponentListWidth = 260.0;

    public static readonly StyledProperty<IEnumerable<AbstractNodeType>?> AvailableComponentTypesProperty =
        AvaloniaProperty.Register<AddEntityComponentUserControl, IEnumerable<AbstractNodeType>?>(nameof(AvailableComponentTypes));

    public static readonly StyledProperty<IEnumerable<AbstractNodeTypeGroup>?> AvailableComponentTypeGroupsProperty =
        AvaloniaProperty.Register<AddEntityComponentUserControl, IEnumerable<AbstractNodeTypeGroup>?>(nameof(AvailableComponentTypeGroups));

    public static readonly StyledProperty<AbstractNodeTypeGroup?> SelectedGroupProperty =
        AvaloniaProperty.Register<AddEntityComponentUserControl, AbstractNodeTypeGroup?>(nameof(SelectedGroup));

    public static readonly StyledProperty<string?> SearchTokenProperty =
        AvaloniaProperty.Register<AddEntityComponentUserControl, string?>(nameof(SearchToken));

    public static readonly StyledProperty<IEnumerable<AbstractNodeType>?> ComponentTypesProperty =
        AvaloniaProperty.Register<AddEntityComponentUserControl, IEnumerable<AbstractNodeType>?>(nameof(ComponentTypes));

    public static readonly StyledProperty<ICommand?> AddNewItemCommandProperty =
        AvaloniaProperty.Register<AddEntityComponentUserControl, ICommand?>(nameof(AddNewItemCommand));

    public static readonly StyledProperty<ICommand?> AddNewScriptComponentCommandProperty =
        AvaloniaProperty.Register<AddEntityComponentUserControl, ICommand?>(nameof(AddNewScriptComponentCommand));

    public IEnumerable<AbstractNodeType>? AvailableComponentTypes
    {
        get => GetValue(AvailableComponentTypesProperty);
        set => SetValue(AvailableComponentTypesProperty, value);
    }

    public IEnumerable<AbstractNodeTypeGroup>? AvailableComponentTypeGroups
    {
        get => GetValue(AvailableComponentTypeGroupsProperty);
        set => SetValue(AvailableComponentTypeGroupsProperty, value);
    }

    public AbstractNodeTypeGroup? SelectedGroup
    {
        get => GetValue(SelectedGroupProperty);
        set => SetValue(SelectedGroupProperty, value);
    }

    public string? SearchToken
    {
        get => GetValue(SearchTokenProperty);
        set => SetValue(SearchTokenProperty, value);
    }

    public IEnumerable<AbstractNodeType>? ComponentTypes
    {
        get => GetValue(ComponentTypesProperty);
        set => SetValue(ComponentTypesProperty, value);
    }

    public ICommand? AddNewItemCommand
    {
        get => GetValue(AddNewItemCommandProperty);
        set => SetValue(AddNewItemCommandProperty, value);
    }

    public ICommand? AddNewScriptComponentCommand
    {
        get => GetValue(AddNewScriptComponentCommandProperty);
        set => SetValue(AddNewScriptComponentCommandProperty, value);
    }

    private ToggleButton? toggleButton;
    private Popup? popup;
    private Border? popupRoot;
    private TextBox? searchTextBox;
    private ListBox? groupListBox;
    private ListBox? componentListBox;

    public AddEntityComponentUserControl()
    {
        InitializeComponent();
        InitializeControls();
    }

    private void InitializeControls()
    {
        toggleButton = this.FindControl<ToggleButton>("ToggleButton");
        popup = this.FindControl<Popup>("Popup");
        popupRoot = this.FindControl<Border>("PopupRoot");
        searchTextBox = this.FindControl<TextBox>("SearchTextBox");
        groupListBox = this.FindControl<ListBox>("GroupListBox");
        componentListBox = this.FindControl<ListBox>("ComponentListBox");

        popupRoot ??= popup?.Child as Border;

        if (popupRoot != null)
        {
            popupRoot.DataContext = this;
        }

        if (popup != null)
        {
            popup.Opened += OnPopupOpened;
            popup.Closed += OnPopupClosed;
        }

        if (searchTextBox != null)
        {
            searchTextBox.KeyDown += OnSearchTextBoxKeyDown;
        }

        if (groupListBox != null)
        {
            groupListBox.SelectionChanged += OnGroupListBoxSelectionChanged;
            groupListBox.KeyDown += OnGroupListBoxKeyDown;
        }

        if (componentListBox != null)
        {
            componentListBox.AddHandler(InputElement.TappedEvent, OnComponentListBoxTapped, RoutingStrategies.Bubble);
            componentListBox.KeyDown += OnComponentListBoxKeyDown;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ClosePopup();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedGroupProperty)
        {
            if (change.NewValue is AbstractNodeTypeGroup group)
            {
                ComponentTypes = group.Types;
            }
            else if (string.IsNullOrWhiteSpace(SearchToken))
            {
                ComponentTypes = null;
            }
        }
        else if (change.Property == SearchTokenProperty)
        {
            UpdateSearch(change.NewValue as string);
        }
        else if (change.Property == AvailableComponentTypesProperty)
        {
            if (!string.IsNullOrWhiteSpace(SearchToken))
            {
                UpdateSearch(SearchToken);
            }
        }
    }

    private void UpdateSearch(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            ComponentTypes = SelectedGroup?.Types;
        }
        else
        {
            if (SelectedGroup != null)
            {
                SelectedGroup = null;
                if (groupListBox != null)
                {
                    groupListBox.SelectedItem = null;
                }
            }

            if (AvailableComponentTypes != null)
            {
                var search = token.Trim();
                ComponentTypes = AvailableComponentTypes
                    .Where(t => t.DisplayValue != null && t.DisplayValue.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            else
            {
                ComponentTypes = null;
            }
        }
    }

    private void OnPopupOpened(object? sender, EventArgs e)
    {
        searchTextBox?.Focus();
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        ResetState();
    }

    private void OnGroupListBoxSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (groupListBox?.SelectedItem is AbstractNodeTypeGroup group)
        {
            if (!string.IsNullOrEmpty(SearchToken))
            {
                SearchToken = null;
            }
            SelectedGroup = group;
            ComponentTypes = group.Types;
        }
    }

    private void OnGroupListBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Right)
        {
            if (componentListBox != null)
            {
                componentListBox.Focus();
                if (componentListBox.SelectedIndex < 0 && ComponentTypes?.Any() == true)
                {
                    componentListBox.SelectedIndex = 0;
                }
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Escape)
        {
            ClosePopup();
            e.Handled = true;
        }
    }

    private void OnComponentListBoxTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Visual visual && ((visual as ListBoxItem) ?? visual.FindAncestorOfType<ListBoxItem>()) is { DataContext: AbstractNodeType nodeType })
        {
            ExecuteAddItem(nodeType);
            e.Handled = true;
        }
    }

    private void OnComponentListBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && componentListBox?.SelectedItem is AbstractNodeType selectedType)
        {
            ExecuteAddItem(selectedType);
            e.Handled = true;
        }
        else if (e.Key == Key.Left)
        {
            groupListBox?.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            ClosePopup();
            e.Handled = true;
        }
    }

    private void OnSearchTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var firstItem = ComponentTypes?.FirstOrDefault();
            if (firstItem != null)
            {
                ExecuteAddItem(firstItem);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Down)
        {
            if (componentListBox != null)
            {
                componentListBox.Focus();
                if (componentListBox.SelectedIndex < 0 && ComponentTypes?.Any() == true)
                {
                    componentListBox.SelectedIndex = 0;
                }
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Escape)
        {
            ClosePopup();
            e.Handled = true;
        }
    }

    private void ExecuteAddItem(AbstractNodeType type)
    {
        if (AddNewItemCommand?.CanExecute(type) == true)
        {
            AddNewItemCommand.Execute(type);
        }
        ClosePopup();
    }

    private void ClosePopup()
    {
        if (toggleButton != null)
        {
            toggleButton.IsChecked = false;
        }
        if (popup != null)
        {
            popup.IsOpen = false;
        }
        ResetState();
    }

    private void ResetState()
    {
        SearchToken = null;
        SelectedGroup = null;
        if (groupListBox != null)
        {
            groupListBox.SelectedItem = null;
        }
        if (componentListBox != null)
        {
            componentListBox.SelectedItem = null;
        }
        ComponentTypes = null;
    }
}
