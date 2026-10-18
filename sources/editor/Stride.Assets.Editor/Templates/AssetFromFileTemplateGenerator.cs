// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Templates;
using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;
using Stride.Core.Translation;

namespace Stride.Assets.Editor.Templates
{
    public class AssetFromFileTemplateGenerator : AssetFactoryTemplateGenerator
    {
        public new static readonly AssetFromFileTemplateGenerator Default = new AssetFromFileTemplateGenerator();

        protected static readonly PropertyKey<IEnumerable<UFile>> SourceFilesPathKey = new PropertyKey<IEnumerable<UFile>>("SourceFilesPathKey", typeof(AssetFromFileTemplateGenerator));

        protected virtual bool CanCreateAssetWithoutSource => true;

        public override bool IsSupportingTemplate(TemplateDescription templateDescription)
        {
            if (!(templateDescription is TemplateAssetFactoryDescription description))
                return false;
            var assetType = description.GetAssetType();
            return base.IsSupportingTemplate(templateDescription) && description.ImportSource && typeof(IAssetWithSource).IsAssignableFrom(assetType);
        }

        protected override async Task<bool> PrepareAssetCreation(AssetTemplateGeneratorParameters parameters)
        {
            if (parameters.Description is not TemplateAssetDescription description)
                return true;

            var assetType = description.GetAssetType();
            var files = parameters.SourceFiles.Count > 0 ? parameters.SourceFiles : await BrowseForSourceFiles(description, true);
            while (files == null)
            {
                var session = SessionViewModel.Instance;
                var dialogService = session?.ServiceProvider.Get<IDialogService>();
                if (dialogService == null)
                    return false;

                var assetTypeName = TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<DisplayAttribute>(assetType)?.Name ?? assetType.Name;
                var buttons = new[]
                {
                    new DialogButtonInfo { Content = Tr._p("Button", "Create"), Result = 1 },
                    new DialogButtonInfo { Content = Tr._p("Button", "Select a source..."), Result = 2, IsDefault = true },
                    new DialogButtonInfo { Content = Tr._p("Button", "Cancel"), Result = 3, IsCancel = true }
                };
                var result = await dialogService.MessageBoxAsync(
                    string.Format(Tr._p("Message", "Do you want to create this {0} without a source file?"), assetTypeName),
                    buttons, MessageBoxImage.Question);

                // Close without clicking a button or Cancel
                if (result == 0 || result == 3)
                    return false;

                // Create without source
                if (result == 1)
                    break;

                // Display the file picker again
                files = await BrowseForSourceFiles(description, true);
            }

            parameters.Tags.Add(SourceFilesPathKey, files);
            return await base.PrepareAssetCreation(parameters);
        }

        protected override IEnumerable<AssetItem> CreateAssets(AssetTemplateGeneratorParameters parameters)
        {
            var sources = parameters.Tags.Get(SourceFilesPathKey);
            if (sources == null)
                return base.CreateAssets(parameters);

            var assets = new List<AssetItem>();
            var defaultName = parameters.Name;
            foreach (var source in sources)
            {
                // Use the source file name for the asset name if it is valid
                parameters.Name = source.GetFileNameWithoutExtension();
                if (string.IsNullOrWhiteSpace(parameters.Name))
                    parameters.Name = defaultName;

                var assetImport = base.CreateAssets(parameters).First();
                ((IAssetWithSource)assetImport.Asset).Source = source;
                assets.Add(assetImport);
            }
            return assets;
        }

        protected virtual async Task<IEnumerable<UFile>?> BrowseForSourceFiles(TemplateAssetDescription description, bool allowMultiSelection)
        {
            var extensions = description.GetSupportedExtensions();
            var result = await BrowseForFiles(extensions, true, allowMultiSelection, InternalSettings.FileDialogLastImportDirectory.GetValue());
            if (result != null)
            {
                var list = result.ToList();
                InternalSettings.FileDialogLastImportDirectory.SetValue(list.First());
                InternalSettings.Save();
                return list;
            }
            return null;
        }

        protected List<AssetItem> MakeUniqueNames(IEnumerable<AssetItem> assets)
        {
            var result = new List<AssetItem>();
            foreach (var asset in assets)
            {
                var uniqueName = NamingHelper.ComputeNewName(asset.Location, x => result.Any(y => y.Asset != asset.Asset && x == y.Location), "{0}_{1}");
                result.Add(new AssetItem(uniqueName, asset.Asset));
            }
            return result;
        }
    }
}
