// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.BuildEngine;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Rendering;
using Stride.Shaders.Compiler;

namespace Stride.Assets.Editor.Build
{
    public class GameStudioBuilderService : AssetBuilderService
    {
        public static string GlobalEffectLogPath;
        private readonly ManualResetEvent shaderLoadedEvent = new ManualResetEvent(false);
        private readonly EffectPriorityScheduler taskScheduler;
        private readonly EffectCompilerBase effectCompiler;
        private readonly IEditorDebugService debugService;
        private readonly IDebugPage assetBuilderServiceDebugPage;
        private readonly IDebugPage effectCompilerServiceDebugPage;
        private readonly bool createDebugTools;
        private int currentJobToken = -1;

        public GameStudioBuilderService(SessionViewModel sessionViewModel, GameSettingsProviderService settingsProvider, string buildDirectory, bool createDebugTools = true)
            : base(buildDirectory)
        {
            this.createDebugTools = createDebugTools;
            if (createDebugTools)
            {
                debugService = sessionViewModel.ServiceProvider.TryGet<IEditorDebugService>();
                assetBuilderServiceDebugPage = debugService?.CreateLogDebugPage(GlobalLogger.GetLogger("AssetBuilderService"), "AssetBuilderService");
                effectCompilerServiceDebugPage = debugService?.CreateLogDebugPage(GlobalLogger.GetLogger("EffectCompilerCache"), "EffectCompilerCache");
            }

            SessionViewModel = sessionViewModel ?? throw new ArgumentNullException(nameof(sessionViewModel));

            var shaderImporter = new Stride.Editor.Build.StrideShaderImporter();
            var shaderBuildSteps = shaderImporter.CreateSystemShaderBuildSteps(sessionViewModel);
            if (shaderBuildSteps != null)
            {
                shaderBuildSteps.StepProcessed += ShaderBuildStepsStepProcessed;
                PushBuildUnit(new PrecompiledAssetBuildUnit(AssetBuildUnitIdentifier.Default, shaderBuildSteps, true));
            }

            Database = new GameStudioDatabase(this, settingsProvider);

            const string shaderBundleUrl = "/binary/editor/EditorShadersD3D11.bundle";
            if (VirtualFileSystem.FileExists(shaderBundleUrl))
            {
                Builder.ObjectDatabase.BundleBackend.LoadBundleFromUrl("EditorShadersD3D11", Builder.ObjectDatabase.ContentIndexMap, shaderBundleUrl, true).Wait();
            }

            taskScheduler = new EffectPriorityScheduler(ThreadPriority.BelowNormal, Math.Max(1, Environment.ProcessorCount / 2));
            TaskSchedulerSelector taskSchedulerSelector = (mixinTree, compilerParameters) => taskScheduler.GetOrCreatePriorityGroup(compilerParameters?.TaskPriority ?? 0);
            effectCompiler = (EffectCompilerBase)EffectCompilerFactory.CreateEffectCompiler(MicrothreadLocalDatabases.GetSharedDatabase(), taskSchedulerSelector: taskSchedulerSelector);

            StartPushNotificationsTask();
        }

        /// <summary>
        /// Gets the session view model attached to this building service.
        /// </summary>
        /// <value>The session view model.</value>
        public SessionViewModel SessionViewModel { get; private set; }

        public SessionViewModel Session => SessionViewModel;

        public GameStudioDatabase Database { get; }

        /// <summary>
        /// Gets the effect compiler.
        /// </summary>
        public IEffectCompiler EffectCompiler => effectCompiler;

        public string EffectLogPath => GlobalEffectLogPath;

        /// <summary>
        /// Gets whether this instance of <see cref="GameStudioBuilderService"/> has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        ///   The number of shader-compile tasks waiting in the queue. Test harnesses use this to
        ///   detect a quiescent state; production code should treat it as informational only.
        /// </summary>
        public int PendingShaderCompilationCount => taskScheduler.QueuedTaskCount;

        public override void Dispose()
        {
            base.Dispose();
            if (createDebugTools && debugService != null)
            {
                if (assetBuilderServiceDebugPage != null)
                    debugService.UnregisterDebugPage(assetBuilderServiceDebugPage);
                if (effectCompilerServiceDebugPage != null)
                    debugService.UnregisterDebugPage(effectCompilerServiceDebugPage);
            }
            if (!IsDisposed)
            {
                IsDisposed = true;
            }
        }

        private void ShaderBuildStepsStepProcessed(object sender, BuildStepEventArgs e)
        {
            shaderLoadedEvent.Set();
        }

        public void WaitForShaders()
        {
            shaderLoadedEvent.WaitOne();
        }

        private void StartPushNotificationsTask()
        {
            Task.Run(async () =>
            {
                while (!IsDisposed)
                {
                    await Task.Delay(500);
                    if (currentJobToken >= 0)
                    {
                        if (taskScheduler.QueuedTaskCount > 0)
                        {
                            SessionViewModel.Main?.Status?.NotifyBackgroundJobProgress(currentJobToken, taskScheduler.QueuedTaskCount, true);
                        }
                        else
                        {
                            SessionViewModel.Main?.Status?.NotifyBackgroundJobFinished(currentJobToken);
                            currentJobToken = -1;
                        }
                    }
                    else if (taskScheduler.QueuedTaskCount > 0)
                    {
                        currentJobToken = SessionViewModel.Main?.Status?.NotifyBackgroundJobStarted("Building effects ({0} in queue)", JobPriority.Editor) ?? -1;
                    }
                }
            });
        }
    }
}
