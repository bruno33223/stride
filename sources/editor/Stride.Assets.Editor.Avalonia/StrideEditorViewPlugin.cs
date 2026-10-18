// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Avalonia.Controls;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Assets.Editor;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Avalonia.Views;
using Stride.Core.Presentation.Views;
using Stride.Editor.Annotations;
using Stride.Editor.Avalonia.Preview;
using Stride.Editor.Preview;
using Stride.Editor.Preview.Views;

namespace Stride.Assets.Editor.Avalonia;

public sealed class StrideEditorViewPlugin : AssetsEditorPlugin
{
    public override void InitializePlugin(ILogger logger)
    {
        // nothing for now
    }

    public override void InitializeSession(ISessionViewModel session)
    {
        var pluginService = session.ServiceProvider.Get<IAssetsPluginService>();
        var previewFactories = new Dictionary<Type, AssetPreviewFactory>();
        foreach (var stridePlugin in pluginService.Plugins.OfType<AssetsEditorPlugin>())
        {
            var pluginAssembly = stridePlugin.GetType().Assembly;
            foreach (var type in pluginAssembly.GetTypes())
            {
                if (typeof(IAssetPreview).IsAssignableFrom(type) &&
                    type.GetCustomAttribute<AssetPreviewAttribute>() is { } attribute)
                {
                    previewFactories.Add(attribute.AssetType, (_, _, _) => (IAssetPreview)Activator.CreateInstance(type)!);
                }
            }
        }

        var previewService = new GameStudioPreviewService(session);
        previewService.RegisterAssetPreviewFactories(previewFactories);
        session.ServiceProvider.RegisterService(previewService);
    }

    public override void RegisterAssetEditorViewTypes(IDictionary<Type, Type> assetEditorViewTypes)
    {
        base.RegisterAssetEditorViewTypes(assetEditorViewTypes);
        assetEditorViewTypes.TryAdd(typeof(SpriteSheetEditorViewModel), typeof(SpriteSheetEditorView));
        assetEditorViewTypes.TryAdd(typeof(UIPageEditorViewModel), typeof(UIPageEditorView));
        assetEditorViewTypes.TryAdd(typeof(UILibraryEditorViewModel), typeof(UILibraryEditorView));
        assetEditorViewTypes.TryAdd(typeof(GraphicsCompositorEditorViewModel), typeof(GraphicsCompositorEditorView));
        assetEditorViewTypes.TryAdd(typeof(VisualScriptEditorViewModel), typeof(VisualScriptEditorView));
        assetEditorViewTypes.TryAdd(typeof(ScriptSourceFileEditorViewModel), typeof(ScriptSourceFileEditorView));
        assetEditorViewTypes.TryAdd(typeof(EntityHierarchyEditorViewModel), typeof(EntityHierarchyEditorView));
        assetEditorViewTypes.TryAdd(typeof(ModelEditorViewModel), typeof(ModelEditorView));
        assetEditorViewTypes.TryAdd(typeof(MaterialEditorViewModel), typeof(MaterialEditorView));
        assetEditorViewTypes.TryAdd(typeof(SoundEditorViewModel), typeof(SoundEditorView));
        assetEditorViewTypes.TryAdd(typeof(VideoEditorViewModel), typeof(VideoEditorView));
    }

    public override void RegisterAssetPreviewViewModelTypes(IDictionary<Type, Type> assetPreviewViewModelTypes)
    {
        // nothing for now
    }

    public override void RegisterAssetPreviewViewTypes(IDictionary<Type, Type> assetPreviewViewTypes)
    {
        var pluginAssembly = GetType().Assembly;
        foreach (var type in pluginAssembly.GetTypes())
        {
            if (typeof(IPreviewView).IsAssignableFrom(type))
            {
                foreach (var attribute in type.GetCustomAttributes<AssetPreviewViewAttribute>())
                {
                    assetPreviewViewTypes.Add(attribute.AssetPreviewType, type);
                }
            }
        }
    }

    public override void RegisterPrimitiveTypes(ICollection<Type> primitiveTypes)
    {
        // nothing for now
    }

    public override void RegisterTemplateProviders(ICollection<ITemplateProvider> templateProviders)
    {
        var dictionaries = new ResourceDictionary[]
        {
            new EntityPropertyTemplateProviders(),
            new AnimationPropertyTemplates(),
            new SkeletonPropertyTemplates(),
            new MaterialPropertyTemplates(),
            new SpriteFontPropertyTemplates(),
            new UIPropertyTemplates(),
        };

        foreach (var dictionary in dictionaries)
        {
            foreach (var (_, value) in dictionary)
            {
                if (value is TemplateProviderBase provider)
                {
                    templateProviders.Add(provider);
                }
            }
        }
    }
}
