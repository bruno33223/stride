// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Assets.Templates;
using Stride.Core.Presentation.Services;

namespace Stride.Assets.Editor.Services;

/// <summary>
/// This interface represents a modal dialog used to configure the class name, namespace and template when creating a new script.
/// </summary>
public interface IScriptNameDialog : IModalDialog
{
    /// <summary>
    /// Gets the class name entered and validated by the user.
    /// </summary>
    string ClassName { get; }

    /// <summary>
    /// Gets the namespace entered and validated by the user.
    /// </summary>
    string Namespace { get; }

    /// <summary>
    /// Gets the selected script template description.
    /// </summary>
    TemplateAssetDescription? ScriptTemplate { get; }
}
