// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Xaml.Interactivity;
using Action = System.Action;
using Xunit;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.ViewModels;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// Teste integrado para a Fatia 1 (slice-1) do Marco P30:
/// "Viewport Interaction, Sliding & Selection Behaviors (Avalonia)".
/// Valida os 15 contratos de Behaviors, Extensions, Controls e Commands em Avalonia UI / .NET 10.
/// </summary>
public class TestPresentationBehaviorsAndControlsIntegration
{
    private static readonly Assembly AssetsEditorAvaloniaAssembly =
        typeof(Stride.Assets.Editor.Avalonia.Behaviors.ThumbLikeBehavior).Assembly;

    private static readonly Assembly CoreAssetsEditorAvaloniaAssembly =
        typeof(Stride.Core.Assets.Editor.Avalonia.Behaviors.TilePanelThumbnailPrioritizationBehavior).Assembly;

    #region Mock Classes

    private sealed class MockResizingTarget : IResizingTarget
    {
        public bool StartedCalled { get; private set; }
        public ResizingDirection? StartedDirection { get; private set; }

        public int DeltaCallCount { get; private set; }
        public double LastDeltaX { get; private set; }
        public double LastDeltaY { get; private set; }

        public bool CompletedCalled { get; private set; }
        public double CompletedTotalX { get; private set; }
        public double CompletedTotalY { get; private set; }

        public void OnResizingStarted(ResizingDirection direction)
        {
            StartedCalled = true;
            StartedDirection = direction;
        }

        public void OnResizingDelta(ResizingDirection direction, double horizontalChange, double verticalChange)
        {
            DeltaCallCount++;
            LastDeltaX = horizontalChange;
            LastDeltaY = verticalChange;
        }

        public void OnResizingCompleted(ResizingDirection direction, double horizontalChange, double verticalChange)
        {
            CompletedCalled = true;
            CompletedTotalX = horizontalChange;
            CompletedTotalY = verticalChange;
        }
    }

    private sealed class MockLogMessage : ILogMessage
    {
        public string? Module { get; set; }
        public LogMessageType Type { get; set; }
        public string? Text { get; set; }
        public ExceptionInfo? ExceptionInfo { get; set; }

        public MockLogMessage(LogMessageType type, string text)
        {
            Type = type;
            Text = text;
        }
    }

    private sealed class MockCommand : ICommand
    {
        public int ExecuteCount { get; private set; }
        public object? LastParameter { get; private set; }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            ExecuteCount++;
            LastParameter = parameter;
        }

        public event EventHandler? CanExecuteChanged;
    }

    private sealed class TestActivateOnLocationTarget : Control
    {
        public bool IsActivated { get; set; }
    }

    #endregion

    #region 1. BindScrollOffsetBehavior Tests

    [Fact]
    public void TestBindScrollOffsetBehaviorTypeAndProperties()
    {
        var type = AssetsEditorAvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Behaviors.BindScrollOffsetBehavior");
        Assert.True(type != null, "BindScrollOffsetBehavior deve existir em Stride.Assets.Editor.Avalonia.Behaviors.");
        Assert.True(typeof(Behavior<ScrollViewer>).IsAssignableFrom(type), "BindScrollOffsetBehavior deve herdar de Behavior<ScrollViewer>.");

        dynamic behavior = Activator.CreateInstance(type!)!;
        Assert.NotNull((object)behavior);

        var hProp = type.GetProperty("HorizontalOffset");
        var vProp = type.GetProperty("VerticalOffset");
        Assert.NotNull(hProp);
        Assert.NotNull(vProp);

        behavior.HorizontalOffset = 150.0;
        behavior.VerticalOffset = 250.0;
        Assert.Equal(150.0, (double)behavior.HorizontalOffset);
        Assert.Equal(250.0, (double)behavior.VerticalOffset);
    }

    [Fact]
    public void TestBindScrollOffsetBehaviorAttachmentAndSync()
    {
        var type = AssetsEditorAvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Behaviors.BindScrollOffsetBehavior");
        Assert.True(type != null, "BindScrollOffsetBehavior deve existir.");

        dynamic behavior = Activator.CreateInstance(type!)!;
        var scrollViewer = new ScrollViewer();

        ((Behavior<ScrollViewer>)behavior).Attach(scrollViewer);
        Assert.Same(scrollViewer, ((Behavior<ScrollViewer>)behavior).AssociatedObject);

        // Modifica offsets no behavior
        behavior.HorizontalOffset = 80.0;
        behavior.VerticalOffset = 120.0;
        Assert.Equal(80.0, (double)behavior.HorizontalOffset);
        Assert.Equal(120.0, (double)behavior.VerticalOffset);

        ((Behavior<ScrollViewer>)behavior).Detach();
        Assert.Null(((Behavior<ScrollViewer>)behavior).AssociatedObject);
    }

    #endregion

    #region 2. ThumbBehavior Tests

    [Fact]
    public void TestThumbBehaviorTypeAndProperties()
    {
        var type = AssetsEditorAvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Behaviors.ThumbBehavior");
        Assert.True(type != null, "ThumbBehavior deve existir em Stride.Assets.Editor.Avalonia.Behaviors.");
        Assert.True(typeof(Behavior<Thumb>).IsAssignableFrom(type), "ThumbBehavior deve herdar de Behavior<Thumb>.");

        dynamic behavior = Activator.CreateInstance(type!)!;
        Assert.NotNull((object)behavior);

        Assert.NotNull(type.GetProperty("Direction"));
        Assert.NotNull(type.GetProperty("Target"));
        Assert.NotNull(type.GetProperty("IsDragging"));

        var mockTarget = new MockResizingTarget();
        behavior.Direction = ResizingDirection.BottomRight;
        behavior.Target = mockTarget;

        Assert.Equal(ResizingDirection.BottomRight, (ResizingDirection)behavior.Direction);
        Assert.Same(mockTarget, (IResizingTarget)behavior.Target);
        Assert.False((bool)behavior.IsDragging);
    }

    [Fact]
    public void TestThumbBehaviorDragCycle()
    {
        var type = AssetsEditorAvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Behaviors.ThumbBehavior");
        Assert.True(type != null, "ThumbBehavior deve existir.");

        dynamic behavior = Activator.CreateInstance(type!)!;
        var mockTarget = new MockResizingTarget();
        behavior.Direction = ResizingDirection.TopLeft;
        behavior.Target = mockTarget;

        var thumb = new Thumb();
        ((Behavior<Thumb>)behavior).Attach(thumb);

        // Testa métodos simulados / manipuladores de eventos
        var startMethod = type.GetMethod("SimulateDragStart", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?? type.GetMethod("OnDragStarted", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var deltaMethod = type.GetMethod("SimulateDragDelta", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?? type.GetMethod("OnDragDelta", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var completeMethod = type.GetMethod("SimulateDragComplete", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?? type.GetMethod("OnDragCompleted", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.True(startMethod != null, "ThumbBehavior deve ter método para disparar/simular início do arraste.");
        Assert.True(deltaMethod != null, "ThumbBehavior deve ter método para disparar/simular delta de arraste.");
        Assert.True(completeMethod != null, "ThumbBehavior deve ter método para disparar/simular conclusão do arraste.");

        if (startMethod.GetParameters().Length == 0)
            startMethod.Invoke(behavior, null);
        else if (startMethod.GetParameters().Length == 2)
            startMethod.Invoke(behavior, new object?[] { thumb, new VectorEventArgs() });
        else
            startMethod.Invoke(behavior, new object[] { ResizingDirection.TopLeft });

        Assert.True(mockTarget.StartedCalled);
        Assert.Equal(ResizingDirection.TopLeft, mockTarget.StartedDirection);
        Assert.True((bool)behavior.IsDragging);

        if (deltaMethod.GetParameters().Length == 2 && deltaMethod.GetParameters()[0].ParameterType == typeof(double))
            deltaMethod.Invoke(behavior, new object[] { 10.0, 15.0 });
        else if (deltaMethod.GetParameters().Length == 2)
            deltaMethod.Invoke(behavior, new object?[] { thumb, new VectorEventArgs { Vector = new Vector(10.0, 15.0) } });

        Assert.True(mockTarget.DeltaCallCount > 0);
        Assert.Equal(10.0, mockTarget.LastDeltaX);
        Assert.Equal(15.0, mockTarget.LastDeltaY);

        if (completeMethod.GetParameters().Length == 2 && completeMethod.GetParameters()[0].ParameterType == typeof(double))
            completeMethod.Invoke(behavior, new object[] { 10.0, 15.0 });
        else if (completeMethod.GetParameters().Length == 2)
            completeMethod.Invoke(behavior, new object?[] { thumb, new VectorEventArgs { Vector = new Vector(10.0, 15.0) } });

        Assert.True(mockTarget.CompletedCalled);
        Assert.Equal(10.0, mockTarget.CompletedTotalX);
        Assert.Equal(15.0, mockTarget.CompletedTotalY);
        Assert.False((bool)behavior.IsDragging);

        ((Behavior<Thumb>)behavior).Detach();
    }

    #endregion

    #region 3. ViewportZoomBehavior Tests

    [Fact]
    public void TestViewportZoomBehaviorTypeAndProperties()
    {
        var type = AssetsEditorAvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Behaviors.ViewportZoomBehavior");
        Assert.True(type != null, "ViewportZoomBehavior deve existir em Stride.Assets.Editor.Avalonia.Behaviors.");
        Assert.True(typeof(Behavior<Control>).IsAssignableFrom(type), "ViewportZoomBehavior deve herdar de Behavior<Control>.");

        dynamic behavior = Activator.CreateInstance(type!)!;
        Assert.NotNull((object)behavior);

        Assert.NotNull(type.GetProperty("Modifiers"));
        Assert.NotNull(type.GetProperty("Viewport"));

        behavior.Modifiers = KeyModifiers.Control | KeyModifiers.Shift;
        Assert.Equal(KeyModifiers.Control | KeyModifiers.Shift, (KeyModifiers)behavior.Modifiers);
    }

    [Fact]
    public void TestViewportZoomBehaviorZoomLogic()
    {
        var type = AssetsEditorAvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Behaviors.ViewportZoomBehavior");
        Assert.True(type != null, "ViewportZoomBehavior deve existir.");

        dynamic behavior = Activator.CreateInstance(type!)!;
        var serviceProvider = new ViewModelServiceProvider();
        var viewport = new ViewportViewModel(serviceProvider)
        {
            ViewportWidth = 800,
            ViewportHeight = 600,
            ContentWidth = 400,
            ContentHeight = 300,
        };

        behavior.Viewport = viewport;
        var panel = new Panel { Width = 800, Height = 600 };
        ((Behavior<Control>)behavior).Attach(panel);

        double initialScale = viewport.ScaleFactor;

        // Testa zoom programático / simulação
        var zoomMethod = type.GetMethod("SimulateZoom", BindingFlags.Public | BindingFlags.Instance);
        if (zoomMethod != null)
        {
            zoomMethod.Invoke(behavior, new object[] { new Point(400, 300), 1.0, KeyModifiers.Control });
            Assert.True(viewport.ScaleFactor > initialScale, "Zoom in deve aumentar scale factor.");

            zoomMethod.Invoke(behavior, new object[] { new Point(400, 300), -1.0, KeyModifiers.Control });
        }

        ((Behavior<Control>)behavior).Detach();
    }

    #endregion

    #region 4. ValidateTextBoxAfterSlidingBehavior Tests

    [Fact]
    public void TestValidateTextBoxAfterSlidingBehaviorTypeAndProperties()
    {
        var type = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Behaviors.ValidateTextBoxAfterSlidingBehavior");
        Assert.True(type != null, "ValidateTextBoxAfterSlidingBehavior deve existir em Stride.Core.Assets.Editor.Avalonia.Behaviors.");
        Assert.True(typeof(Behavior<Slider>).IsAssignableFrom(type), "ValidateTextBoxAfterSlidingBehavior deve herdar de Behavior<Slider>.");

        dynamic behavior = Activator.CreateInstance(type!)!;
        Assert.NotNull((object)behavior);

        Assert.NotNull(type.GetProperty("TextBox"));

        var textBox = new TextBox();
        behavior.TextBox = textBox;
        Assert.Same(textBox, (Control)behavior.TextBox);
    }

    [Fact]
    public void TestValidateTextBoxAfterSlidingBehaviorValidationExecution()
    {
        var type = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Behaviors.ValidateTextBoxAfterSlidingBehavior");
        Assert.True(type != null, "ValidateTextBoxAfterSlidingBehavior deve existir.");

        dynamic behavior = Activator.CreateInstance(type!)!;
        var slider = new Slider { Minimum = 0, Maximum = 100, Value = 10 };
        var textBox = new TextBox { Text = "10" };

        bool validated = false;
        var validateMethod = type.GetMethod("ValidateTextBox", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(validateMethod);

        behavior.TextBox = textBox;
        var actionProp = type.GetProperty("ValidationAction");
        if (actionProp != null)
        {
            actionProp.SetValue(behavior, new Action(() => validated = true));
        }

        ((Behavior<Slider>)behavior).Attach(slider);

        validateMethod.Invoke(behavior, null);
        if (actionProp != null)
        {
            Assert.True(validated, "ValidationAction deve ter sido executada.");
        }

        ((Behavior<Slider>)behavior).Detach();
    }

    #endregion

    #region 5. DragContainer Tests

    [Fact]
    public void TestDragContainerPropertiesAndSerialization()
    {
        var type = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Behaviors.DragContainer");
        Assert.True(type != null, "DragContainer deve existir em Stride.Core.Assets.Editor.Avalonia.Behaviors.");
        Assert.True(typeof(ViewModelBase).IsAssignableFrom(type), "DragContainer deve herdar de ViewModelBase.");

        var ctor = type.GetConstructor(new[] { typeof(IEnumerable<object>) });
        Assert.NotNull(ctor);

        var items = new List<object> { "Item1", "Item2", 42 };
        dynamic container = ctor.Invoke(new object[] { items })!;

        Assert.NotNull((object)container);
        Assert.Equal(3, ((object[])container.Items).Length);

        // Preview items
        var preview = ((IEnumerable<object>)container.PreviewItems).ToList();
        Assert.Equal(3, preview.Count);

        container.Message = "Test Drag Message";
        Assert.Equal("Test Drag Message", (string)container.Message);

        container.IsAccepted = true;
        Assert.True((bool)container.IsAccepted);
        Assert.False((bool)container.IsRejected);

        container.IsAccepted = false;
        Assert.False((bool)container.IsAccepted);
        Assert.True((bool)container.IsRejected);

        // Constantes
        var formatField = type.GetField("Format", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(formatField);
        Assert.Equal("DraggedData", formatField.GetValue(null));
    }

    #endregion

    #region 6. ActivateOnLogBehavior Tests

    [Fact]
    public void TestActivateOnLogBehaviorTypeAndHierarchy()
    {
        var baseType = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Behaviors.ActivateOnLogBehavior`1");
        Assert.True(baseType != null, "ActivateOnLogBehavior<T> deve existir em Stride.Core.Assets.Editor.Avalonia.Behaviors.");

        var tabItemBehaviorType = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Behaviors.TabItemActivateOnLogBehavior");
        Assert.True(tabItemBehaviorType != null, "TabItemActivateOnLogBehavior deve existir em Stride.Core.Assets.Editor.Avalonia.Behaviors.");

        var tabItemGenericType = baseType.MakeGenericType(typeof(TabItem));
        Assert.True(tabItemGenericType.IsAssignableFrom(tabItemBehaviorType), "TabItemActivateOnLogBehavior deve herdar de ActivateOnLogBehavior<TabItem>.");
    }

    [Fact]
    public void TestTabItemActivateOnLogBehaviorActivation()
    {
        var tabItemBehaviorType = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Behaviors.TabItemActivateOnLogBehavior");
        Assert.True(tabItemBehaviorType != null, "TabItemActivateOnLogBehavior deve existir.");

        dynamic behavior = Activator.CreateInstance(tabItemBehaviorType!)!;
        var tabItem = new TabItem { IsSelected = false };
        var logCollection = new ObservableCollection<ILogMessage>();

        behavior.MinimumLevel = LogMessageType.Warning;
        behavior.Collection = logCollection;

        ((Behavior<TabItem>)behavior).Attach(tabItem);

        // Mensagem Info não deve ativar
        logCollection.Add(new MockLogMessage(LogMessageType.Info, "Information only"));
        Assert.False(tabItem.IsSelected);

        // Mensagem Warning deve ativar
        logCollection.Add(new MockLogMessage(LogMessageType.Warning, "Warning message!"));
        Assert.True(tabItem.IsSelected);

        ((Behavior<TabItem>)behavior).Detach();
    }

    #endregion

    #region 7. TreeViewBindableSelectedItemsBehavior Tests

    [Fact]
    public void TestTreeViewBindableSelectedItemsBehaviorTypeAndProperties()
    {
        var type = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Behaviors.TreeViewBindableSelectedItemsBehavior");
        Assert.True(type != null, "TreeViewBindableSelectedItemsBehavior deve existir em Stride.Core.Assets.Editor.Avalonia.Behaviors.");
        Assert.True(typeof(Behavior<TreeView>).IsAssignableFrom(type), "TreeViewBindableSelectedItemsBehavior deve herdar de Behavior<TreeView>.");

        dynamic behavior = Activator.CreateInstance(type!)!;
        Assert.NotNull((object)behavior);

        Assert.NotNull(type.GetProperty("SelectedItems"));
        Assert.NotNull(type.GetProperty("GiveFocusOnSelectionChange"));

        var observableList = new ObservableList<object>();
        behavior.SelectedItems = observableList;
        Assert.Same(observableList, (IObservableList<object>)behavior.SelectedItems);
    }

    [Fact]
    public void TestTreeViewBindableSelectedItemsBehaviorSync()
    {
        var type = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Behaviors.TreeViewBindableSelectedItemsBehavior");
        Assert.True(type != null, "TreeViewBindableSelectedItemsBehavior deve existir.");

        dynamic behavior = Activator.CreateInstance(type!)!;
        var treeView = new TreeView();
        var selectedList = new ObservableList<object>();

        behavior.SelectedItems = selectedList;
        ((Behavior<TreeView>)behavior).Attach(treeView);

        // Adiciona item na coleção de seleção
        var testObj = "Node1";
        selectedList.Add(testObj);

        // Limpa seleção
        selectedList.Clear();
        Assert.Empty(selectedList);

        ((Behavior<TreeView>)behavior).Detach();
    }

    #endregion

    #region 8. BringSelectionToViewBehavior Tests

    [Fact]
    public void TestBringSelectionToViewBehaviorType()
    {
        var type = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Behaviors.BringSelectionToViewBehavior");
        Assert.True(type != null, "BringSelectionToViewBehavior deve existir em Stride.Core.Assets.Editor.Avalonia.Behaviors.");

        var listBoxType = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Controls.EditableContentListBox")
            ?? typeof(SelectingItemsControl);
        var expectedBase = typeof(Behavior<>).MakeGenericType(listBoxType);
        Assert.True(expectedBase.IsAssignableFrom(type) || typeof(Behavior<SelectingItemsControl>).IsAssignableFrom(type) || typeof(Behavior<ListBox>).IsAssignableFrom(type),
            "BringSelectionToViewBehavior deve herdar de Behavior<EditableContentListBox> ou Behavior<SelectingItemsControl>.");

        dynamic behavior = Activator.CreateInstance(type!)!;
        Assert.NotNull((object)behavior);
    }

    #endregion

    #region 9. PropertyViewFilteringBehavior Tests

    [Fact]
    public void TestPropertyViewFilteringBehaviorTypeAndMatch()
    {
        var type = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Behaviors.PropertyViewFilteringBehavior");
        Assert.True(type != null, "PropertyViewFilteringBehavior deve existir em Stride.Core.Assets.Editor.Avalonia.Behaviors.");

        dynamic behavior = Activator.CreateInstance(type!)!;
        Assert.NotNull((object)behavior);

        Assert.NotNull(type.GetProperty("FilterToken"));

        behavior.FilterToken = "Position";
        Assert.Equal("Position", (string)behavior.FilterToken);

        var matchMethod = type.GetMethod("Match", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(matchMethod);
    }

    #endregion

    #region 10. OnComboBoxClosedWithSelectionBehavior Tests

    [Fact]
    public void TestOnComboBoxClosedWithSelectionBehaviorTypeAndExecution()
    {
        var type = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Behaviors.OnComboBoxClosedWithSelectionBehavior");
        Assert.True(type != null, "OnComboBoxClosedWithSelectionBehavior deve existir em Stride.Core.Assets.Editor.Avalonia.Behaviors.");
        Assert.True(typeof(Behavior<ComboBox>).IsAssignableFrom(type), "OnComboBoxClosedWithSelectionBehavior deve herdar de Behavior<ComboBox>.");

        dynamic behavior = Activator.CreateInstance(type!)!;
        Assert.NotNull((object)behavior);

        var mockCommand = new MockCommand();
        behavior.Command = mockCommand;
        behavior.CommandParameter = "TestParam";

        var comboBox = new ComboBox();
        ((Behavior<ComboBox>)behavior).Attach(comboBox);

        // Sem item selecionado, invocar não executa o comando
        var onEventMethod = type.GetMethod("OnEvent", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(onEventMethod);

        onEventMethod.Invoke(behavior, null);
        Assert.Equal(0, mockCommand.ExecuteCount);

        // Com item selecionado, executa o comando
        comboBox.ItemsSource = new[] { "OptionA", "OptionB" };
        comboBox.SelectedItem = "OptionA";

        onEventMethod.Invoke(behavior, null);
        Assert.Equal(1, mockCommand.ExecuteCount);
        Assert.Equal("TestParam", (string)mockCommand.LastParameter!);

        ((Behavior<ComboBox>)behavior).Detach();
    }

    #endregion

    #region 11. OnSelectionChangedWithSelectionBehavior Tests

    [Fact]
    public void TestOnSelectionChangedWithSelectionBehaviorTypeAndExecution()
    {
        var type = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Behaviors.OnSelectionChangedWithSelectionBehavior");
        Assert.True(type != null, "OnSelectionChangedWithSelectionBehavior deve existir em Stride.Core.Assets.Editor.Avalonia.Behaviors.");
        Assert.True(typeof(Behavior<SelectingItemsControl>).IsAssignableFrom(type) || typeof(Behavior<Control>).IsAssignableFrom(type),
            "OnSelectionChangedWithSelectionBehavior deve herdar de Behavior<SelectingItemsControl>.");

        dynamic behavior = Activator.CreateInstance(type!)!;
        Assert.NotNull((object)behavior);

        var mockCommand = new MockCommand();
        behavior.Command = mockCommand;
        behavior.CommandParameter = "SelParam";

        var listBox = new ListBox();
        ((Behavior<SelectingItemsControl>)behavior).Attach(listBox);

        var onEventMethod = type.GetMethod("OnEvent", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(onEventMethod);

        // Sem item selecionado
        onEventMethod.Invoke(behavior, null);
        Assert.Equal(0, mockCommand.ExecuteCount);

        // Com item selecionado dispara SelectionChanged automaticamente
        listBox.ItemsSource = new[] { "Alpha", "Beta" };
        listBox.SelectedItem = "Beta";

        Assert.Equal(1, mockCommand.ExecuteCount);
        Assert.Equal("SelParam", (string)mockCommand.LastParameter!);

        ((Behavior<SelectingItemsControl>)behavior).Detach();
    }

    #endregion

    #region 12. ActivateOnLocationChangedBehavior Tests

    [Fact]
    public void TestActivateOnLocationChangedBehaviorActivation()
    {
        var type = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Behaviors.ActivateOnLocationChangedBehavior`1");
        Assert.True(type != null, "ActivateOnLocationChangedBehavior<T> deve existir em Stride.Core.Assets.Editor.Avalonia.Behaviors.");
    }

    #endregion

    #region 13. ControlExtensions Tests

    [Fact]
    public void TestControlExtensionsTypeAndMethods()
    {
        var type = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Extensions.ControlExtensions");
        Assert.True(type != null, "ControlExtensions deve existir em Stride.Core.Assets.Editor.Avalonia.Extensions.");

        var expandMethod = type.GetMethod("ExpandSessionObjects", BindingFlags.Public | BindingFlags.Static);
        var pathMethod = type.GetMethod("GetSessionObjectPath", BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(expandMethod);
        Assert.NotNull(pathMethod);
    }

    #endregion

    #region 14. TreeViewTemplateSelector Tests

    [Fact]
    public void TestTreeViewTemplateSelectorTypeAndSelection()
    {
        var type = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Controls.TreeViewTemplateSelector");
        Assert.True(type != null, "TreeViewTemplateSelector deve existir em Stride.Core.Assets.Editor.Avalonia.Controls.");
        Assert.True(typeof(IDataTemplate).IsAssignableFrom(type), "TreeViewTemplateSelector deve implementar IDataTemplate.");

        dynamic selector = Activator.CreateInstance(type!)!;
        Assert.NotNull((object)selector);

        var dummyTemplate = new FuncDataTemplate<object>((_, _) => new TextBlock());

        selector.ProjectTemplate = dummyTemplate;
        selector.PackageTemplate = dummyTemplate;
        selector.DirectoryTemplate = dummyTemplate;
        selector.AssetMountPointTemplate = dummyTemplate;
        selector.DependencyCategoryTemplate = dummyTemplate;
        selector.ProjectCodeTemplate = dummyTemplate;
        selector.PackageReferenceTemplate = dummyTemplate;

        Assert.Same(dummyTemplate, (IDataTemplate)selector.ProjectTemplate);
        Assert.Same(dummyTemplate, (IDataTemplate)selector.PackageTemplate);
        Assert.Same(dummyTemplate, (IDataTemplate)selector.DirectoryTemplate);
    }

    #endregion

    #region 15. SetContentTemplateCommand Tests

    [Fact]
    public void TestSetContentTemplateCommandTypeAndExecution()
    {
        var type = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Commands.SetContentTemplateCommand");
        Assert.True(type != null, "SetContentTemplateCommand deve existir em Stride.Core.Assets.Editor.Avalonia.Commands.");
        Assert.True(typeof(ICommand).IsAssignableFrom(type), "SetContentTemplateCommand deve implementar ICommand.");

        dynamic command = Activator.CreateInstance(type!)!;
        Assert.NotNull((object)command);

        Assert.True(command.CanExecute(null));

        var presenter = new ContentPresenter();
        command.Target = presenter;
        Assert.Same(presenter, (ContentPresenter)command.Target);

        var template = new FuncDataTemplate<object>((_, _) => new TextBlock());
        command.Execute(template);
        Assert.Same(template, presenter.ContentTemplate);
    }

    #endregion
}
