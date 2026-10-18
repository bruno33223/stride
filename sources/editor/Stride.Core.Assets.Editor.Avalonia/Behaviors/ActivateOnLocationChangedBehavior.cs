// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using DirectoryBaseViewModel = Stride.Core.Assets.Editor.ViewModel.DirectoryBaseViewModel;

namespace Stride.Core.Assets.Editor.Avalonia.Behaviors
{
    public abstract class ActivateOnLocationChangedBehavior<T> : ActivateOnCollectionChangedBehavior<T> where T : AvaloniaObject
    {
        private bool selectionDone;

        protected override bool MatchChange(NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                selectionDone = false;
            }
            if (e.Action == NotifyCollectionChangedAction.Add && !selectionDone && e.NewItems != null)
            {
                if (e.NewItems.OfType<DirectoryBaseViewModel>().Any())
                {
                    selectionDone = true;
                }
            }
            return selectionDone;
        }
    }
}
