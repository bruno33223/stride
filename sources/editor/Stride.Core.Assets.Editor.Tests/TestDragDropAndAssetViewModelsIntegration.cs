// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Stride.Assets.Presentation.Extensions;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Extensions;
using Stride.Core.Assets.Editor.View.Behaviors;
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;
using InsertPosition = Stride.Core.Assets.Editor.View.Behaviors.InsertPosition;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests
{
    public class TestDragDropAndAssetViewModelsIntegration
    {
        private static readonly Assembly CoreAssetsEditorAssembly = typeof(Stride.Core.Assets.Editor.ViewModels.SessionViewModel).Assembly;
        private static readonly Assembly AssetsPresentationAssembly = typeof(Stride.Assets.Presentation.ViewModels.ModelViewModel).Assembly;
        private static readonly Assembly CoreAssetsEditorAvaloniaAssembly = typeof(Stride.Core.Assets.Editor.View.Behaviors.TreeViewDragDropBehavior).Assembly;

        [Fact]
        public void Contract01_DeletedPartsTrackingOperation_ExistsAndValid()
        {
            var type = CoreAssetsEditorAssembly.GetType("Stride.Core.Assets.Editor.ViewModel.DeletedPartsTrackingOperation`2");
            Assert.True(type != null, "Contrato 1 Falhou: Tipo Stride.Core.Assets.Editor.ViewModel.DeletedPartsTrackingOperation`2 não foi encontrado.");

            Assert.True(type.IsClass, "DeletedPartsTrackingOperation deve ser uma classe.");
            Assert.False(type.IsAbstract, "DeletedPartsTrackingOperation não deve ser abstrata.");

            var baseType = type.BaseType;
            Assert.NotNull(baseType);
            Assert.Equal("DirtyingOperation", baseType.Name);

            var undoMethod = type.GetMethod("Undo", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.NotNull(undoMethod);

            var redoMethod = type.GetMethod("Redo", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.NotNull(redoMethod);

            var freezeMethod = type.GetMethod("FreezeContent", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.NotNull(freezeMethod);

            // Verifica parâmetros do construtor
            var constructors = type.GetConstructors();
            Assert.Single(constructors);
            var parameters = constructors[0].GetParameters();
            Assert.Equal(2, parameters.Length);
            Assert.StartsWith("AssetCompositeHierarchyViewModel", parameters[0].ParameterType.Name);
            Assert.Equal(typeof(HashSet<Tuple<Guid, Guid>>), parameters[1].ParameterType);
        }

        [Fact]
        public void Contract02_CodeAssetViewModel_ExistsAndValid()
        {
            var genericType = AssetsPresentationAssembly.GetType("Stride.Assets.Presentation.ViewModel.CodeAssetViewModel`1");
            Assert.True(genericType != null, "Contrato 2 Falhou: Tipo Stride.Assets.Presentation.ViewModel.CodeAssetViewModel`1 não foi encontrado.");
            Assert.True(genericType.IsAbstract, "CodeAssetViewModel`1 deve ser abstrata.");

            var nonGenericType = AssetsPresentationAssembly.GetType("Stride.Assets.Presentation.ViewModel.CodeAssetViewModel");
            Assert.True(nonGenericType != null, "Contrato 2 Falhou: Tipo Stride.Assets.Presentation.ViewModel.CodeAssetViewModel não foi encontrado.");
            Assert.False(nonGenericType.IsAbstract, "CodeAssetViewModel não deve ser abstrata.");

            // Verifica o atributo de ViewModel
            var attr = nonGenericType.GetCustomAttribute<AssetViewModelAttribute>();
            Assert.NotNull(attr);
            Assert.Equal(typeof(SourceCodeAsset), attr.AssetType);

            var fullPathProp = genericType.GetProperty("FullPath", BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(fullPathProp);

            var onSessionSavedMethod = genericType.GetMethod("OnSessionSaved", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(onSessionSavedMethod);
        }

        [Fact]
        public void Contract03_AddAssetTemplateCollectionViewModel_ExistsAndValid()
        {
            var type = CoreAssetsEditorAssembly.GetType("Stride.Core.Assets.Editor.Components.AddAssets.AddAssetTemplateCollectionViewModel");
            Assert.True(type != null, "Contrato 3 Falhou: Tipo Stride.Core.Assets.Editor.Components.AddAssets.AddAssetTemplateCollectionViewModel não foi encontrado.");

            Assert.NotNull(type.BaseType);
            Assert.Equal("AddItemTemplateCollectionViewModel", type.BaseType.Name);

            var currDirProp = type.GetProperty("CurrentDirectory", BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(currDirProp);

            var targetDirProp = type.GetProperty("TargetDirectory", BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(targetDirProp);

            var updateNameMethod = type.GetMethod("UpdateNameFromSelectedTemplate", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(updateNameMethod);
        }

        [Fact]
        public void Contract04_UpdatePackageTemplateCollectionViewModel_ExistsAndValid()
        {
            var type = CoreAssetsEditorAssembly.GetType("Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels.UpdatePackageTemplateCollectionViewModel");
            Assert.True(type != null, "Contrato 4 Falhou: Tipo Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels.UpdatePackageTemplateCollectionViewModel não foi encontrado.");

            Assert.NotNull(type.BaseType);
            Assert.Equal("ProjectTemplateCollectionViewModel", type.BaseType.Name);

            var rootGroupsProp = type.GetProperty("RootGroups", BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(rootGroupsProp);

            var updateNameMethod = type.GetMethod("UpdateNameFromSelectedTemplate", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(updateNameMethod);
        }

        [Fact]
        public void Contract05_ListBoxDragDropBehavior_ExistsAndValid()
        {
            var behavior = new ListBoxDragDropBehavior();
            Assert.NotNull(behavior);
            Assert.True(behavior.CanDrag);
            Assert.True(behavior.CanDrop);
            Assert.False(behavior.CanInsert);

            // Testa threshold de inserção
            var item = new ListBoxItem { Height = 100 };
            item.Measure(new Size(100, 100));
            item.Arrange(new Rect(0, 0, 100, 100));

            // Mouse no topo (Y = 2) -> Before
            var targetBefore = behavior.GetInsertTargetItem(item, new Point(10, 2), out var posBefore);
            Assert.Equal(InsertPosition.Before, posBefore);

            // Mouse no fundo (Y = 98) -> After
            var targetAfter = behavior.GetInsertTargetItem(item, new Point(10, 98), out var posAfter);
            Assert.Equal(InsertPosition.After, posAfter);

            // Mouse no meio (Y = 50) -> Retorna null
            var targetMiddle = behavior.GetInsertTargetItem(item, new Point(10, 50), out _);
            Assert.Null(targetMiddle);
        }

        [Fact]
        public void Contract06_InsertAdorner_ExistsAndValid()
        {
            var adornedElement = new Border();
            var adorner = new InsertAdorner(adornedElement);

            Assert.NotNull(adorner);
            Assert.Same(adornedElement, adorner.AdornedElement);
            Assert.False(adorner.IsHitTestVisible);

            adorner.Position = InsertPosition.Before;
            Assert.Equal(InsertPosition.Before, adorner.Position);

            adorner.Position = InsertPosition.After;
            Assert.Equal(InsertPosition.After, adorner.Position);
        }

        [Fact]
        public void Contract07_ReferenceHostDragDropBehavior_ExistsAndValid()
        {
            var behavior = new ReferenceHostDragDropBehavior();
            Assert.NotNull(behavior);

            var container = new Border();
            var items = behavior.GetItemsToDrag(container);
            Assert.Empty(items);

            var dropTarget = behavior.GetDropTargetItem(container);
            Assert.Null(dropTarget);
        }

        [Fact]
        public void Contract08_TreeViewStopEditOnLostFocusBehavior_ExistsAndValid()
        {
            var behavior = new TreeViewStopEditOnLostFocusBehavior();
            Assert.NotNull(behavior);
            Assert.Equal("LostFocus", behavior.EventName);

            // OnEvent seguro mesmo sem AssociatedObject
            behavior.OnEvent();
        }

        [Fact]
        public void Contract09_FrameworkElementDragDropBehavior_ExistsAndValid()
        {
            var behavior = new FrameworkElementDragDropBehavior();
            Assert.NotNull(behavior);

            var container = new Border();
            var items = behavior.GetItemsToDrag(container);
            Assert.Empty(items);

            var matchedContainer = behavior.GetContainer(new object());
            Assert.Null(matchedContainer);
        }

        [Fact]
        public void Contract10_InsertPosition_ExistsAndValid()
        {
            Assert.Equal(0, (int)InsertPosition.Before);
            Assert.Equal(1, (int)InsertPosition.After);

            var names = Enum.GetNames(typeof(InsertPosition));
            Assert.Contains("Before", names);
            Assert.Contains("After", names);
        }

        [Fact]
        public void Contract11_EnumerableExtensions_ExistsAndFunctional()
        {
            // Validação funcional de Repeat
            var counter = 0;
            Func<int> gen = () => ++counter;
            var repeatResult = gen.Repeat(5).ToList();
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, repeatResult);

            Assert.Throws<ArgumentOutOfRangeException>(() => gen.Repeat(-1).ToList());

            // Validação funcional de GroupAdjacentBy
            var items = new List<int> { 1, 1, 2, 2, 2, 3, 1, 1 };
            var grouped = items.GroupAdjacentBy((a, b) => a == b).Select(g => g.ToList()).ToList();
            Assert.Equal(4, grouped.Count);
            Assert.Equal(new[] { 1, 1 }, grouped[0]);
            Assert.Equal(new[] { 2, 2, 2 }, grouped[1]);
            Assert.Equal(new[] { 3 }, grouped[2]);
            Assert.Equal(new[] { 1, 1 }, grouped[3]);

            // Teste com lista vazia
            var empty = Enumerable.Empty<int>().GroupAdjacentBy((a, b) => a == b);
            Assert.Empty(empty);
        }

        private class MockChildViewModel : IChildViewModel
        {
            private readonly IChildViewModel? parent;
            public MockChildViewModel(IChildViewModel? parent) => this.parent = parent;
            public string GetName() => "Mock";
            public IChildViewModel? GetParent() => parent;
        }

        [Fact]
        public void Contract12_ViewModelExtensions_ExistsAndFunctional()
        {
            var parentA = new MockChildViewModel(null);
            var parentB = new MockChildViewModel(null);

            var siblings = new List<IChildViewModel>
            {
                new MockChildViewModel(parentA),
                new MockChildViewModel(parentA),
                new MockChildViewModel(parentA)
            };
            Assert.True(siblings.AllSiblings());

            var notSiblings = new List<IChildViewModel>
            {
                new MockChildViewModel(parentA),
                new MockChildViewModel(parentB)
            };
            Assert.False(notSiblings.AllSiblings());

            var empty = new List<IChildViewModel>();
            Assert.False(empty.AllSiblings());

            Assert.Throws<ArgumentNullException>(() => ((IEnumerable<IChildViewModel>)null).AllSiblings());
        }
    }
}
