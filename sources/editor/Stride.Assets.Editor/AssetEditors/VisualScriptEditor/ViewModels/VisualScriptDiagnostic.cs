// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;

namespace Stride.Assets.Editor.ViewModels;

public struct VisualScriptDiagnostic
{
    public Diagnostic? RoslynDiagnostic { get; set; }
    public Guid? BlockId { get; set; }
    public Guid? LinkId { get; set; }

    public readonly string Message => RoslynDiagnostic?.GetMessage() ?? string.Empty;
    public readonly DiagnosticSeverity Severity => RoslynDiagnostic?.Severity ?? DiagnosticSeverity.Info;
}
