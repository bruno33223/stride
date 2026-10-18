// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Stride.Assets.Editor.AssetEditors.ScriptEditor;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Scripts;
using Stride.Core.Annotations;
using Stride.Core.Assets;
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.TextAccessors;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;
using Stride.Core.Translation;

namespace Stride.Assets.Editor.ViewModels
{
    [AssetViewModel<ScriptSourceFileAsset>]
    public class ScriptSourceFileAssetViewModel : SourceCodeViewModel<ScriptSourceFileAsset>
    {
        private readonly StringBuilder mirroredText = new StringBuilder();
        private RoslynWorkspace workspace;
        private bool textReloading;
        private bool existsOnDisk;
        private bool hasExternalChanges;
        private readonly CancellationTokenSource cancellationToken = new CancellationTokenSource();

        public ScriptSourceFileAssetViewModel(ConstructorParameters parameters) : base(parameters)
        {
        }

        public UFile FullPath => AssetItem.FullPath;

        /// <summary>
        /// The document id. It is built asynchronously when Roslyn workspace is ready.
        /// </summary>
        /// <remarks>
        /// Meant to be used from code, not UI binding.
        /// </remarks>
        public Task<DocumentId> DocumentId { get; private set; }

        public SourceTextContainer TextContainer { get; private set; }

        public ScriptTextDocument TextDocument { get; private set; }

        public override void Initialize()
        {
            base.Initialize();

            // Duplicate the text so it is more easily accessible from a different thread
            var initialText = Asset.TextAccessor.Get() ?? string.Empty;
            lock (mirroredText)
            {
                mirroredText.Clear();
                mirroredText.Append(initialText);
            }

            // Text document and container needs to be owned by the UI thread
            TextContainer = Dispatcher.Invoke(() =>
            {
                // Load initial text from asset and create a text document
                var textDoc = new ScriptTextDocument(initialText);
                textDoc.UndoStack.PropertyChanged += UndoStackOnPropertyChanged;

                // Replace the text accessor with one using custom save logic
                Asset.TextAccessor = new ScriptTextAccessor(this);

                textDoc.Changed += TextDocumentOnChanged;
                TextDocument = textDoc;

                return new ScriptSourceTextContainer(textDoc);
            });

            // Track document
            TrackDocument();
        }

        private void TextDocumentOnChanged(object sender, ScriptDocumentChangeEventArgs documentChangeEventArgs)
        {
            lock (mirroredText)
            {
                mirroredText.Remove(documentChangeEventArgs.Offset, documentChangeEventArgs.RemovalLength);
                mirroredText.Insert(documentChangeEventArgs.Offset, documentChangeEventArgs.InsertedText);
            }
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            if (DocumentId != null)
                UntrackDocument();

            DocumentId = null;

            // Dispose of text container
            if (TextContainer is IDisposable disposableContainer)
            {
                disposableContainer.Dispose();
            }

            // Restore text accessor on the asset
            if (Asset.TextAccessor is ScriptTextAccessor)
                Asset.TextAccessor = null;

            base.Destroy();
        }

        protected override void UpdateIsDeletedStatus()
        {
            base.UpdateIsDeletedStatus();

            if (IsDeleted)
            {
                var documentIdToRemove = DocumentId?.Result;
                if (documentIdToRemove != null)
                {
                    UntrackDocument();
                    workspace?.RemoveDocument(documentIdToRemove);
                }
            }
            else
            {
                if (!Initializing && DocumentId == null)
                {
                    // This will create document if necessary
                    TrackDocument();
                }
            }
        }

        protected virtual async Task UpdateAssetFromSource(Logger logger)
        {
            if (DocumentId == null)
                return;

            await DocumentId;

            var document = workspace?.GetDocument(DocumentId?.Result);
            if (document == null)
                return;

            object reloadState = null;
            if (AllowReload(document, ref reloadState))
            {
                // Reset file
                workspace.UpdateDocument(DocumentId.Result, new FileTextLoader(document.FilePath, null));

                // Get updated document
                document = workspace.GetDocument(DocumentId.Result);

                // Set new text
                TextDocument.Text = (await document.GetTextAsync()).ToString();

                // Update dirty state
                hasExternalChanges = false;
                existsOnDisk = File.Exists(FullPath);

                // This will trigger UpdateDirtiness
                TextDocument.UndoStack.MarkAsOriginalFile();
            }
        }

        protected override void UpdateDirtiness(bool value)
        {
            // Completely ignore dirty state that is given and use IsTextDocumentDirty instead
            // if we don't the file will still be marked dirty after saving it from the text editor,
            // since it doesn't update the action stack save point when saving only a single asset
            UpdateDirtiness();
        }

        /// <summary>
        /// Updates the dirtyness based on the value of <see cref="IsTextDocumentDirty"/>
        /// </summary>
        protected void UpdateDirtiness()
        {
            base.UpdateDirtiness(IsTextDocumentDirty());
        }

        protected override void OnPropertyChanged(params string[] propertyNames)
        {
            base.OnPropertyChanged(propertyNames);

            if (propertyNames.Contains(nameof(Url)))
            {
                OnFileMoved();
            }
        }

        public virtual void OnSessionSaved()
        {
            OnFileSaved();
        }

        internal void OnFileMoved()
        {
            Dispatcher.Invoke(() =>
            {
                // Update source file location with latest FullPath (in case it changed)
                workspace?.UpdateFilePath(DocumentId?.Result, FullPath.ToOSPath());

                // TODO: This doesn't take into account the case where you move and then undo,
                // to fix this case the file would need to check for modifications after it has been moved
                hasExternalChanges = true;
                UpdateDirtiness();
            });
        }

        /// <summary>
        /// Should be called when the contents of the file on disk are synchronized with it's contents currently in memory
        /// </summary>
        private void OnFileSaved()
        {
            TextDocument?.UndoStack.MarkAsOriginalFile();

            existsOnDisk = true;
            hasExternalChanges = false;
            UpdateDirtiness();
        }

        private void UndoStackOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!IsDestroyed && !textReloading && e.PropertyName == nameof(ScriptUndoStack.IsOriginalFile))
            {
                UpdateDirtiness();
            }
        }

        private bool IsTextDocumentDirty()
        {
            // Force dirty state when the file does not exist on disk
            if (!existsOnDisk)
                return true;

            if (hasExternalChanges)
                return true;

            return TextDocument != null && !TextDocument.UndoStack.IsOriginalFile;
        }

        private void TrackDocument()
        {
            // Make sure the path is correct
            AssetItem.UpdateSourceFolders();

            // Capture full path before going in a Task (might be renamed in between)
            var fullPath = AssetItem.FullPath.ToOSPath();

            DocumentId = Task.Run(async () =>
            {
                // Find DocumentId
                if (StrideAssetsViewModel.Instance != null)
                {
                    var strideAssets = await StrideAssetsViewModel.InstanceTask;
                    workspace = await strideAssets.Code.Workspace;
                }
                else
                {
                    workspace = ServiceProvider.TryGet<RoslynWorkspace>()
                                ?? ServiceProvider.TryGet<RoslynHost>()?.Workspace
                                ?? new RoslynHost().Workspace;
                }

                AssetItem.UpdateSourceFolders();
                var sourceContainer = AssetItem.Package?.Container as SolutionProject;
                var sourceProject = sourceContainer?.FullPath.ToOSPath();
                if (sourceProject == null && workspace?.CurrentSolution.Projects.Any() == true)
                {
                    sourceProject = workspace.CurrentSolution.Projects.First().FilePath;
                }

                // Wait for project to be loaded if available
                Project project = null;
                if (sourceProject != null && workspace != null)
                {
                    while (true)
                    {
                        cancellationToken.Token.ThrowIfCancellationRequested();

                        // Wait for project to be available
                        project = workspace.CurrentSolution.Projects.FirstOrDefault(x => x.FilePath == sourceProject);
                        if (project != null || !workspace.CurrentSolution.Projects.Any())
                            break;
                        await Task.Delay(10);
                    }
                }

                if (project == null && workspace != null)
                {
                    project = workspace.CurrentSolution.Projects.FirstOrDefault();
                    if (project == null)
                    {
                        var projId = ProjectId.CreateNewId();
                        var projInfo = ProjectInfo.Create(projId, VersionStamp.Create(), "DefaultProject", "DefaultAssembly", LanguageNames.CSharp);
                        workspace.AddProject(projInfo);
                        project = workspace.CurrentSolution.GetProject(projId);
                    }
                }

                // If possible, we use AbsoluteSourceLocation which is the path from where it was loaded (in case it moved afterwards)
                var foundDocumentId = workspace?.CurrentSolution.GetDocumentIdsWithFilePath(fullPath).FirstOrDefault();
                if (foundDocumentId == null && workspace != null && project != null)
                {
                    // New asset, let's create it in the workspace
                    foundDocumentId = workspace.AddDocument(project.Id, fullPath, loader: new ScriptTextLoader(this));
                }

                // Register to notifications in case of reloading or text updated
                if (foundDocumentId != null && workspace != null)
                {
                    workspace.TrackDocument(foundDocumentId, AllowReload, TextUpdated, ExternalChangesDetected);
                }

                // This is to make sure that the file will stay dirty until saved after just creating it (since it will only be stored in memory)
                existsOnDisk = File.Exists(FullPath);

                // Check if the file changed from the file on the disk
                if (existsOnDisk)
                {
                    try
                    {
                        var diskContent = File.ReadAllText(FullPath);
                        if (diskContent != Asset.Text)
                        {
                            // Prompt for reload from source file
                            Dispatcher.Invoke(() => UpdateAssetFromSource(new LoggerResult()));
                        }
                    }
                    catch (Exception)
                    {
                        // In this case the file either does not exist anymore or can somehow not be read, just mark file as dirty and continue as usual
                        hasExternalChanges = true;
                    }
                }

                UpdateDirtiness();

                return foundDocumentId;
            });
        }

        private void UntrackDocument()
        {
            if (DocumentId == null)
                return;

            DocumentId.ContinueWith(documentId => workspace?.UntrackDocument(documentId.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
            DocumentId = null;
        }

        #region Workspace events

        internal class ReloadState
        {
            public bool? AutoReloadDirty;
        }

        private bool AllowReload(Document newDocument, ref object state)
        {
            // Allow non-dirty files to reload automatically
            if (!IsDirty)
                return true;

            // Don't reload removed files
            if (!File.Exists(FullPath))
                return false;

            var reloadState = state as ReloadState;
            if (reloadState == null)
                state = reloadState = new ReloadState();

            var reloadMode = reloadState.AutoReloadDirty;

            var reloadAllowed = true;

            // User pressed YesToAll or NoToAll before
            // Let's return same value
            if (reloadMode.HasValue)
                reloadAllowed = reloadMode.Value;
            else
            {
                // Ask user
                var message = string.Format(
                            Tr._p("Message", "{0}\r\n\r\nThis file has been changed externally and has unsaved changes inside the editor.\r\nDo you want to reload it and lose your changes?"),
                            newDocument.FilePath);
                var dialogService = ServiceProvider.TryGet<IEditorDialogService>();
                if (dialogService != null)
                {
                    var buttons = DialogHelper.CreateButtons(new[] { "Yes", "Yes to all", "No", "No to all" });
                    var dialogTask = dialogService.MessageBoxAsync(message, buttons, MessageBoxImage.Question);
                    var dialogResult = dialogTask.GetAwaiter().GetResult();

                    switch (dialogResult)
                    {
                        case 0:
                        case 1:
                            reloadAllowed = true;
                            break;
                        case 2:
                            reloadState.AutoReloadDirty = reloadAllowed = true;
                            break;
                        case 3:
                            reloadAllowed = false;
                            break;
                        default:
                            reloadState.AutoReloadDirty = reloadAllowed = false;
                            break;
                    }
                }
            }

            // Mark the document as dirty since we didn't update to external changed
            if (!reloadAllowed)
            {
                hasExternalChanges = true;
                UpdateDirtiness();
            }

            return reloadAllowed;
        }

        // The text was updated
        private void TextUpdated(SourceText newSourceText, bool external)
        {
            Dispatcher.Invoke(() =>
            {
                textReloading = true;
                // If text didn't change, ignore (that's probably because we are the one who saved the new version of the file, or it's a "touch")
                if (TextContainer is ScriptSourceTextContainer scriptContainer)
                {
                    if (scriptContainer.CurrentText != newSourceText && !scriptContainer.CurrentText.ContentEquals(newSourceText))
                    {
                        scriptContainer.UpdateText(newSourceText);
                    }
                }

                // On external changes, reset the file to a non-dirty state
                if (external)
                {
                    TextDocument?.UndoStack.MarkAsOriginalFile();
                    hasExternalChanges = false;
                }

                textReloading = false;

                UpdateDirtiness();
            });
        }

        private void ExternalChangesDetected()
        {
            existsOnDisk = File.Exists(FullPath);
            UpdateDirtiness();
        }

        #endregion

        /// <summary>
        /// A Roslyn <see cref="TextLoader"/> that uses the text container on the view model
        /// </summary>
        private class ScriptTextLoader : TextLoader
        {
            private readonly ScriptSourceFileAssetViewModel script;

            public ScriptTextLoader(ScriptSourceFileAssetViewModel script)
            {
                this.script = script;
            }

            [Obsolete]
            public override Task<TextAndVersion> LoadTextAndVersionAsync(Workspace? workspace, DocumentId? documentId, CancellationToken cancellationToken)
            {
                lock (script.mirroredText)
                {
                    var sourceText = SourceText.From(script.mirroredText.ToString());
                    return Task.FromResult(TextAndVersion.Create(sourceText, VersionStamp.Create()));
                }
            }
        }

        /// <summary>
        /// Class that uses the script's content through <see cref="TextDocument"/>
        /// </summary>
        private class ScriptTextAccessor : ITextAccessor
        {
            private readonly ScriptSourceFileAssetViewModel asset;

            public ScriptTextAccessor(ScriptSourceFileAssetViewModel asset)
            {
                this.asset = asset;
            }

            [NotNull]
            public string Get()
            {
                // Retrieve text from the editor thread
                lock (asset.mirroredText)
                {
                    return asset.mirroredText.ToString();
                }
            }

            public void Set([NotNull] string value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));

                // Set text on the editor thread
                if (asset.Dispatcher.CheckAccess())
                    asset.TextDocument.Text = value;
                else
                    asset.Dispatcher.InvokeAsync(() => asset.TextDocument.Text = value).Wait();
            }

            public async Task Save(Stream stream)
            {
                await asset.Session.Dispatcher.InvokeTask(async () =>
                {
                    using (var streamWriter = new StreamWriter(stream, Encoding.UTF8, 1024, true))
                    {
                        await streamWriter.WriteAsync(Get());
                    }
                });
            }

            [NotNull]
            public ISerializableTextAccessor GetSerializableVersion()
            {
                return new StringTextAccessor { Text = Get() };
            }
        }
    }

    public class ScriptDocumentChangeEventArgs : EventArgs
    {
        public int Offset { get; }
        public int RemovalLength { get; }
        public string InsertedText { get; }

        public ScriptDocumentChangeEventArgs(int offset, int removalLength, string insertedText)
        {
            Offset = offset;
            RemovalLength = removalLength;
            InsertedText = insertedText;
        }
    }

    public class ScriptUndoStack : INotifyPropertyChanged
    {
        private bool isOriginalFile = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsOriginalFile
        {
            get => isOriginalFile;
            set
            {
                if (isOriginalFile != value)
                {
                    isOriginalFile = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsOriginalFile)));
                }
            }
        }

        public void MarkAsOriginalFile()
        {
            IsOriginalFile = true;
        }

        public void MarkAsDirty()
        {
            IsOriginalFile = false;
        }
    }

    public class ScriptTextDocument
    {
        private string text;

        public event EventHandler<ScriptDocumentChangeEventArgs> Changed;

        public ScriptUndoStack UndoStack { get; } = new ScriptUndoStack();

        public ScriptTextDocument(string initialText = "")
        {
            text = initialText ?? string.Empty;
        }

        public string Text
        {
            get => text;
            set
            {
                var newText = value ?? string.Empty;
                if (text != newText)
                {
                    var oldLength = text.Length;
                    text = newText;
                    UndoStack.MarkAsDirty();
                    Changed?.Invoke(this, new ScriptDocumentChangeEventArgs(0, oldLength, newText));
                }
            }
        }
    }

    public sealed class ScriptSourceTextContainer : SourceTextContainer, IDisposable
    {
        private SourceText currentText;
        private readonly ScriptTextDocument document;

        public override SourceText CurrentText => currentText;

        public override event EventHandler<TextChangeEventArgs> TextChanged;

        public ScriptSourceTextContainer(ScriptTextDocument document)
        {
            this.document = document;
            currentText = SourceText.From(document.Text);
            document.Changed += DocumentOnChanged;
        }

        private void DocumentOnChanged(object sender, ScriptDocumentChangeEventArgs e)
        {
            var oldText = currentText;
            currentText = SourceText.From(document.Text);
            TextChanged?.Invoke(this, new TextChangeEventArgs(oldText, currentText));
        }

        public void UpdateText(SourceText newText)
        {
            var oldText = currentText;
            currentText = newText;
            document.Text = newText.ToString();
            TextChanged?.Invoke(this, new TextChangeEventArgs(oldText, newText));
        }

        public void Dispose()
        {
            document.Changed -= DocumentOnChanged;
        }
    }
}
