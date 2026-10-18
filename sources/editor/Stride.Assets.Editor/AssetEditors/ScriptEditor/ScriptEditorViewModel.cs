// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Roslyn.Diagnostics;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Annotations;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Assets.Editor.Annotations;

namespace Stride.Assets.Editor.AssetEditors.ScriptEditor
{
    /// <summary>
    /// View model for the script editor (using Roslyn & RoslynPad).
    /// </summary>
    [AssetEditorViewModel<ScriptSourceFileAssetViewModel>]
    public class ScriptEditorViewModel : AssetEditorViewModel
    {
        public ScriptEditorViewModel([NotNull] ScriptSourceFileAssetViewModel script)
            : base(script)
        {
            Code = StrideAssetsViewModel.Instance?.Code;
            SourceTextContainer = script.TextContainer;
        }

        /// <summary>
        /// The asset being edited.
        /// </summary>
        public override ScriptSourceFileAssetViewModel Asset => (ScriptSourceFileAssetViewModel)base.Asset;

        /// <summary>
        /// The code view model that manages roslyn states.
        /// </summary>
        public CodeViewModel Code { get; }

        /// <summary>
        /// The source text container used for editing.
        /// </summary>
        public SourceTextContainer SourceTextContainer { get; }

        /// <summary>
        /// The roslyn host.
        /// </summary>
        public RoslynHost RoslynHost { get; private set; }

        /// <summary>
        /// The roslyn workspace.
        /// </summary>
        public RoslynWorkspace Workspace { get; private set; }

        /// <summary>
        /// The roslyn document id.
        /// </summary>
        public DocumentId DocumentId { get; private set; }

        public event EventHandler DocumentOpened;
        public event EventHandler DocumentClosed;
        public event EventHandler<DiagnosticsChangedArgs> ProcessDiagnostics;

        /// <inheritdoc/>
        public sealed override async Task<bool> Initialize()
        {
            if (Code != null)
            {
                var projectWatcher = await Code.ProjectWatcher;
                RoslynHost = await projectWatcher.RoslynHost;
                Workspace = await Code.Workspace;
            }
            else
            {
                RoslynHost = ServiceProvider.TryGet<RoslynHost>() ?? new RoslynHost();
                Workspace = RoslynHost.Workspace;
            }

            Workspace.HostDocumentClosed += WorkspaceHostDocumentClosed;

            if (Asset.DocumentId == null)
                return false;

            // Open document
            var docId = await Asset.DocumentId;
            DocumentId = Workspace.OpenDocument(SourceTextContainer, docId,
                a => Dispatcher.Invoke(() => ProcessDiagnostics?.Invoke(this, a)));
            //Workspace.TrackDocument(await Asset.DocumentId, OnTextUpdated);

            // Failed? let's close editor right away
            if (DocumentId == null)
                return false;

            DocumentOpened?.Invoke(this, EventArgs.Empty);

            return true;
        }

        private void WorkspaceHostDocumentClosed(DocumentId documentId)
        {
            if (documentId == DocumentId)
            {
                Dispatcher.Invoke(() =>
                {
                    DocumentClosed?.Invoke(this, EventArgs.Empty);

                    // Document is closing, we assume we have been asked before to save
                    ServiceProvider.Get<IAssetEditorsManager>()?.CloseAssetEditorWindow(Asset, false);
                });
            }
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            if (DocumentId != null)
                Workspace?.CloseDocument(DocumentId);

            if (Workspace != null)
                Workspace.HostDocumentClosed -= WorkspaceHostDocumentClosed;

            base.Destroy();
        }
    }
}
