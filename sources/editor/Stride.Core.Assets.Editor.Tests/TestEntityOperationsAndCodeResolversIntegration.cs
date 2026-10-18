// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.ViewModels;
using Stride.UI;

namespace Stride.Core.Assets.Editor.Tests
{
    public class TestEntityOperationsAndCodeResolversIntegration
    {
        private static readonly Assembly AssetsPresentationAssembly = typeof(EntityHierarchyViewModel).Assembly;

        [Fact]
        public void TestRequiredTypesExistInAssetsPresentation()
        {
            var folderOpType = AssetsPresentationAssembly.GetType("Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels.EntityFolderOperation");
            Assert.True(folderOpType != null, "EntityFolderOperation deve existir em Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels");

            var resolverType = AssetsPresentationAssembly.GetType("Stride.Assets.Presentation.AssetEditors.EntityComponentSourceCodeResolver");
            Assert.True(resolverType != null, "EntityComponentSourceCodeResolver deve existir em Stride.Assets.Presentation.AssetEditors");

            var symbolExtType = AssetsPresentationAssembly.GetType("Stride.Assets.Presentation.AssetEditors.SymbolExtensions");
            Assert.True(symbolExtType != null, "SymbolExtensions deve existir em Stride.Assets.Presentation.AssetEditors");

            var nodeUpdaterType = AssetsPresentationAssembly.GetType("Stride.Assets.Presentation.NodePresenters.Updaters.UIAssetNodeUpdater");
            Assert.True(nodeUpdaterType != null, "UIAssetNodeUpdater deve existir em Stride.Assets.Presentation.NodePresenters.Updaters");
        }

        [Fact]
        public void TestEntityFolderOperationContractAndBehavior()
        {
            var folderOpType = AssetsPresentationAssembly.GetType("Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels.EntityFolderOperation");
            Assert.NotNull(folderOpType);

            Assert.True(typeof(DirtyingOperation).IsAssignableFrom(folderOpType), "EntityFolderOperation deve herdar de DirtyingOperation");

            var actionEnumType = folderOpType.GetNestedType("Action");
            Assert.NotNull(actionEnumType);
            Assert.True(actionEnumType.IsEnum, "EntityFolderOperation.Action deve ser um enum");

            var enumNames = Enum.GetNames(actionEnumType);
            Assert.Contains("FolderCreated", enumNames);
            Assert.Contains("FolderDeleted", enumNames);
            Assert.Contains("FolderRenamed", enumNames);

            // Verificar construtores
            var ctors = folderOpType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            Assert.True(ctors.Length >= 2, "EntityFolderOperation deve ter pelo menos 2 construtores públicos");

            // Criar mock de SceneViewModel (concreto de EntityHierarchyViewModel)
            var session = CreateTestSession();
            var assetItem = new AssetItem("TestScene", new SceneAsset());
            var assetVm = (EntityHierarchyViewModel)RuntimeHelpers.GetUninitializedObject(typeof(SceneViewModel));
            typeof(ViewModelBase).GetProperty("ServiceProvider", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
                .GetSetMethod(true)!.Invoke(assetVm, [session.ServiceProvider]);
            typeof(AssetViewModel).GetField("assetItem", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(assetVm, assetItem);

            var folderCreatedVal = Enum.Parse(actionEnumType, "FolderCreated");
            var folderDeletedVal = Enum.Parse(actionEnumType, "FolderDeleted");
            var folderRenamedVal = Enum.Parse(actionEnumType, "FolderRenamed");

            var ownerId = new AbsoluteId(assetVm.Id, Guid.NewGuid());

            // Construtor 1: (asset, action, folderPath, AbsoluteId)
            var ctor2 = folderOpType.GetConstructor([typeof(EntityHierarchyViewModel), actionEnumType, typeof(string), typeof(AbsoluteId)]);
            Assert.NotNull(ctor2);

            // Chamar com FolderRenamed no construtor de 2 parâmetros deve falhar com ArgumentException
            var targetInvEx = Assert.Throws<TargetInvocationException>(() => ctor2.Invoke([assetVm, folderRenamedVal, "FolderA", ownerId]));
            Assert.IsType<ArgumentException>(targetInvEx.InnerException);

            // Criar operação válida de FolderCreated
            var opCreated = (DirtyingOperation)ctor2.Invoke([assetVm, folderCreatedVal, "FolderA", ownerId]);
            Assert.NotNull(opCreated);
            var strCreated = opCreated.ToString();
            Assert.NotNull(strCreated);
            Assert.Contains("create", strCreated);
            Assert.Contains("FolderA", strCreated);

            // Construtor 2: (asset, action, folderPath, oldFolderName, AbsoluteId)
            var ctorRename = folderOpType.GetConstructor([typeof(EntityHierarchyViewModel), actionEnumType, typeof(string), typeof(string), typeof(AbsoluteId)]);
            Assert.NotNull(ctorRename);

            // Chamar com FolderCreated no construtor de rename deve falhar com ArgumentException
            var targetInvEx2 = Assert.Throws<TargetInvocationException>(() => ctorRename.Invoke([assetVm, folderCreatedVal, "FolderB", "FolderA", ownerId]));
            Assert.IsType<ArgumentException>(targetInvEx2.InnerException);

            // Criar operação válida de FolderRenamed
            var opRenamed = (DirtyingOperation)ctorRename.Invoke([assetVm, folderRenamedVal, "FolderB", "FolderOld", ownerId]);
            Assert.NotNull(opRenamed);
            var strRenamed = opRenamed.ToString();
            Assert.NotNull(strRenamed);
            Assert.Contains("rename", strRenamed);
            Assert.Contains("FolderOld", strRenamed);
            Assert.Contains("FolderB", strRenamed);

            // Testar congelamento (Freeze)
            var iopType = typeof(Stride.Core.Transactions.Operation).GetInterfaces().First(x => x.Name == "IOperation");
            var freezeMethod = iopType.GetMethod("Freeze", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            freezeMethod?.Invoke(opCreated, []);
            var isFrozenProp = typeof(Stride.Core.Transactions.Operation).GetProperty("IsFrozen", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var isFrozenVal = (bool)(isFrozenProp?.GetValue(opCreated) ?? false);
            Assert.True(isFrozenVal);
            Assert.Contains("Frozen", opCreated.ToString());
        }

        [Fact]
        public void TestEntityComponentSourceCodeResolverContractAndBehavior()
        {
            var resolverType = AssetsPresentationAssembly.GetType("Stride.Assets.Presentation.AssetEditors.EntityComponentSourceCodeResolver");
            Assert.NotNull(resolverType);

            var interfaceType = AssetsPresentationAssembly.GetType("Stride.Assets.Presentation.AssetEditors.IEntityComponentSourceCodeResolver")
                             ?? typeof(Stride.Assets.Editor.ViewModels.IEntityComponentSourceCodeResolver);
            Assert.NotNull(interfaceType);

            Assert.True(interfaceType.IsAssignableFrom(resolverType), "EntityComponentSourceCodeResolver deve implementar IEntityComponentSourceCodeResolver");

            // Instanciação com construtor padrão
            var instance = Activator.CreateInstance(resolverType);
            Assert.NotNull(instance);

            // Testar GetTypesFromSourceFile com null e caminho inexistente
            var getTypesMethod = resolverType.GetMethod("GetTypesFromSourceFile", [typeof(UFile)]);
            Assert.NotNull(getTypesMethod);

            var nullResult = (IEnumerable<Type>?)getTypesMethod.Invoke(instance, [null]);
            Assert.NotNull(nullResult);
            Assert.Empty(nullResult);

            var nonExistentResult = (IEnumerable<Type>?)getTypesMethod.Invoke(instance, [new UFile("/tmp/nonexistent/script.cs")]);
            Assert.NotNull(nonExistentResult);
            Assert.Empty(nonExistentResult);

            // Testar LatestCompilation e LatestCompilationChanged
            var latestCompProp = resolverType.GetProperty("LatestCompilation");
            Assert.NotNull(latestCompProp);
            Assert.Null(latestCompProp.GetValue(instance));

            var changedEvent = resolverType.GetEvent("LatestCompilationChanged");
            Assert.NotNull(changedEvent);

            var eventFired = false;
            EventHandler handler = (s, e) => { eventFired = true; };
            changedEvent.AddEventHandler(instance, handler);
            changedEvent.RemoveEventHandler(instance, handler);
            Assert.False(eventFired);

            // Testar SymbolExtensions.GetFullNamespace
            var symbolExtType = AssetsPresentationAssembly.GetType("Stride.Assets.Presentation.AssetEditors.SymbolExtensions");
            Assert.NotNull(symbolExtType);

            var getFullNsMethod = symbolExtType.GetMethod("GetFullNamespace", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(getFullNsMethod);
        }

        [Fact]
        public void TestUIAssetNodeUpdaterContractAndBehavior()
        {
            var updaterType = AssetsPresentationAssembly.GetType("Stride.Assets.Presentation.NodePresenters.Updaters.UIAssetNodeUpdater");
            Assert.NotNull(updaterType);

            Assert.True(typeof(AssetNodePresenterUpdaterBase).IsAssignableFrom(updaterType), "UIAssetNodeUpdater deve herdar de AssetNodePresenterUpdaterBase");

            // Instanciação com construtor padrão
            var updater = Activator.CreateInstance(updaterType);
            Assert.NotNull(updater);
        }

        private static SessionViewModel CreateTestSession()
        {
            var sp = new ViewModelServiceProvider();
            sp.RegisterService(new NullDispatcherService());
            sp.RegisterService(new UndoRedoService(100));

            var session = (SessionViewModel)RuntimeHelpers.GetUninitializedObject(typeof(SessionViewModel));
            typeof(ViewModelBase).GetProperty("ServiceProvider", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
                .GetSetMethod(true)!.Invoke(session, [sp]);

            return session;
        }
    }
}
