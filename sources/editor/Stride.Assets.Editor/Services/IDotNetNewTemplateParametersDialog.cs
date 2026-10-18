// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Presentation.Services;

namespace Stride.Assets.Editor.Services;

/// <summary>
/// This interface represents a modal dialog used to configure user-facing parameters for a dotnet new template.
/// </summary>
public interface IDotNetNewTemplateParametersDialog : IModalDialog
{
    /// <summary>
    /// Gets the parameter values collected from the dialog.
    /// </summary>
    Dictionary<string, string> Parameters { get; }
}
