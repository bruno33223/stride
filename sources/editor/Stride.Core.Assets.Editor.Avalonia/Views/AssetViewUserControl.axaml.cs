// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;
using Avalonia.Markup.Xaml;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

/// <summary>
/// Interaction logic for AssetViewUserControl.axaml
/// </summary>
public partial class AssetViewUserControl : UserControl
{
    /// <summary>
    /// Identifies the <see cref="AssetCollection"/> styled property.
    /// </summary>
    public static readonly StyledProperty<AssetCollectionViewModel?> AssetCollectionProperty =
        AvaloniaProperty.Register<AssetViewUserControl, AssetCollectionViewModel?>(nameof(AssetCollection));

    /// <summary>
    /// Identifies the <see cref="AssetContextMenu"/> styled property.
    /// </summary>
    public static readonly StyledProperty<Control?> AssetContextMenuProperty =
        AvaloniaProperty.Register<AssetViewUserControl, Control?>(nameof(AssetContextMenu));

    /// <summary>
    /// Identifies the <see cref="CanEditAssets"/> styled property.
    /// </summary>
    public static readonly StyledProperty<bool> CanEditAssetsProperty =
        AvaloniaProperty.Register<AssetViewUserControl, bool>(nameof(CanEditAssets), defaultValue: true);

    /// <summary>
    /// Identifies the <see cref="CanAddAssets"/> styled property.
    /// </summary>
    public static readonly StyledProperty<bool> CanAddAssetsProperty =
        AvaloniaProperty.Register<AssetViewUserControl, bool>(nameof(CanAddAssets), defaultValue: true);

    /// <summary>
    /// Identifies the <see cref="CanDeleteAssets"/> styled property.
    /// </summary>
    public static readonly StyledProperty<bool> CanDeleteAssetsProperty =
        AvaloniaProperty.Register<AssetViewUserControl, bool>(nameof(CanDeleteAssets), defaultValue: true);

    /// <summary>
    /// Identifies the <see cref="CanRecursivelyDisplayAssets"/> styled property.
    /// </summary>
    public static readonly StyledProperty<bool> CanRecursivelyDisplayAssetsProperty =
        AvaloniaProperty.Register<AssetViewUserControl, bool>(nameof(CanRecursivelyDisplayAssets), defaultValue: true);

    /// <summary>
    /// Identifies the <see cref="GiveFocusOnSelectionChange"/> styled property.
    /// </summary>
    public static readonly StyledProperty<bool> GiveFocusOnSelectionChangeProperty =
        AvaloniaProperty.Register<AssetViewUserControl, bool>(nameof(GiveFocusOnSelectionChange), defaultValue: true);

    /// <summary>
    /// Identifies the <see cref="ThumbnailZoomIncrement"/> styled property.
    /// </summary>
    public static readonly StyledProperty<double> ThumbnailZoomIncrementProperty =
        AvaloniaProperty.Register<AssetViewUserControl, double>(nameof(ThumbnailZoomIncrement), defaultValue: 16.0);

    /// <summary>
    /// Identifies the <see cref="ThumbnailMinimumSize"/> styled property.
    /// </summary>
    public static readonly StyledProperty<double> ThumbnailMinimumSizeProperty =
        AvaloniaProperty.Register<AssetViewUserControl, double>(nameof(ThumbnailMinimumSize), defaultValue: 16.0);

    /// <summary>
    /// Identifies the <see cref="ThumbnailMaximumSize"/> styled property.
    /// </summary>
    public static readonly StyledProperty<double> ThumbnailMaximumSizeProperty =
        AvaloniaProperty.Register<AssetViewUserControl, double>(nameof(ThumbnailMaximumSize), defaultValue: 128.0);

    /// <summary>
    /// Identifies the <see cref="TileThumbnailSize"/> styled property.
    /// </summary>
    public static readonly StyledProperty<double> TileThumbnailSizeProperty =
        AvaloniaProperty.Register<AssetViewUserControl, double>(nameof(TileThumbnailSize), defaultValue: 64.0);

    /// <summary>
    /// Identifies the <see cref="GridThumbnailSize"/> styled property.
    /// </summary>
    public static readonly StyledProperty<double> GridThumbnailSizeProperty =
        AvaloniaProperty.Register<AssetViewUserControl, double>(nameof(GridThumbnailSize), defaultValue: 32.0);

    /// <summary>
    /// Identifies the <see cref="AssetDoubleClick"/> styled property.
    /// </summary>
    public static readonly StyledProperty<ICommand?> AssetDoubleClickProperty =
        AvaloniaProperty.Register<AssetViewUserControl, ICommand?>(nameof(AssetDoubleClick));

    /// <summary>
    /// Gets the command that initiates the edition of the currently selected item.
    /// </summary>
    public ICommand BeginEditCommand { get; }

    /// <summary>
    /// Gets the command that will increase the size of thumbnails.
    /// </summary>
    public ICommand ZoomInCommand { get; }

    /// <summary>
    /// Gets the command that will decrease the size of thumbnails.
    /// </summary>
    public ICommand ZoomOutCommand { get; }

    /// <summary>
    /// Gets the list of items to display in the primary tool bar. The primary tool bar won't be displayed if this list is empty.
    /// </summary>
    public IList PrimaryToolBarItems { get; } = new ObservableCollection<object>();

    static AssetViewUserControl()
    {
        AssetCollectionProperty.Changed.AddClassHandler<AssetViewUserControl>((x, e) => x.OnAssetCollectionChanged(e));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetViewUserControl"/> class.
    /// </summary>
    public AssetViewUserControl()
    {
        var sp = new ViewModelServiceProvider();
        BeginEditCommand = new AnonymousCommand(sp, BeginEdit, CanBeginEdit);
        ZoomInCommand = new AnonymousCommand(sp, ZoomIn);
        ZoomOutCommand = new AnonymousCommand(sp, ZoomOut);

        InitializeComponent();

        AddHandler(PointerWheelChangedEvent, OnPreviewPointerWheel, RoutingStrategies.Tunnel);
        AddHandler(PointerPressedEvent, OnPreviewPointerPressed, RoutingStrategies.Tunnel);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Gets or sets the <see cref="AssetCollectionViewModel"/> to display in this control.
    /// </summary>
    public AssetCollectionViewModel? AssetCollection
    {
        get => GetValue(AssetCollectionProperty);
        set => SetValue(AssetCollectionProperty, value);
    }

    /// <summary>
    /// Gets or sets the control to use as context menu for assets.
    /// </summary>
    public Control? AssetContextMenu
    {
        get => GetValue(AssetContextMenuProperty);
        set => SetValue(AssetContextMenuProperty, value);
    }

    /// <summary>
    /// Gets or sets whether it is possible to edit assets with this <see cref="AssetViewUserControl"/>.
    /// </summary>
    public bool CanEditAssets
    {
        get => GetValue(CanEditAssetsProperty);
        set => SetValue(CanEditAssetsProperty, value);
    }

    /// <summary>
    /// Gets or sets whether it is possible to add assets with this <see cref="AssetViewUserControl"/>.
    /// </summary>
    public bool CanAddAssets
    {
        get => GetValue(CanAddAssetsProperty);
        set => SetValue(CanAddAssetsProperty, value);
    }

    /// <summary>
    /// Gets or sets whether it is possible to delete assets with this <see cref="AssetViewUserControl"/>.
    /// </summary>
    public bool CanDeleteAssets
    {
        get => GetValue(CanDeleteAssetsProperty);
        set => SetValue(CanDeleteAssetsProperty, value);
    }

    /// <summary>
    /// Gets or sets whether it is possible to select to display asset recursively from selected locations.
    /// </summary>
    public bool CanRecursivelyDisplayAssets
    {
        get => GetValue(CanRecursivelyDisplayAssetsProperty);
        set => SetValue(CanRecursivelyDisplayAssetsProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the control should get the focus when its selection changes. The focus is not given if the selection is cleared.
    /// </summary>
    public bool GiveFocusOnSelectionChange
    {
        get => GetValue(GiveFocusOnSelectionChangeProperty);
        set => SetValue(GiveFocusOnSelectionChangeProperty, value);
    }

    /// <summary>
    /// Gets or sets the zoom increment value.
    /// </summary>
    public double ThumbnailZoomIncrement
    {
        get => GetValue(ThumbnailZoomIncrementProperty);
        set => SetValue(ThumbnailZoomIncrementProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimum size of thumbnails.
    /// </summary>
    public double ThumbnailMinimumSize
    {
        get => GetValue(ThumbnailMinimumSizeProperty);
        set => SetValue(ThumbnailMinimumSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum size of thumbnails.
    /// </summary>
    public double ThumbnailMaximumSize
    {
        get => GetValue(ThumbnailMaximumSizeProperty);
        set => SetValue(ThumbnailMaximumSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the size of thumbnails in tile view.
    /// </summary>
    public double TileThumbnailSize
    {
        get => GetValue(TileThumbnailSizeProperty);
        set => SetValue(TileThumbnailSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the size of thumbnails in grid view.
    /// </summary>
    public double GridThumbnailSize
    {
        get => GetValue(GridThumbnailSizeProperty);
        set => SetValue(GridThumbnailSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the command to execute when user double-clicks an asset.
    /// </summary>
    public ICommand? AssetDoubleClick
    {
        get => GetValue(AssetDoubleClickProperty);
        set => SetValue(AssetDoubleClickProperty, value);
    }

    /// <summary>
    /// Begins edition of the currently selected content.
    /// </summary>
    public void BeginEdit()
    {
        if (!CanEditAssets || AssetCollection == null)
            return;

        var selectedAsset = AssetCollection.SelectedContent.LastOrDefault();
        if (selectedAsset == null)
            return;

        // In Avalonia, setting IsEditing on the selected item initiates editing
        if (selectedAsset is Stride.Core.Presentation.ViewModels.IIsEditableViewModel editable)
        {
            editable.IsEditing = true;
        }
    }

    public void ZoomIn()
    {
        TileThumbnailSize += ThumbnailZoomIncrement;
        if (TileThumbnailSize >= ThumbnailMaximumSize)
        {
            TileThumbnailSize = ThumbnailMaximumSize;
        }

        GridThumbnailSize += ThumbnailZoomIncrement;
        if (GridThumbnailSize >= ThumbnailMaximumSize)
        {
            GridThumbnailSize = ThumbnailMaximumSize;
        }
    }

    public void ZoomOut()
    {
        TileThumbnailSize -= ThumbnailZoomIncrement;
        if (TileThumbnailSize <= ThumbnailMinimumSize)
        {
            TileThumbnailSize = ThumbnailMinimumSize;
        }

        GridThumbnailSize -= ThumbnailZoomIncrement;
        if (GridThumbnailSize <= ThumbnailMinimumSize)
        {
            GridThumbnailSize = ThumbnailMinimumSize;
        }
    }

    private void OnPreviewPointerWheel(object? sender, PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (e.Delta.Y > 0)
            {
                ZoomIn();
                e.Handled = true;
            }
            else if (e.Delta.Y < 0)
            {
                ZoomOut();
                e.Handled = true;
            }
        }
    }

    private void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsFocused)
        {
            Focus();
        }
    }

    private bool CanBeginEdit()
    {
        if (!CanEditAssets || AssetCollection == null)
            return false;

        if (AssetCollection.SelectedContent.Count == 0)
            return true;

        if (AssetCollection.SelectedContent.Count != 1)
            return false;

        var asset = AssetCollection.SelectedContent.Last() as Stride.Core.Assets.Presentation.ViewModels.AssetViewModel;
        return asset?.IsEditable ?? true;
    }

    private void OnAssetCollectionChanged(AvaloniaPropertyChangedEventArgs e)
    {
        var rootContainer = this.FindControl<Control>("RootContainer");
        if (rootContainer != null)
        {
            rootContainer.DataContext = e.NewValue;
        }
        else
        {
            DataContext = e.NewValue;
        }
    }
}
