// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.ViewModels;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Scripts;
using Stride.Engine;
using ScriptSourceFileAssetViewModel = Stride.Assets.Editor.ViewModels.ScriptSourceFileAssetViewModel;

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.ViewModels.AddAssetPolicies
{
    public class AddEntityComponentFileAssetPolicy : CreateComponentPolicyBase<ScriptSourceFileAsset, ScriptSourceFileAssetViewModel>
    {
        protected override bool CanAddOrInsert(EntityHierarchyItemViewModel parent, ScriptSourceFileAssetViewModel asset, AddChildModifiers modifiers, int index, out string message, params object[] messageArgs)
        {
            var scriptType = FindScriptType(asset.ServiceProvider, asset.AssetItem)?.FirstOrDefault();
            if (scriptType == null)
            {
                message = $"No class inheriting from {nameof(EntityComponent)} found in file {asset.Url}";
                return false;
            }

            message = string.Format($"Add script {scriptType.Name} into {{0}}", messageArgs);
            return true;
        }

        protected override EntityComponent? CreateComponentFromAsset(EntityHierarchyItemViewModel parent, ScriptSourceFileAssetViewModel asset)
        {
            var scriptType = FindScriptType(asset.ServiceProvider, asset.AssetItem)?.FirstOrDefault();
            if (scriptType == null)
                return null;

            try
            {
                return Activator.CreateInstance(scriptType) as EntityComponent;
            }
            catch
            {
                return null;
            }
        }

        private static IEnumerable<Type>? FindScriptType(IViewModelServiceProvider serviceProvider, AssetItem scriptAsset)
        {
            return serviceProvider.TryGet<IEntityComponentSourceCodeResolver>()?.GetTypesFromSourceFile(scriptAsset.FullPath);
        }
    }
}
