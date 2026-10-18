// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.BraceMatching;
using RoslynPad.Roslyn.Diagnostics;
using RoslynPad.Roslyn.QuickInfo;
using Stride.Core.Mathematics;

namespace Stride.Assets.Presentation.AssetEditors.ScriptEditor
{
    /// <summary>
    /// A code text editor with intellisense connected to our Roslyn workspace.
    /// </summary>
    public class SimpleCodeTextEditor
    {
        private RoslynWorkspace? workspace;
        private DocumentId? documentId;
        private SourceTextContainer? sourceTextContainer;

        private object? syntaxHighlightingColorizer;
        private object? textMarkerService;
        private object? contextActionsRenderer;
        private object? contextActionProvider;
        private IQuickInfoProvider? quickInfoProvider;

        private object? braceMatcherHighlighter;
        private IBraceMatchingService? braceMatchingService;
        private CancellationTokenSource? braceMatchingCts;

        private string text = string.Empty;
        private int caretOffset;
        private object? contextActionsIcon;

        public static readonly string ContextActionsIconPropertyName = nameof(ContextActionsIcon);

        public object? ContextActionsIcon
        {
            get => contextActionsIcon;
            set
            {
                contextActionsIcon = value;
                OnContextActionsIconChanged(value);
            }
        }

        public string Text
        {
            get => text;
            set
            {
                text = value ?? string.Empty;
                TextChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public int CaretOffset
        {
            get => caretOffset;
            set
            {
                if (caretOffset != value)
                {
                    caretOffset = value;
                    CaretOnPositionChanged(this, EventArgs.Empty);
                }
            }
        }

        public object? Document { get; set; }

        public object? CompletionProvider { get; set; }

        public Func<object, Task>? AsyncToolTipRequest { get; set; }

        public DocumentId? DocumentId => documentId;

        public RoslynWorkspace? Workspace => workspace;

        public SourceTextContainer? SourceTextContainer => sourceTextContainer;

        public event EventHandler? TextChanged;

        public SimpleCodeTextEditor()
        {
        }

        private void OnContextActionsIconChanged(object? icon)
        {
            if (contextActionsRenderer != null)
            {
                // Hook to context actions renderer if available
            }
        }

        /// <summary>
        /// Connects the text editor to a roslyn document and a <see cref="SourceTextContainer"/>. This will setup syntax highlighting, intellisense, etc.
        /// </summary>
        public void BindSourceTextContainer(RoslynWorkspace workspace, SourceTextContainer sourceTextContainer, DocumentId documentId)
        {
            this.workspace = workspace;
            this.sourceTextContainer = sourceTextContainer;
            this.documentId = documentId;

            Document = sourceTextContainer;

            var host = workspace?.GetType().GetProperty("Host")?.GetValue(workspace);
            if (host != null)
            {
                var getServiceMethod = host.GetType().GetMethod("GetService");
                if (getServiceMethod != null)
                {
                    quickInfoProvider = getServiceMethod.MakeGenericMethod(typeof(IQuickInfoProvider)).Invoke(host, null) as IQuickInfoProvider;
                    braceMatchingService = getServiceMethod.MakeGenericMethod(typeof(IBraceMatchingService)).Invoke(host, null) as IBraceMatchingService;
                }
            }

            AsyncToolTipRequest = ProcessAsyncToolTipRequest;
        }

        /// <summary>
        /// Disconnects the text editor from a roslyn document.
        /// </summary>
        public void Unbind()
        {
            AsyncToolTipRequest = null;
            contextActionsRenderer = null;
            contextActionProvider = null;
            CompletionProvider = null;
            syntaxHighlightingColorizer = null;
            textMarkerService = null;
            braceMatcherHighlighter = null;
            braceMatchingService = null;

            workspace = null;
            sourceTextContainer = null;
            documentId = null;
        }

        public void ProcessDiagnostics(DiagnosticsChangedArgs args)
        {
            if (args.DocumentId != documentId)
                return;

            ProcessDiagnosticsOnUiThread(args);
        }

        private void ProcessDiagnosticsOnUiThread(DiagnosticsChangedArgs args)
        {
            var addedDiagnostics = args.AddedDiagnostics;
            if (addedDiagnostics == null)
                return;

            foreach (var diagnosticData in addedDiagnostics)
            {
                if (diagnosticData.Severity == DiagnosticSeverity.Hidden || diagnosticData.IsSuppressed)
                {
                    continue;
                }

                if (sourceTextContainer != null)
                {
                    var span = diagnosticData.GetTextSpan(sourceTextContainer.CurrentText);
                    if (span == null)
                    {
                        continue;
                    }

                    var color = GetDiagnosticsColor(diagnosticData);
                    _ = color;
                }
            }
        }

        private async void CaretOnPositionChanged(object? sender, EventArgs eventArgs)
        {
            braceMatchingCts?.Cancel();

            if (braceMatchingService == null || workspace == null || documentId == null)
                return;

            var cts = new CancellationTokenSource();
            var token = cts.Token;
            braceMatchingCts = cts;

            var document = workspace.CurrentSolution.GetDocument(documentId);
            if (document == null)
                return;

            try
            {
                var docText = await document.GetTextAsync(token).ConfigureAwait(false);
                var currentOffset = CaretOffset;
                if (currentOffset <= docText.Length)
                {
                    var result = await braceMatchingService.GetAllMatchingBracesAsync(document, currentOffset, token).ConfigureAwait(true);
                    _ = result;
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void ScrollToLine(int line)
        {
        }

        public void OnKeyDown(object? e)
        {
            TryJumpToBrace();
        }

        private void TryJumpToBrace()
        {
            if (braceMatcherHighlighter == null)
                return;

            var caret = CaretOffset;
            ScrollToLine(caret);
        }

        private bool TryJumpToPosition(BraceMatchingResult? position, int caret)
        {
            if (position != null)
            {
                if (position.Value.LeftSpan.Contains(caret))
                {
                    CaretOffset = position.Value.RightSpan.End;
                    return true;
                }

                if (position.Value.RightSpan.Contains(caret) || position.Value.RightSpan.End == caret)
                {
                    CaretOffset = position.Value.LeftSpan.Start;
                    return true;
                }
            }

            return false;
        }

        private static Color GetDiagnosticsColor(DiagnosticData diagnosticData)
        {
            switch (diagnosticData.Severity)
            {
                case DiagnosticSeverity.Info:
                    return Color.LimeGreen;
                case DiagnosticSeverity.Warning:
                    return Color.DodgerBlue;
                case DiagnosticSeverity.Error:
                    return Color.Red;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async Task ProcessAsyncToolTipRequest(object? arg)
        {
            if (workspace == null || documentId == null || quickInfoProvider == null)
                return;

            var document = workspace.CurrentSolution.GetDocument(documentId);
            if (document != null && arg is int position)
            {
                var info = await quickInfoProvider.GetItemAsync(document, position, CancellationToken.None).ConfigureAwait(true);
                _ = info;
            }
        }
    }
}
