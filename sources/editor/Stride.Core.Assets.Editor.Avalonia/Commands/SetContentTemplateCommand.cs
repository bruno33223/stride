// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Windows.Input;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;

namespace Stride.Core.Assets.Editor.Avalonia.Commands
{
    public class SetContentTemplateCommand : ICommand
    {
        public ContentPresenter? Target { get; set; }

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            if (Target != null)
            {
                Target.ContentTemplate = (IDataTemplate?)parameter;
            }
        }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }
    }
}
