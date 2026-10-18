// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows.Input;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Editor.AssetEditors.GraphicsCompositorEditor.ViewModels;

public class SharedRendererFactoryViewModel : DispatcherViewModel
{
    [NotNull] private readonly IObjectNode sharedRenderersNode;

    public SharedRendererFactoryViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] IObjectNode sharedRenderersNode, [NotNull] Type type)
        : base(serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(sharedRenderersNode);
        ArgumentNullException.ThrowIfNull(type);
        this.sharedRenderersNode = sharedRenderersNode;
        Name = DisplayAttribute.GetDisplayName(type);
        Type = type;
        CreateCommand = new AnonymousCommand(serviceProvider, Create);
    }

    public string Name { get; }

    public Type Type { get; }

    public ICommand CreateCommand { get; }

    private void Create()
    {
        var renderer = (ISharedRenderer?)Activator.CreateInstance(Type);
        if (renderer == null) return;

        using var transaction = ServiceProvider.Get<IUndoRedoService>().CreateTransaction();
        sharedRenderersNode.Add(renderer);
        ServiceProvider.Get<IUndoRedoService>().SetName(transaction, "Create renderer");
    }
}
