// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.ViewModels.AddAssetPolicies;
using Stride.Assets.Editor.Templates;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Sprite;
using Stride.Assets.SpriteFont;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Templates;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Sprites;
using Stride.SpriteStudio.Offline;
using Stride.SpriteStudio.Runtime;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestSpriteStudioViewModelsAndPoliciesIntegration
{
    private static readonly Assembly EditorAssembly = typeof(AssetTemplateGenerator).Assembly;

    private static (SessionViewModel session, ViewModelServiceProvider sp) CreateTestSession()
    {
        var sp = new ViewModelServiceProvider();
        sp.RegisterService(new NullDispatcherService());

        var session = (SessionViewModel)RuntimeHelpers.GetUninitializedObject(typeof(SessionViewModel));
        typeof(ViewModelBase).GetProperty("ServiceProvider", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .GetSetMethod(true)!.Invoke(session, [sp]);

        return (session, sp);
    }

    [Fact]
    public void TestSpriteSheetViewModel_TypeAndHierarchy()
    {
        var vmType = EditorAssembly.GetType("Stride.Assets.Editor.ViewModels.SpriteSheetViewModel");
        Assert.True(vmType != null, "Stride.Assets.Editor.ViewModels.SpriteSheetViewModel must exist in Stride.Assets.Editor assembly.");

        Assert.True(typeof(AssetViewModel<SpriteSheetAsset>).IsAssignableFrom(vmType),
            "SpriteSheetViewModel must inherit from AssetViewModel<SpriteSheetAsset>.");

        var attr = vmType.GetCustomAttribute<AssetViewModelAttribute<SpriteSheetAsset>>(inherit: false);
        Assert.NotNull(attr);

        var ctor = vmType.GetConstructor([typeof(AssetViewModel.ConstructorParameters)]);
        Assert.NotNull(ctor);
    }

    [Fact]
    public void TestSpriteFontViewModel_CommandsAndStructure()
    {
        var vmType = EditorAssembly.GetType("Stride.Assets.Editor.ViewModels.SpriteFontViewModel");
        Assert.True(vmType != null, "Stride.Assets.Editor.ViewModels.SpriteFontViewModel must exist in Stride.Assets.Editor assembly.");

        Assert.True(typeof(AssetViewModel<SpriteFontAsset>).IsAssignableFrom(vmType),
            "SpriteFontViewModel must inherit from AssetViewModel<SpriteFontAsset>.");

        var attr = vmType.GetCustomAttribute<AssetViewModelAttribute<SpriteFontAsset>>(inherit: false);
        Assert.NotNull(attr);

        var fontCatField = vmType.GetField("FontCategory", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(fontCatField);
        Assert.Equal("Font", fontCatField.GetValue(null));

        var charCatField = vmType.GetField("CharactersCategory", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(charCatField);
        Assert.Equal("Characters", charCatField.GetValue(null));

        var cmdProp = vmType.GetProperty("GeneratePrecompiledFontCommand", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(cmdProp);
        Assert.True(typeof(ICommandBase).IsAssignableFrom(cmdProp.PropertyType),
            "GeneratePrecompiledFontCommand property must be of type ICommandBase.");
    }

    [Fact]
    public void TestPrecompiledSpriteFontViewModel_TypeAndHierarchy()
    {
        var vmType = EditorAssembly.GetType("Stride.Assets.Editor.ViewModels.PrecompiledSpriteFontViewModel");
        Assert.True(vmType != null, "Stride.Assets.Editor.ViewModels.PrecompiledSpriteFontViewModel must exist in Stride.Assets.Editor assembly.");

        Assert.True(typeof(AssetViewModel<PrecompiledSpriteFontAsset>).IsAssignableFrom(vmType),
            "PrecompiledSpriteFontViewModel must inherit from AssetViewModel<PrecompiledSpriteFontAsset>.");

        var attr = vmType.GetCustomAttribute<AssetViewModelAttribute<PrecompiledSpriteFontAsset>>(inherit: false);
        Assert.NotNull(attr);

        var ctor = vmType.GetConstructor([typeof(AssetViewModel.ConstructorParameters)]);
        Assert.NotNull(ctor);
    }

    [Fact]
    public void TestSpriteStudioModelViewModel_TypeAndHierarchy()
    {
        var vmType = EditorAssembly.GetType("Stride.Assets.Editor.ViewModels.SpriteStudioModelViewModel");
        Assert.True(vmType != null, "Stride.Assets.Editor.ViewModels.SpriteStudioModelViewModel must exist in Stride.Assets.Editor assembly.");

        var importedBaseType = EditorAssembly.GetType("Stride.Assets.Editor.ViewModels.ImportedAssetViewModel`1")
            ?? typeof(AssetViewModel).Assembly.GetType("Stride.Core.Assets.Presentation.ViewModels.ImportedAssetViewModel`1");

        Assert.True(importedBaseType != null, "ImportedAssetViewModel<T> must exist.");

        var genericImported = importedBaseType.MakeGenericType(typeof(SpriteStudioModelAsset));
        Assert.True(genericImported.IsAssignableFrom(vmType),
            "SpriteStudioModelViewModel must inherit from ImportedAssetViewModel<SpriteStudioModelAsset>.");

        var attr = vmType.GetCustomAttribute<AssetViewModelAttribute<SpriteStudioModelAsset>>(inherit: false);
        Assert.NotNull(attr);

        var getImporterMethod = vmType.GetMethod("GetImporter", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(getImporterMethod);

        var updateFromSourceMethod = vmType.GetMethod("UpdateAssetFromSource", BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(SpriteStudioModelAsset)], null);
        Assert.NotNull(updateFromSourceMethod);
    }

    [Fact]
    public void TestSpriteSheetFromFileTemplateGenerator_IdsAndSupport()
    {
        var generatorType = EditorAssembly.GetType("Stride.Assets.Editor.Templates.SpriteSheetFromFileTemplateGenerator");
        Assert.True(generatorType != null, "Stride.Assets.Editor.Templates.SpriteSheetFromFileTemplateGenerator must exist.");

        Assert.True(typeof(AssetFromFileTemplateGenerator).IsAssignableFrom(generatorType),
            "SpriteSheetFromFileTemplateGenerator must inherit from AssetFromFileTemplateGenerator.");

        var defaultField = generatorType.GetField("Default", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(defaultField);
        var generator = defaultField.GetValue(null) as ITemplateGenerator;
        Assert.NotNull(generator);

        var spriteSheetIdField = generatorType.GetField("SpriteSheetId", BindingFlags.Public | BindingFlags.Static);
        var uiSpriteSheetIdField = generatorType.GetField("UISpriteSheetId", BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(spriteSheetIdField);
        Assert.NotNull(uiSpriteSheetIdField);

        var spriteSheetId = (Guid)spriteSheetIdField.GetValue(null)!;
        var uiSpriteSheetId = (Guid)uiSpriteSheetIdField.GetValue(null)!;

        Assert.Equal(new Guid("3778061A-5621-46F0-9091-B7C7E725237D"), spriteSheetId);
        Assert.Equal(new Guid("EFA882D2-BA27-4E81-8553-83187DDCD6D2"), uiSpriteSheetId);

        var spriteDesc = new TemplateAssetDescription { Id = spriteSheetId };
        var uiSpriteDesc = new TemplateAssetDescription { Id = uiSpriteSheetId };
        var otherDesc = new TemplateAssetDescription { Id = Guid.NewGuid() };

        var isSupportingMethod = generatorType.GetMethod("IsSupportingTemplate", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(isSupportingMethod);

        Assert.True((bool)isSupportingMethod.Invoke(generator, [spriteDesc])!);
        Assert.True((bool)isSupportingMethod.Invoke(generator, [uiSpriteDesc])!);
        Assert.False((bool)isSupportingMethod.Invoke(generator, [otherDesc])!);
    }

    [Fact]
    public void TestAddSpriteStudioModelAssetPolicy_TypeAndAcceptance()
    {
        var policyType = EditorAssembly.GetType("Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.ViewModels.AddAssetPolicies.AddSpriteStudioModelAssetPolicy");
        Assert.True(policyType != null, "Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.ViewModels.AddAssetPolicies.AddSpriteStudioModelAssetPolicy must exist.");

        var vmType = EditorAssembly.GetType("Stride.Assets.Editor.ViewModels.SpriteStudioModelViewModel");
        Assert.NotNull(vmType);

        var basePolicyType = typeof(CreateComponentPolicyBase<,>).MakeGenericType(typeof(SpriteStudioModelAsset), vmType);
        Assert.True(basePolicyType.IsAssignableFrom(policyType),
            "AddSpriteStudioModelAssetPolicy must inherit from CreateComponentPolicyBase<SpriteStudioModelAsset, SpriteStudioModelViewModel>.");

        var policy = Activator.CreateInstance(policyType);
        Assert.NotNull(policy);

        var acceptMethod = policyType.GetMethod("Accept", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(acceptMethod);

        Assert.True((bool)acceptMethod.Invoke(policy, [typeof(SpriteStudioModelAsset)])!);
        Assert.False((bool)acceptMethod.Invoke(policy, [typeof(SpriteSheetAsset)])!);
        Assert.False((bool)acceptMethod.Invoke(policy, [typeof(SpriteFontAsset)])!);
    }
}
