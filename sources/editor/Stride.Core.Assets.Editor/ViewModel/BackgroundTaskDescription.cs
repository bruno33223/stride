// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModel
{
    public abstract class BackgroundTaskDescription : ViewModelBase, IDisposable
    {
        private string description = string.Empty;

        public string Description { get { return description; } set { SetValue(ref description, value); } }

        public virtual void Dispose()
        {
            Destroy();
        }

        protected static void PropertyChanged(BackgroundTaskDescription d)
        {
            d.Description = d.UpdateDescription();
        }

        public void NotifyPropertyChanged()
        {
            Description = UpdateDescription();
        }

        protected abstract string UpdateDescription();
    }
}
