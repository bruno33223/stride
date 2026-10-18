// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Roslyn;

namespace Stride.Assets.Presentation.AssetEditors.ScriptEditor
{
    public class ScriptTextEditor
    {
        private SimpleCodeTextEditor codeEditor;
        private SourceTextContainer? sourceTextContainer;
        private RoslynWorkspace? workspace;
        private ProjectId? projectId;
        private string text = string.Empty;

        public RoslynWorkspace? Workspace
        {
            get => workspace;
            set
            {
                if (workspace != value)
                {
                    workspace = value;
                    OnRecreateScript(this);
                }
            }
        }

        public ProjectId? ProjectId
        {
            get => projectId;
            set
            {
                if (projectId != value)
                {
                    projectId = value;
                    OnRecreateScript(this);
                }
            }
        }

        public DocumentId? DocumentId { get; private set; }

        public string Text
        {
            get => text;
            set
            {
                text = value ?? string.Empty;
                if (codeEditor != null)
                {
                    codeEditor.Text = text;
                }
            }
        }

        public bool HasChangesToValidate { get; set; }

        public SimpleCodeTextEditor CodeEditor => codeEditor;

        public ScriptTextEditor()
        {
            codeEditor = new SimpleCodeTextEditor();
        }

        public virtual void OnApplyTemplate()
        {
            SetupCodeEditor();
        }

        protected virtual void OnValidating(object? e)
        {
            if (sourceTextContainer != null)
            {
                Text = sourceTextContainer.CurrentText.ToString();
            }
        }

        private static void OnRecreateScript(ScriptTextEditor scriptTextEditor)
        {
            scriptTextEditor.SetupCodeEditor();
        }

        private void HandleHandledKeyDown(object? sender, object? e)
        {
        }

        private void SetupCodeEditor()
        {
            if (workspace != null)
            {
                if (workspace == Workspace)
                {
                    return;
                }

                CleanupCodeEditor();
            }

            if (Workspace == null || codeEditor == null || ProjectId == null)
                return;

            this.workspace = Workspace;

            try
            {
                var currentWorkspace = Workspace;
                var currentProjectId = ProjectId;

                // Tentar adicionar o documento usando AddDocument se disponivel no workspace
                var addDocMethod = currentWorkspace.GetType().GetMethod("AddDocument", new[] { typeof(ProjectId), typeof(string), typeof(SourceCodeKind), typeof(TextLoader) })
                                   ?? currentWorkspace.GetType().GetMethod("AddDocument", new[] { typeof(ProjectId), typeof(string) });

                DocumentId? newDocId = null;
                if (addDocMethod != null)
                {
                    var pars = addDocMethod.GetParameters();
                    if (pars.Length == 4)
                    {
                        var loader = TextLoader.From(TextAndVersion.Create(SourceText.From(Text ?? string.Empty), VersionStamp.Create()));
                        newDocId = addDocMethod.Invoke(currentWorkspace, new object[] { currentProjectId, $"script-{Guid.NewGuid()}.cs", SourceCodeKind.Script, loader }) as DocumentId;
                    }
                    else if (pars.Length == 2)
                    {
                        newDocId = addDocMethod.Invoke(currentWorkspace, new object[] { currentProjectId, $"script-{Guid.NewGuid()}.cs" }) as DocumentId;
                    }
                }

                if (newDocId == null)
                {
                    newDocId = DocumentId.CreateNewId(currentProjectId, $"script-{Guid.NewGuid()}.cs");
                }

                DocumentId = newDocId;

                var doc = currentWorkspace.CurrentSolution.GetDocument(newDocId);
                var container = doc?.GetTextAsync().Result?.Container;
                if (container != null)
                {
                    sourceTextContainer = container;
                    sourceTextContainer.TextChanged += SourceTextContainer_TextChanged;

                    var openDocMethod = currentWorkspace.GetType().GetMethod("OpenDocument", new[] { typeof(SourceTextContainer), typeof(DocumentId), typeof(Action<RoslynPad.Roslyn.Diagnostics.DiagnosticsChangedArgs>) });
                    openDocMethod?.Invoke(currentWorkspace, new object?[] { sourceTextContainer, DocumentId, (Action<RoslynPad.Roslyn.Diagnostics.DiagnosticsChangedArgs>)(a => codeEditor.ProcessDiagnostics(a)) });

                    codeEditor.BindSourceTextContainer(currentWorkspace, sourceTextContainer, DocumentId);
                }
            }
            catch
            {
                // Silently ignore during headless or mock workspace initialization
            }
        }

        private void CleanupCodeEditor()
        {
            if (workspace != null && DocumentId != null)
            {
                try
                {
                    var closeDocMethod = workspace.GetType().GetMethod("CloseDocument", new[] { typeof(DocumentId) });
                    closeDocMethod?.Invoke(workspace, new object[] { DocumentId });
                }
                catch { }

                codeEditor?.Unbind();

                if (sourceTextContainer != null)
                {
                    sourceTextContainer.TextChanged -= SourceTextContainer_TextChanged;
                    sourceTextContainer = null;
                }

                try
                {
                    var removeDocMethod = workspace.GetType().GetMethod("RemoveDocument", new[] { typeof(DocumentId) });
                    removeDocMethod?.Invoke(workspace, new object[] { DocumentId });
                }
                catch { }

                DocumentId = null;
            }
        }

        private void SourceTextContainer_TextChanged(object? sender, TextChangeEventArgs e)
        {
            HasChangesToValidate = true;
        }
    }
}
