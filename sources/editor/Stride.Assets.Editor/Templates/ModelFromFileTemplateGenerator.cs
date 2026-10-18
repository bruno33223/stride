// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Assets.Editor.Services;
using Stride.Assets.Materials;
using Stride.Assets.Models;
using Stride.Assets.Textures;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Serialization;
using Stride.Core.Settings;
using Stride.Rendering;

namespace Stride.Assets.Editor.Templates;

public static class ModelFromFileTemplateSettings
{
    public static SettingsKey<bool> ImportMaterials = new SettingsKey<bool>("Templates/ModelFromFile/ImportMaterials", PackageUserSettings.SettingsContainer, true);
    public static SettingsKey<bool> DeduplicateMaterials = new SettingsKey<bool>("Templates/ModelFromFile/DeduplicateMaterials", PackageUserSettings.SettingsContainer, true);
    public static SettingsKey<bool> ImportTextures = new SettingsKey<bool>("Templates/ModelFromFile/ImportTextures", PackageUserSettings.SettingsContainer, true);
    public static SettingsKey<bool> ImportAnimations = new SettingsKey<bool>("Templates/ModelFromFile/ImportAnimations", PackageUserSettings.SettingsContainer, true);
    public static SettingsKey<bool> ImportSkeleton = new SettingsKey<bool>("Templates/ModelFromFile/ImportSkeleton", PackageUserSettings.SettingsContainer, true);
    public static SettingsKey<AssetId> DefaultSkeleton = new SettingsKey<AssetId>("Templates/ModelFromFile/DefaultSkeleton", PackageUserSettings.SettingsContainer, AssetId.Empty);
}

public class ModelFromFileTemplateGenerator : AssetFromFileTemplateGenerator
{
    public new static ModelFromFileTemplateGenerator Default { get; } = new ModelFromFileTemplateGenerator();

    public static Guid Id = new Guid("3B778954-54C4-4FF3-97EF-4CD7AEA0B97D");

    public static Func<IModelAssetTemplateDialog>? CreateDialogDelegate { get; set; }

    protected static readonly PropertyKey<bool> ImportMaterialsKey = new PropertyKey<bool>("ImportMaterials", typeof(ModelFromFileTemplateGenerator));
    protected static readonly PropertyKey<bool> DeduplicateMaterialsKey = new PropertyKey<bool>("DeduplicateMaterials", typeof(ModelFromFileTemplateGenerator));
    protected static readonly PropertyKey<bool> ImportTexturesKey = new PropertyKey<bool>("ImportTextures", typeof(ModelFromFileTemplateGenerator));
    protected static readonly PropertyKey<bool> ImportAnimationsKey = new PropertyKey<bool>("ImportAnimations", typeof(ModelFromFileTemplateGenerator));
    protected static readonly PropertyKey<bool> ImportSkeletonKey = new PropertyKey<bool>("ImportSkeleton", typeof(ModelFromFileTemplateGenerator));
    protected static readonly PropertyKey<Skeleton?> SkeletonToUseKey = new PropertyKey<Skeleton?>("SkeletonToUse", typeof(ModelFromFileTemplateGenerator));

    public override bool IsSupportingTemplate(TemplateDescription templateDescription)
    {
        return templateDescription?.Id == Id;
    }

    protected override async Task<bool> PrepareAssetCreation(AssetTemplateGeneratorParameters parameters)
    {
        var result = await base.PrepareAssetCreation(parameters);
        if (!result)
            return false;

        var files = parameters.Tags.Get(SourceFilesPathKey);
        if (files == null)
            return true;

        var showDeduplicateMaterialsCheckBox = files.Any(x => ImportThreeDCommand.IsSupportingExtensions(x.GetFileExtension()));
        var profile = parameters.Package?.UserSettings?.Profile;

        bool importMaterials = ModelFromFileTemplateSettings.ImportMaterials.GetValue(profile, true);
        bool deduplicateMaterials = ModelFromFileTemplateSettings.DeduplicateMaterials.GetValue(profile, true);
        bool importTextures = ModelFromFileTemplateSettings.ImportTextures.GetValue(profile, true);
        bool importAnimations = ModelFromFileTemplateSettings.ImportAnimations.GetValue(profile, true);
        bool importSkeleton = ModelFromFileTemplateSettings.ImportSkeleton.GetValue(profile, true);
        Skeleton? skeletonToReuse = null;

        if (!parameters.Unattended)
        {
            var dialog = CreateDialogDelegate?.Invoke() ?? SessionViewModel.Instance?.ServiceProvider.TryGet<IStrideDialogService>()?.CreateModelAssetTemplateDialog();
            if (dialog != null)
            {
                dialog.Parameters.ImportMaterials = importMaterials;
                dialog.Parameters.ShowDeduplicateMaterialsCheckBox = showDeduplicateMaterialsCheckBox;
                dialog.Parameters.ShowFbxDedupeNotSupportedWarning = false;
                dialog.Parameters.DeduplicateMaterials = deduplicateMaterials;
                dialog.Parameters.ImportTextures = importTextures;
                dialog.Parameters.ImportAnimations = importAnimations;
                dialog.Parameters.ImportSkeleton = importSkeleton;

                var skeletonId = ModelFromFileTemplateSettings.DefaultSkeleton.GetValue();
                var skeleton = SessionViewModel.Instance?.GetAssetById(skeletonId);
                if (skeleton != null)
                {
                    dialog.Parameters.ReuseSkeleton = true;
                    dialog.Parameters.SkeletonToReuse = ContentReferenceHelper.CreateReference<Skeleton>(skeleton);
                }

                await dialog.ShowModal();

                if (dialog.Result == DialogResult.Cancel)
                    return false;

                importMaterials = dialog.Parameters.ImportMaterials;
                deduplicateMaterials = dialog.Parameters.DeduplicateMaterials;
                importTextures = dialog.Parameters.ImportTextures;
                importAnimations = dialog.Parameters.ImportAnimations;
                importSkeleton = dialog.Parameters.ImportSkeleton;
                skeletonToReuse = dialog.Parameters.ReuseSkeleton ? dialog.Parameters.SkeletonToReuse : null;
            }
        }

        // Apply settings
        parameters.Tags.Set(ImportMaterialsKey, importMaterials);
        parameters.Tags.Set(DeduplicateMaterialsKey, deduplicateMaterials);
        parameters.Tags.Set(ImportTexturesKey, importTextures);
        parameters.Tags.Set(ImportAnimationsKey, importAnimations);
        parameters.Tags.Set(ImportSkeletonKey, importSkeleton);
        parameters.Tags.Set(SkeletonToUseKey, skeletonToReuse);

        // Save settings
        if (profile != null)
        {
            ModelFromFileTemplateSettings.ImportMaterials.SetValue(importMaterials, profile);
            ModelFromFileTemplateSettings.DeduplicateMaterials.SetValue(deduplicateMaterials, profile);
            ModelFromFileTemplateSettings.ImportTextures.SetValue(importTextures, profile);
            ModelFromFileTemplateSettings.ImportAnimations.SetValue(importAnimations, profile);
            ModelFromFileTemplateSettings.ImportSkeleton.SetValue(importSkeleton, profile);
            var skeletonId = AttachedReferenceManager.GetAttachedReference(skeletonToReuse)?.Id ?? AssetId.Empty;
            ModelFromFileTemplateSettings.DefaultSkeleton.SetValue(skeletonId, profile);
            parameters.Package?.UserSettings?.Save();
        }

        return true;
    }

    protected override IEnumerable<AssetItem> CreateAssets(AssetTemplateGeneratorParameters parameters)
    {
        var files = parameters.Tags.Get(SourceFilesPathKey);
        if (files == null)
            return base.CreateAssets(parameters);

        var importMaterials = parameters.Tags.Get(ImportMaterialsKey);
        var deduplicateMaterials = parameters.Tags.Get(DeduplicateMaterialsKey);
        var importTextures = parameters.Tags.Get(ImportTexturesKey);
        var importAnimations = parameters.Tags.Get(ImportAnimationsKey);
        var importSkeleton = parameters.Tags.Get(ImportSkeletonKey);
        var skeletonToReuse = parameters.Tags.Get(SkeletonToUseKey);

        var importParameters = new AssetImporterParameters { Logger = parameters.Logger };
        importParameters.InputParameters.Set(ModelAssetImporter.DeduplicateMaterialsKey, deduplicateMaterials);
        importParameters.SelectedOutputTypes.Add(typeof(ModelAsset), true);
        importParameters.SelectedOutputTypes.Add(typeof(MaterialAsset), importMaterials);
        importParameters.SelectedOutputTypes.Add(typeof(TextureAsset), importTextures);
        importParameters.SelectedOutputTypes.Add(typeof(AnimationAsset), importAnimations);
        importParameters.SelectedOutputTypes.Add(typeof(SkeletonAsset), importSkeleton);

        var importedAssets = new List<AssetItem>();

        foreach (var file in files)
        {
            var importer = AssetRegistry.FindImporterForFile(file).OfType<ModelAssetImporter>().FirstOrDefault();
            if (importer == null)
            {
                parameters.Logger?.Warning($"No importer found for file \"{file}\"");
                continue;
            }

            var assets = importer.Import(file, importParameters).Select(x => new AssetItem(UPath.Combine(parameters.TargetLocation, x.Location), x.Asset)).ToList();

            foreach (var model in assets.Select(x => x.Asset).OfType<ModelAsset>())
            {
                if (skeletonToReuse != null)
                {
                    model.Skeleton = skeletonToReuse;
                }
            }

            // Create unique names amongst the list of assets
            importedAssets.AddRange(MakeUniqueNames(assets));
        }

        return importedAssets;
    }
}
