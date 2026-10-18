// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    public class ReferenceHostDragDropBehavior : DragDropBehavior<Control, Control>
    {
        public override IEnumerable<object> GetItemsToDrag(Control container)
        {
            return Enumerable.Empty<object>();
        }

        public override IAddChildViewModel GetDropTargetItem(Control container)
        {
            if (AssociatedObject?.DataContext is not NodeViewModel node)
                return null;

            if (!node.AssociatedData.TryGetValue(ReferenceData.AddReferenceViewModel, out var data))
                return null;

            if (data is not IAddReferenceViewModel referenceViewModel)
                return null;

            referenceViewModel.SetTargetNode(node);
            return referenceViewModel;
        }
    }
}
