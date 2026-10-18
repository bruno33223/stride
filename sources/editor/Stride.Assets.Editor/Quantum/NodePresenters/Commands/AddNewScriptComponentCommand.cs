using Stride.Core.Assets.Editor.ViewModels;
// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Editor.Templates;
using Stride.Core;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Templates;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Quantum;
using Stride.Engine;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Commands;

public class AddNewScriptComponentCommand : SyncNodePresenterCommandBase
{
    /// <summary>
    /// The name of this command.
    /// </summary>
    public const string CommandName = "AddNewScriptComponent";

    /// <inheritdoc/>
    public override string Name => CommandName;

    /// <inheritdoc/>
    public override CombineMode CombineMode => CombineMode.CombineOnlyForAll;

    /// <inheritdoc/>
    public override bool CanAttach(INodePresenter nodePresenter)
    {
        return typeof(EntityComponentCollection).IsAssignableFrom(nodePresenter.Descriptor.Type);
    }

    /// <inheritdoc/>
    protected override async void ExecuteSync(INodePresenter nodePresenter, object? parameter, object? preExecuteResult)
    {
        if (nodePresenter is not AssetMemberNodePresenter assetPresenter)
            return;

        if (assetPresenter.Asset.Session is not SessionViewModel session) return;
        var undoRedoService = assetPresenter.Asset.UndoRedoService;
        var serviceProvider = assetPresenter.Asset.ServiceProvider;

        var scriptSourceCodeProvider = serviceProvider.TryGet<IEntityComponentSourceCodeResolver>();
        if (scriptSourceCodeProvider == null)
            return;

        var template = ScriptTemplateGenerator.GetScriptTemplateAssetDescriptions(session.FindTemplates(TemplateScope.Asset)).FirstOrDefault();
        if (template == null)
            return;

        var viewModel = new TemplateDescriptionViewModel(serviceProvider, template);
        var customParameters = ScriptTemplateGenerator.GetAssetOverrideParameters(parameter as string, true);
        var assetViewModel = (await session.AssetCollection.RunAssetTemplate(viewModel, null, customParameters)).FirstOrDefault();

        if (assetViewModel == null || assetViewModel.IsDeleted)
            return;

        IEnumerable<Type> componentTypes = scriptSourceCodeProvider.GetTypesFromSourceFile(assetViewModel.AssetItem.FullPath);
        var componentType = componentTypes.FirstOrDefault();
        if (componentType != null)
        {
            using (var transaction = session.UndoRedoService.CreateTransaction())
            {
                object? component = Activator.CreateInstance(componentType);
                if (component != null)
                {
                    var index = new NodeIndex(nodePresenter.Children.Count);
                    nodePresenter.AddItem(component);
                    session.UndoRedoService.PushOperation(
                        new AnonymousDirtyingOperation(
                            assetPresenter.Asset.Dirtiables,
                            () => nodePresenter.RemoveItem(component, index),
                            () => nodePresenter.AddItem(component)));

                    session.UndoRedoService.SetName(transaction, "Add new script component.");
                }
            }
        }
    }
}
