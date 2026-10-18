// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.UI;
using Stride.Core.Assets;
using Stride.Core.Presentation.ViewModels;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels;

public class UIElementFactoryViewModel : ViewModelBase, IUIElementFactory
{
    private readonly Func<UIElement>? createFunc;
    private readonly UILibraryViewModel? library;
    private readonly Guid libraryElementId;

    public UIElementFactoryViewModel(
        string name,
        string category,
        Type elementType,
        string? iconData = null,
        Func<UIElement>? createFunc = null,
        string? description = null)
    {
        Name = name;
        Category = category;
        ElementType = elementType;
        IconData = iconData ?? "M19,3H5C3.89,3 3,3.89 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5C21,3.89 20.1,3 19,3M19,5V19H5V5H19Z";
        this.createFunc = createFunc;
        Description = description ?? name;
    }

    public UIElementFactoryViewModel(UILibraryViewModel library, Guid elementId, string name)
    {
        this.library = library;
        libraryElementId = elementId;
        Name = name;
        Category = library.Url ?? "Library";
        ElementType = typeof(UIElement);
        IconData = "M4,6H2V20A2,2 0 0,0 4,22H18V20H4V6M20,2H8A2,2 0 0,0 6,4V16A2,2 0 0,0 8,18H20A2,2 0 0,0 22,16V4A2,2 0 0,0 20,2M20,16H8V4H20V16Z";
        Description = $"Library Element: {name}";
    }

    public string Name { get; }

    public string Category { get; }

    public Type? ElementType { get; }

    public string? IconData { get; }

    public string Description { get; }

    public AssetCompositeHierarchyData<UIElementDesign, UIElement> Create(UIAssetBase targetAsset)
    {
        if (library != null)
        {
            var libAsset = (UILibraryAsset)library.Asset;
            return libAsset.CreateElementInstance(targetAsset, library.Url, libraryElementId);
        }

        var element = createFunc != null
            ? createFunc()
            : (UIElement)Activator.CreateInstance(ElementType ?? typeof(Canvas))!;

        element.Id = Guid.NewGuid();
        element.Name = Name;

        var design = new UIElementDesign(element);
        var hierarchyData = new AssetCompositeHierarchyData<UIElementDesign, UIElement>();
        hierarchyData.RootParts.Add(element);
        hierarchyData.Parts.Add(design);
        return hierarchyData;
    }

    public static List<IUIElementFactory> CreatePanelFactories()
    {
        return
        [
            new UIElementFactoryViewModel(
                "Canvas",
                "Panels",
                typeof(Canvas),
                "M19,3H5C3.89,3 3,3.89 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5C21,3.89 20.1,3 19,3M19,5V19H5V5H19Z",
                () => new Canvas(),
                "Freeform absolute and relative positioning canvas"),

            new UIElementFactoryViewModel(
                "Grid",
                "Panels",
                typeof(Grid),
                "M3,3H11V11H3V3M13,3H21V11H13V3M3,13H11V21H3V13M13,13H21V21H13V13Z",
                () =>
                {
                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new StripDefinition { Type = StripType.Star });
                    grid.RowDefinitions.Add(new StripDefinition { Type = StripType.Star });
                    return grid;
                },
                "Flexible grid with rows, columns and layers"),

            new UIElementFactoryViewModel(
                "StackPanel",
                "Panels",
                typeof(StackPanel),
                "M3,4H21V8H3V4M3,10H21V14H3V10M3,16H21V20H3V16Z",
                () => new StackPanel { Orientation = Orientation.Vertical },
                "Linear stack panel (vertical, horizontal or depth)"),

            new UIElementFactoryViewModel(
                "UniformGrid",
                "Panels",
                typeof(UniformGrid),
                "M4,4H9V9H4V4M10,4H15V9H10V4M16,4H21V9H16V4M4,10H9V15H4V10M10,10H15V15H10V10M16,10H21V15H16V10M4,16H9V21H4V16M10,16H15V21H10V16M16,16H21V21H16V16Z",
                () => new UniformGrid { Columns = 2, Rows = 2 },
                "Uniform grid where all cells have equal size"),
        ];
    }

    public static List<IUIElementFactory> CreateStandardFactories()
    {
        var list = new List<IUIElementFactory>();

        // Panels
        list.AddRange(CreatePanelFactories());

        // Controls
        list.Add(new UIElementFactoryViewModel(
            "Button",
            "Controls",
            typeof(Button),
            "M19 6H5c-1.1 0-2 .9-2 2v8c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2zm0 10H5V8h14v8z",
            () => new Button { Content = new TextBlock { Text = "Button" } },
            "Standard clickable button control"));

        list.Add(new UIElementFactoryViewModel(
            "ToggleButton",
            "Controls",
            typeof(ToggleButton),
            "M17 7H7c-2.76 0-5 2.24-5 5s2.24 5 5 5h10c2.76 0 5-2.24 5-5s-2.24-5-5-5zm0 8c-1.66 0-3-1.34-3-3s1.34-3 3-3 3 1.34 3 3-1.34 3-3 3z",
            () => new ToggleButton { Content = new TextBlock { Text = "Toggle" } },
            "Two-state button control"));

        list.Add(new UIElementFactoryViewModel(
            "ImageButton",
            "Controls",
            typeof(ImageButton),
            "M21 19V5c0-1.1-.9-2-2-2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2zM8.5 13.5l2.5 3.01L14.5 12l4.5 6H5l3.5-4.5z",
            () => new ImageButton(),
            "Button with image content"));

        list.Add(new UIElementFactoryViewModel(
            "Border",
            "Controls",
            typeof(Border),
            "M19,3H5C3.89,3 3,3.89 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5C21,3.89 20.1,3 19,3M19,19H5V5H19V19Z",
            () => new Border(),
            "Border decorator around an element"));

        list.Add(new UIElementFactoryViewModel(
            "Slider",
            "Controls",
            typeof(Slider),
            "M4 10h16v4H4v-4zm6-6h4v16h-4V4z",
            () => new Slider { Minimum = 0, Maximum = 100, Value = 50 },
            "Interactive range slider"));

        list.Add(new UIElementFactoryViewModel(
            "ScrollViewer",
            "Controls",
            typeof(ScrollViewer),
            "M3 5v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2H5c-1.1 0-2 .9-2 2zm14 2h2v10h-2V7zm-4 12H5V5h10v14z",
            () => new ScrollViewer(),
            "Scrollable viewport container"));

        list.Add(new UIElementFactoryViewModel(
            "ScrollBar",
            "Controls",
            typeof(ScrollBar),
            "M17 2H7c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h10c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm-2 18h-6V4h6v16z",
            () => new ScrollBar(),
            "Standalone scrollbar control"));

        list.Add(new UIElementFactoryViewModel(
            "ModalElement",
            "Controls",
            typeof(ModalElement),
            "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-2 10H7v-2h10v2z",
            () => new ModalElement(),
            "Modal popup container"));

        // Text & Input
        list.Add(new UIElementFactoryViewModel(
            "TextBlock",
            "Text & Input",
            typeof(TextBlock),
            "M5,4V7H10.5V19H13.5V7H19V4H5Z",
            () => new TextBlock { Text = "Text Block" },
            "Displays read-only text"));

        list.Add(new UIElementFactoryViewModel(
            "ScrollingText",
            "Text & Input",
            typeof(ScrollingText),
            "M19,7H5V5H19M21,11H3V9H21M17,15H7V13H17M15,19H9V17H15Z",
            () => new ScrollingText { Text = "Scrolling Text" },
            "Text element with automatic scrolling marquee"));

        list.Add(new UIElementFactoryViewModel(
            "EditText",
            "Text & Input",
            typeof(EditText),
            "M20.71,7.04C21.1,6.65 21.1,6 20.71,5.63L18.37,3.29C18,2.9 17.35,2.9 16.96,3.29L15.12,5.12L18.87,8.87M3,17.25V21H6.75L17.81,9.93L14.07,6.19L3,17.25Z",
            () => new EditText { Text = "Editable text" },
            "Interactive text entry field"));

        // Media
        list.Add(new UIElementFactoryViewModel(
            "ImageElement",
            "Media",
            typeof(ImageElement),
            "M8.5,13.5L11,16.5L14.5,12L19,18H5M21,19V5C21,3.89 20.1,3 19,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19Z",
            () => new ImageElement(),
            "Displays a 2D texture or sprite"));

        // Content
        list.Add(new UIElementFactoryViewModel(
            "ContentPresenter",
            "Content",
            typeof(ContentPresenter),
            "M4,5H20V7H4V5M4,9H20V11H4V9M4,13H20V15H4V13M4,17H20V19H4V17Z",
            () => new ContentPresenter(),
            "Presents dynamic content"));

        list.Add(new UIElementFactoryViewModel(
            "ContentDecorator",
            "Content",
            typeof(ContentDecorator),
            "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4Z",
            () => new ContentDecorator(),
            "Decorates content elements"));

        return list;
    }
}
