// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestAssetPreviewEngineAndViewModelsIntegration
{
    [Fact]
    public void TestAssetPreviewServiceContract()
    {
        var coreAssetsEditorAssembly = typeof(Stride.Core.Assets.Editor.Services.IAssetPreviewService).Assembly;
        var serviceType = coreAssetsEditorAssembly.GetType("Stride.Core.Assets.Editor.Services.IAssetPreviewService");
        Assert.NotNull(serviceType);
        Assert.True(serviceType.IsInterface);
        Assert.True(typeof(IDisposable).IsAssignableFrom(serviceType));

        var setAssetMethod = serviceType.GetMethod("SetAssetToPreview");
        Assert.NotNull(setAssetMethod);
        Assert.Single(setAssetMethod.GetParameters());

        var getViewMethod = serviceType.GetMethod("GetCurrentPreviewView");
        Assert.NotNull(getViewMethod);
        Assert.Equal(typeof(object), getViewMethod.ReturnType);

        var eventUpdated = serviceType.GetEvent("PreviewAssetUpdated");
        Assert.NotNull(eventUpdated);

        Assert.NotNull(serviceType.GetMethod("OnShowPreview"));
        Assert.NotNull(serviceType.GetMethod("OnHidePreview"));
    }

    [Fact]
    public void TestAssetPreviewEngineTypesInStrideAssetsEditor()
    {
        var assetsEditorAssembly = typeof(Stride.Assets.Editor.StrideEditorPlugin).Assembly;

        var expectedPreviewTypes = new[]
        {
            "Stride.Assets.Editor.Preview.IAssetPreview",
            "Stride.Assets.Editor.Preview.IPreviewBuilder",
            "Stride.Assets.Editor.Preview.AssetPreview",
            "Stride.Assets.Editor.Preview.PreviewGame",
            "Stride.Assets.Editor.Preview.PreviewEntity",
            "Stride.Assets.Editor.Annotations.AssetPreviewAttribute",
            "Stride.Assets.Editor.Annotations.AssetPreviewAttribute`1",
            "Stride.Assets.Editor.Annotations.AssetPreviewViewModelAttribute",
            "Stride.Assets.Editor.Annotations.AssetPreviewViewModelAttribute`1",
            "Stride.Assets.Editor.Annotations.AssetPreviewViewAttribute",
            "Stride.Assets.Editor.Annotations.AssetPreviewViewAttribute`1",
            "Stride.Assets.Editor.Preview.ViewModel.IAssetPreviewViewModel"
        };

        foreach (var typeName in expectedPreviewTypes)
        {
            var resolvedType = assetsEditorAssembly.GetType(typeName);
            Assert.True(resolvedType != null, $"Tipo obrigatório {typeName} não encontrado em Stride.Assets.Editor.");
        }

        // Validate IAssetPreview members
        var iAssetPreviewType = assetsEditorAssembly.GetType("Stride.Assets.Editor.Preview.IAssetPreview")!;
        Assert.True(iAssetPreviewType.IsInterface);
        Assert.NotNull(iAssetPreviewType.GetProperty("PreviewViewModel"));
        Assert.NotNull(iAssetPreviewType.GetProperty("AssetViewModel"));
        Assert.NotNull(iAssetPreviewType.GetProperty("RenderingMode"));
        Assert.NotNull(iAssetPreviewType.GetMethod("Initialize"));
        Assert.NotNull(iAssetPreviewType.GetMethod("IsInitialized"));
        Assert.NotNull(iAssetPreviewType.GetMethod("Update"));
        Assert.NotNull(iAssetPreviewType.GetMethod("Dispose"));
        Assert.NotNull(iAssetPreviewType.GetMethod("OnViewAttached"));

        // Validate IPreviewBuilder members
        var iPreviewBuilderType = assetsEditorAssembly.GetType("Stride.Assets.Editor.Preview.IPreviewBuilder")!;
        Assert.True(iPreviewBuilderType.IsInterface);
        Assert.NotNull(iPreviewBuilderType.GetProperty("AssetBuilderService"));
        Assert.NotNull(iPreviewBuilderType.GetProperty("Dispatcher"));
        Assert.NotNull(iPreviewBuilderType.GetProperty("Logger"));
        Assert.NotNull(iPreviewBuilderType.GetProperty("PreviewGame"));
        Assert.NotNull(iPreviewBuilderType.GetMethod("Compile"));
        Assert.NotNull(iPreviewBuilderType.GetMethod("GetStrideView"));

        // Validate PreviewEntity
        var previewEntityType = assetsEditorAssembly.GetType("Stride.Assets.Editor.Preview.PreviewEntity")!;
        Assert.NotNull(previewEntityType.GetField("Entity") ?? (MemberInfo)previewEntityType.GetProperty("Entity")!);
        Assert.NotNull(previewEntityType.GetField("Disposed") ?? (MemberInfo)previewEntityType.GetProperty("Disposed")!);

        // Validate PreviewGame
        var previewGameType = assetsEditorAssembly.GetType("Stride.Assets.Editor.Preview.PreviewGame")!;
        Assert.NotNull(previewGameType.GetMethod("LoadPreviewScene"));
        Assert.NotNull(previewGameType.GetMethod("UnloadPreviewScene"));
        Assert.NotNull(previewGameType.GetMethod("TriggerActiveRenderStageReevaluation"));

        // Validate IAssetPreviewViewModel
        var iAssetPreviewViewModelType = assetsEditorAssembly.GetType("Stride.Assets.Editor.Preview.ViewModel.IAssetPreviewViewModel")!;
        Assert.True(iAssetPreviewViewModelType.IsInterface);
        Assert.NotNull(iAssetPreviewViewModelType.GetProperty("Session"));
        Assert.NotNull(iAssetPreviewViewModelType.GetMethod("AttachPreview"));
    }

    [Fact]
    public void TestPresentationPreviewAndViewModelsInStrideAssetsPresentation()
    {
        var presentationAssembly = typeof(Stride.Assets.Presentation.StrideDefaultAssetsPlugin).Assembly;

        var expectedPresentationTypes = new[]
        {
            "Stride.Assets.Presentation.Preview.BuildAssetPreview`1",
            "Stride.Assets.Presentation.ViewModel.IAnimatedPreviewViewModel",
            "Stride.Assets.Presentation.ViewModel.Preview.AssetPreviewViewModel",
            "Stride.Assets.Presentation.ViewModel.Preview.ModelPreviewViewModel",
            "Stride.Assets.Presentation.ViewModel.Preview.MaterialPreviewViewModel",
            "Stride.Assets.Presentation.ViewModel.Preview.TextureBasePreviewViewModel",
            "Stride.Assets.Presentation.ViewModel.Preview.TexturePreviewViewModel",
            "Stride.Assets.Presentation.ViewModel.Preview.SkyboxPreviewViewModel",
            "Stride.Assets.Presentation.ViewModel.Preview.HeightmapPreviewViewModel",
            "Stride.Assets.Presentation.ViewModel.Preview.EntityPreviewViewModel",
            "Stride.Assets.Presentation.ViewModel.Preview.ProceduralModelPreviewViewModel",
            "Stride.Assets.Presentation.ViewModel.Preview.AnimationPreviewViewModel"
        };

        foreach (var typeName in expectedPresentationTypes)
        {
            var resolvedType = presentationAssembly.GetType(typeName);
            Assert.True(resolvedType != null, $"Tipo obrigatório {typeName} não encontrado em Stride.Assets.Presentation.");
        }

        // Validate AnimationPreviewViewModel
        var animPreviewVmType = presentationAssembly.GetType("Stride.Assets.Presentation.ViewModel.Preview.AnimationPreviewViewModel")!;
        Assert.NotNull(animPreviewVmType.GetMethod("FindModelForPreview", BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(animPreviewVmType.GetProperty("PlayCommand"));
        Assert.NotNull(animPreviewVmType.GetProperty("PauseCommand"));
        Assert.NotNull(animPreviewVmType.GetProperty("TimeScale"));
        Assert.NotNull(animPreviewVmType.GetProperty("CurrentTime"));
        Assert.NotNull(animPreviewVmType.GetProperty("Duration"));
        Assert.NotNull(animPreviewVmType.GetProperty("IsValid"));
        Assert.NotNull(animPreviewVmType.GetProperty("IsPlaying"));

        // Validate IAnimatedPreviewViewModel
        var animatedVmType = presentationAssembly.GetType("Stride.Assets.Presentation.ViewModel.IAnimatedPreviewViewModel")!;
        Assert.True(animatedVmType.IsInterface);
        Assert.NotNull(animatedVmType.GetProperty("PlayCommand"));
        Assert.NotNull(animatedVmType.GetProperty("PauseCommand"));
        Assert.NotNull(animatedVmType.GetProperty("IsPlaying"));

        // Validate BuildAssetPreview`1 methods
        var buildPreviewType = presentationAssembly.GetType("Stride.Assets.Presentation.Preview.BuildAssetPreview`1")!;
        Assert.NotNull(buildPreviewType.GetMethod("LoadAsset"));
        Assert.NotNull(buildPreviewType.GetMethod("UnloadAsset"));

        // Validate TextureBasePreviewViewModel members
        var texBaseVmType = presentationAssembly.GetType("Stride.Assets.Presentation.ViewModel.Preview.TextureBasePreviewViewModel")!;
        Assert.NotNull(texBaseVmType.GetProperty("PreviewSelectedMipMap"));
        Assert.NotNull(texBaseVmType.GetProperty("PreviewAvailableMipMaps"));
        Assert.NotNull(texBaseVmType.GetProperty("SpriteScale"));
        Assert.NotNull(texBaseVmType.GetProperty("PreviewZoomInCommand"));
        Assert.NotNull(texBaseVmType.GetProperty("PreviewZoomOutCommand"));
        Assert.NotNull(texBaseVmType.GetProperty("PreviewFitOnScreenCommand"));
        Assert.NotNull(texBaseVmType.GetProperty("PreviewScaleToRealSizeCommand"));

        // Validate TexturePreviewViewModel members
        var texVmType = presentationAssembly.GetType("Stride.Assets.Presentation.ViewModel.Preview.TexturePreviewViewModel")!;
        Assert.True(texBaseVmType.IsAssignableFrom(texVmType));
        Assert.NotNull(texVmType.GetProperty("PreviewTextureDepth"));
        Assert.NotNull(texVmType.GetProperty("PreviewDimension"));
        Assert.NotNull(texVmType.GetProperty("PreviewTextureHeight"));
        Assert.NotNull(texVmType.GetProperty("PreviewTextureWidth"));
        Assert.NotNull(texVmType.GetProperty("SelectedCubePreviewMode"));
        Assert.NotNull(texVmType.GetProperty("SelectedDepth"));
        Assert.NotNull(texVmType.GetProperty("PreviewPreviousDepthCommand"));
        Assert.NotNull(texVmType.GetProperty("PreviewNextDepthCommand"));

        // Validate ModelPreviewViewModel members
        var modelVmType = presentationAssembly.GetType("Stride.Assets.Presentation.ViewModel.Preview.ModelPreviewViewModel")!;
        Assert.NotNull(modelVmType.GetProperty("ResetModelCommand"));

        // Validate MaterialPreviewViewModel members
        var materialVmType = presentationAssembly.GetType("Stride.Assets.Presentation.ViewModel.Preview.MaterialPreviewViewModel")!;
        Assert.NotNull(materialVmType.GetProperty("PrimitiveTypes"));
        Assert.NotNull(materialVmType.GetProperty("SelectedPrimitive"));

        // Validate SkyboxPreviewViewModel members
        var skyboxVmType = presentationAssembly.GetType("Stride.Assets.Presentation.ViewModel.Preview.SkyboxPreviewViewModel")!;
        Assert.NotNull(skyboxVmType.GetProperty("ResetModelCommand"));
        Assert.NotNull(skyboxVmType.GetProperty("Glossiness"));
        Assert.NotNull(skyboxVmType.GetProperty("Metalness"));

        // Validate HeightmapPreviewViewModel members
        var heightmapVmType = presentationAssembly.GetType("Stride.Assets.Presentation.ViewModel.Preview.HeightmapPreviewViewModel")!;
        Assert.True(texBaseVmType.IsAssignableFrom(heightmapVmType));
        Assert.NotNull(heightmapVmType.GetProperty("PreviewHeightmapLength"));
        Assert.NotNull(heightmapVmType.GetProperty("PreviewHeightmapWidth"));

        // Validate ProceduralModelPreviewViewModel members
        var proceduralVmType = presentationAssembly.GetType("Stride.Assets.Presentation.ViewModel.Preview.ProceduralModelPreviewViewModel")!;
        Assert.NotNull(proceduralVmType.GetProperty("ResetModelCommand"));
    }
}
