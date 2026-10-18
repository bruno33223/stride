// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.MicroThreading;
using Stride.Core.Serialization.Contents;
using Stride.Assets;
using Stride.Editor.Preview;
using Stride.Editor.Build;
using Stride.Graphics;

namespace Stride.Assets.Editor.Build
{
    public class GameStudioDatabase : IDisposable
    {
        private readonly Dictionary<ObjectUrl, OutputObject> database = new Dictionary<ObjectUrl, OutputObject>();
        private readonly GameStudioBuilderService assetBuilderService;
        private readonly GameSettingsProviderService settingsProvider;
        internal readonly AssetCompilerContext CompilerContext = new AssetCompilerContext { CompilationContext = typeof(AssetCompilationContext) };
        private readonly MicroThreadLock databaseLock = new MicroThreadLock();
        private readonly GameSettingsAsset databaseGameSettings;
        internal readonly AssetDependenciesCompiler AssetDependenciesCompiler = new AssetDependenciesCompiler(typeof(EditorGameCompilationContext));
        private bool isDisposed;

        public GameStudioDatabase(GameStudioBuilderService assetBuilderService, GameSettingsProviderService settingsProvider)
        {
            this.assetBuilderService = assetBuilderService;
            this.settingsProvider = settingsProvider;

            CompilerContext.Platform = PlatformType.Windows;

            databaseGameSettings = GameSettingsFactory.Create();
            CompilerContext.SetGameSettingsAsset(databaseGameSettings);
            CompilerContext.CompilationContext = typeof(EditorGameCompilationContext);

            UpdateGameSettings(settingsProvider.CurrentGameSettings);
            settingsProvider.GameSettingsChanged += OnGameSettingsChanged;

            var session = assetBuilderService.SessionViewModel ?? assetBuilderService.Session;
            ((AssetDependencyManager)session.DependencyManager).AssetChanged += OnAssetChanged;          
        }

        private void OnAssetChanged(AssetItem sender, bool oldValue, bool newValue)
        {
        }

        public void Dispose()
        {
            isDisposed = true;
            databaseLock.Dispose();
            settingsProvider.GameSettingsChanged -= OnGameSettingsChanged;
            var session = assetBuilderService.SessionViewModel ?? assetBuilderService.Session;
            ((AssetDependencyManager)session.DependencyManager).AssetChanged -= OnAssetChanged;
            CompilerContext.Dispose();
            database.Clear();
        }

        public ILogger Logger { get; }

        public Task<ISyncLockable> ReserveSyncLock() => databaseLock.ReserveSyncLock();

        public Task<IDisposable> LockAsync() => databaseLock.LockAsync();

        public async Task<IDisposable> MountInCurrentMicroThread()
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(GameStudioDatabase));
            if (Scheduler.CurrentMicroThread == null) throw new InvalidOperationException("The database can only be mounted in a micro-thread.");

            var lockObject = await databaseLock.LockAsync();
            // Return immediately if the database was disposed when waiting for the lock
            if (isDisposed)
                return lockObject;

            MicrothreadLocalDatabases.MountDatabase(database.Yield());
            return lockObject;
        }

        public async Task Build(AssetItem asset, BuildDependencyType dependencyType = BuildDependencyType.Runtime)
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(GameStudioDatabase));

            var buildUnit = new EditorGameBuildUnit(asset, CompilerContext, AssetDependenciesCompiler);

            try
            {
                assetBuilderService.PushBuildUnit(buildUnit);
                await buildUnit.Wait();
            }
            catch (Exception e)
            {
                Logger?.Error($"An error occurred while building the scene: {e.Message}", e);
                return;
            }

            // Merge build result into the database
            using ((await databaseLock.ReserveSyncLock()).Lock())
            {
                if (isDisposed)
                    return;

                if (buildUnit.Failed)
                {
                    database.Remove(new ObjectUrl(UrlType.Content, asset.Location));
                }

                foreach (var outputObject in buildUnit.OutputObjects)
                {
                    database[outputObject.Key] = outputObject.Value;
                }
            }
        }

        private void OnGameSettingsChanged(object sender, GameSettingsChangedEventArgs e)
        {
            UpdateGameSettings(e.GameSettings);
        }

        private void UpdateGameSettings(GameSettingsAsset currentGameSettings)
        {
            databaseGameSettings.GetOrCreate<EditorSettings>().RenderingMode = currentGameSettings.GetOrCreate<EditorSettings>().RenderingMode;
            databaseGameSettings.GetOrCreate<RenderingSettings>().ColorSpace = currentGameSettings.GetOrCreate<RenderingSettings>().ColorSpace;
            databaseGameSettings.GetOrCreate<Stride.Navigation.NavigationSettings>().Groups = currentGameSettings.GetOrDefault<Stride.Navigation.NavigationSettings>().Groups;
            databaseGameSettings.GetOrCreate<RenderingSettings>().DefaultGraphicsProfile = currentGameSettings.GetOrCreate<RenderingSettings>().DefaultGraphicsProfile;
        }
    }
}
