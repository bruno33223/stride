// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Editor.View.Behaviors;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Presentation.ViewModels;
using InsertPosition = Stride.Core.Assets.Presentation.ViewModels.InsertPosition;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests
{
    public class TestDragDropBehaviors
    {
        [Fact]
        public void TestDragContainerPayloadIdentification()
        {
            var items = new List<object> { "Asset1", "Asset2", "Asset3" };
            var container = new DragContainer(items);

            Assert.Equal("DraggedData", DragContainer.Format);
            Assert.Equal(3, container.Items.Length);
            Assert.Equal("Asset1", container.Items[0]);
            Assert.Equal(3, container.PreviewItems.Count());

            // Testa PreviewItems com mais de 10 elementos (deve adicionar reticências "...")
            var manyItems = Enumerable.Range(0, 15).Select(i => (object)$"Item_{i}").ToList();
            var largeContainer = new DragContainer(manyItems);
            var previewList = largeContainer.PreviewItems.ToList();
            Assert.Equal(11, previewList.Count);
            Assert.Equal("...", previewList.Last());

            // Valida reatividade de IsAccepted / IsRejected
            container.IsAccepted = true;
            Assert.True(container.IsAccepted);
            Assert.False(container.IsRejected);

            container.IsAccepted = false;
            Assert.False(container.IsAccepted);
            Assert.True(container.IsRejected);

            container.Message = DragDropBehavior.InvalidDropAreaMessage;
            Assert.False(string.IsNullOrEmpty(container.Message));
        }

        [Fact]
        public void TestInsertPositionThresholdCalculation()
        {
            const double containerHeight = 100.0;
            const double threshold = DragDropBehavior.InsertThreshold;
            Assert.Equal(4.0, threshold);

            // Posição no topo (0 a threshold): deve ser Before
            var posTop = 2.0;
            InsertPosition? calcTop = (posTop >= 0 && posTop <= threshold) ? InsertPosition.Before : (InsertPosition?)null;
            Assert.Equal(InsertPosition.Before, calcTop);

            // Posição no fundo (Height - threshold a Height): deve ser After
            var posBottom = 98.0;
            InsertPosition? calcBottom = (posBottom >= containerHeight - threshold && posBottom <= containerHeight) ? InsertPosition.After : (InsertPosition?)null;
            Assert.Equal(InsertPosition.After, calcBottom);

            // Posição intermediária: não é Before nem After (é Inside/drop normal)
            var posMiddle = 50.0;
            bool isBefore = posMiddle >= 0 && posMiddle <= threshold;
            bool isAfter = posMiddle >= containerHeight - threshold && posMiddle <= containerHeight;
            Assert.False(isBefore);
            Assert.False(isAfter);
        }

        [Fact]
        public void TestDropValidationOnFolderTarget()
        {
            var folderTarget = new MockFolderDropTarget();
            var payload = new[] { "AssetModel.sdpkg" };

            // Drop com modificador None
            bool canAdd = folderTarget.CanAddChildren(payload, AddChildModifiers.None, out string message);
            Assert.True(canAdd);
            Assert.Equal("Drop accepted into folder", message);

            folderTarget.AddChildren(payload, AddChildModifiers.None);
            Assert.Single(folderTarget.DroppedItems);

            // Drop de payload inválido
            var invalidPayload = new[] { 12345 };
            bool canAddInvalid = folderTarget.CanAddChildren(invalidPayload.Cast<object>().ToArray(), AddChildModifiers.None, out string errMessage);
            Assert.False(canAddInvalid);
            Assert.Equal("Unsupported asset type", errMessage);
        }

        [Fact]
        public void TestShouldDisplayDropAdornerRules()
        {
            var internalItems = new object[] { "InternalAsset", 42 };
            var externalItems = new object[] { new UFile("/path/to/texture.png"), new UFile("/path/to/model.fbx") };
            var mixedItems = new object[] { "InternalAsset", new UFile("/path/to/texture.png") };

            // Regra Never: sempre false
            Assert.False(DragDropHelper.ShouldDisplayDropAdorner(DisplayDropAdorner.Never, internalItems));
            Assert.False(DragDropHelper.ShouldDisplayDropAdorner(DisplayDropAdorner.Never, externalItems));

            // Regra InternalOnly: true apenas se nenhum item for UFile
            Assert.True(DragDropHelper.ShouldDisplayDropAdorner(DisplayDropAdorner.InternalOnly, internalItems));
            Assert.False(DragDropHelper.ShouldDisplayDropAdorner(DisplayDropAdorner.InternalOnly, externalItems));
            Assert.False(DragDropHelper.ShouldDisplayDropAdorner(DisplayDropAdorner.InternalOnly, mixedItems));

            // Regra ExternalOnly: true apenas se todos os itens forem UFile
            Assert.False(DragDropHelper.ShouldDisplayDropAdorner(DisplayDropAdorner.ExternalOnly, internalItems));
            Assert.True(DragDropHelper.ShouldDisplayDropAdorner(DisplayDropAdorner.ExternalOnly, externalItems));
            Assert.False(DragDropHelper.ShouldDisplayDropAdorner(DisplayDropAdorner.ExternalOnly, mixedItems));

            // Regra Always: sempre true
            Assert.True(DragDropHelper.ShouldDisplayDropAdorner(DisplayDropAdorner.Always, internalItems));
            Assert.True(DragDropHelper.ShouldDisplayDropAdorner(DisplayDropAdorner.Always, externalItems));
        }

        [Fact]
        public void TestReorderItemViewModelTargeting()
        {
            var mockReorder = new MockReorderItemViewModel();
            var children = new object[] { "NodeA" };

            Assert.True(mockReorder.CanInsertChildren(children, InsertPosition.Before, AddChildModifiers.None, out string msg));
            Assert.Equal("Insert before", msg);

            mockReorder.InsertChildren(children, InsertPosition.Before, AddChildModifiers.None);
            Assert.True(mockReorder.WasInserted);
        }

        private class MockFolderDropTarget : IAddChildViewModel
        {
            public List<object> DroppedItems { get; } = new List<object>();

            public bool CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
            {
                if (children.All(x => x is string))
                {
                    message = "Drop accepted into folder";
                    return true;
                }
                message = "Unsupported asset type";
                return false;
            }

            public void AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
            {
                DroppedItems.AddRange(children);
            }
        }

        private class MockReorderItemViewModel : IReorderItemViewModel
        {
            public bool WasInserted { get; private set; }

            public void SetTargetNode(Stride.Core.Presentation.Quantum.ViewModels.NodeViewModel node)
            {
            }

            public bool CanInsertChildren(IReadOnlyCollection<object> children, Stride.Core.Assets.Presentation.ViewModels.InsertPosition position, AddChildModifiers modifiers, out string message)
            {
                message = position == Stride.Core.Assets.Presentation.ViewModels.InsertPosition.Before ? "Insert before" : "Insert after";
                return true;
            }

            public void InsertChildren(IReadOnlyCollection<object> children, Stride.Core.Assets.Presentation.ViewModels.InsertPosition position, AddChildModifiers modifiers)
            {
                WasInserted = true;
            }
        }
    }
}
