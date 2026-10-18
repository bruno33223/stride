// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Assets.Presentation.ViewModels;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories
{
    public abstract class EntityFactory : IEntityFactory
    {
        public static string ComputeNewName(EntityHierarchyItemViewModel parent, string baseName)
        {
            return baseName;
        }

        public abstract Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent);

        protected static async Task<AssetViewModel?> PickupAsset(ISessionViewModel session, IEnumerable<Type> assetTypes)
        {
            var sessionVm = session as SessionViewModel;
            if (sessionVm == null)
                return null;

            var dialogService = session.ServiceProvider.TryGet<IEditorDialogService>();
            if (dialogService == null)
                return null;

            var assetPicker = dialogService.CreateAssetPickerDialog(sessionVm);
            if (assetPicker == null)
                return null;

            assetPicker.Message = "Select the asset to use for this entity";
            assetPicker.AcceptedTypes.AddRange(assetTypes);
            var result = await assetPicker.ShowModal();
            return result == DialogResult.Ok ? assetPicker.SelectedAssets.FirstOrDefault() : null;
        }

        protected static Task<Entity> CreateEntityWithComponent(string name, EntityComponent component, params EntityComponent[]? additionalComponents)
        {
            var newEntity = new Entity { Name = name };
            newEntity.Components.Add(component);
            if (additionalComponents != null)
            {
                foreach (var additionalComponent in additionalComponents)
                {
                    newEntity.Components.Add(additionalComponent);
                }
            }
            return Task.FromResult(newEntity);
        }
    }
}
