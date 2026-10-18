// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Xunit;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.UI;
using Stride.Core.Assets.Editor.View.Behaviors;
using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Stride.Core.Assets.Editor.Tests
{
    public class TestUIViewModelsAndHierarchyIntegration
    {
        private static readonly Assembly AssetsEditorAssembly = typeof(UIEditorBaseViewModel).Assembly;

        [Fact]
        public void TestRequiredTypesExistInAssetsEditor()
        {
            var hierarchyItemType = AssetsEditorAssembly.GetType("Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels.UIHierarchyItemViewModel");
            Assert.True(hierarchyItemType != null, "UIHierarchyItemViewModel type must exist in Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels");

            var elementVmType = AssetsEditorAssembly.GetType("Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels.UIElementViewModel");
            Assert.True(elementVmType != null, "UIElementViewModel type must exist in Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels");

            var rootVmType = AssetsEditorAssembly.GetType("Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels.UIRootViewModel");
            Assert.True(rootVmType != null, "UIRootViewModel type must exist in Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels");

            var fromLibType = AssetsEditorAssembly.GetType("Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels.UIElementFromLibrary");
            Assert.True(fromLibType != null, "UIElementFromLibrary type must exist in Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels");

            var fromSysLibType = AssetsEditorAssembly.GetType("Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels.UIElementFromSystemLibrary");
            Assert.True(fromSysLibType != null, "UIElementFromSystemLibrary type must exist in Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels");

            var converterType = AssetsEditorAssembly.GetType("Stride.Assets.Editor.AssetEditors.UIEditor.Views.UIElementFactoryTypeConverter");
            Assert.True(converterType != null, "UIElementFactoryTypeConverter type must exist in Stride.Assets.Editor.AssetEditors.UIEditor.Views");
        }

        [Fact]
        public void TestHierarchyItemAndElementContracts()
        {
            var hierarchyItemType = AssetsEditorAssembly.GetType("Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels.UIHierarchyItemViewModel");
            Assert.NotNull(hierarchyItemType);
            Assert.True(typeof(IAddChildViewModel).IsAssignableFrom(hierarchyItemType), "UIHierarchyItemViewModel must implement IAddChildViewModel");
            Assert.True(typeof(IInsertChildViewModel).IsAssignableFrom(hierarchyItemType), "UIHierarchyItemViewModel must implement IInsertChildViewModel");

            var elementVmType = AssetsEditorAssembly.GetType("Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels.UIElementViewModel");
            Assert.NotNull(elementVmType);
            Assert.True(hierarchyItemType.IsAssignableFrom(elementVmType), "UIElementViewModel must inherit from UIHierarchyItemViewModel");
            Assert.True(typeof(IIsEditableViewModel).IsAssignableFrom(elementVmType), "UIElementViewModel must implement IIsEditableViewModel");
            Assert.True(typeof(IAssetPropertyProviderViewModel).IsAssignableFrom(elementVmType), "UIElementViewModel must implement IAssetPropertyProviderViewModel");

            var rootVmType = AssetsEditorAssembly.GetType("Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels.UIRootViewModel");
            Assert.NotNull(rootVmType);
            Assert.True(hierarchyItemType.IsAssignableFrom(rootVmType), "UIRootViewModel must inherit from UIHierarchyItemViewModel");
        }

        [Fact]
        public void TestUIElementFactoryTypeConverterLogic()
        {
            var converterType = AssetsEditorAssembly.GetType("Stride.Assets.Editor.AssetEditors.UIEditor.Views.UIElementFactoryTypeConverter");
            Assert.NotNull(converterType);

            var converterInstance = Activator.CreateInstance(converterType);
            Assert.NotNull(converterInstance);

            var convertMethod = converterType.GetMethod("Convert", [typeof(object), typeof(Type), typeof(object), typeof(CultureInfo)]);
            Assert.NotNull(convertMethod);

            var fromSysLibType = AssetsEditorAssembly.GetType("Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels.UIElementFromSystemLibrary");
            Assert.NotNull(fromSysLibType);
            var sysLibDummy = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(fromSysLibType);

            var sysResult = convertMethod.Invoke(converterInstance, [sysLibDummy, typeof(string), null, CultureInfo.InvariantCulture]);
            Assert.Equal("Standard library", sysResult);

            var fromUserLibType = AssetsEditorAssembly.GetType("Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels.UIElementFromLibrary");
            Assert.NotNull(fromUserLibType);
            var userLibDummy = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(fromUserLibType);

            var userResult = convertMethod.Invoke(converterInstance, [userLibDummy, typeof(string), null, CultureInfo.InvariantCulture]);
            Assert.Equal("User libraries", userResult);
        }
    }
}
