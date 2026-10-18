// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Services;

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// An interface representing a modal dialog to fix broken asset references.
/// </summary>
public interface IFixReferencesDialog : IModalDialog
{
    /// <summary>
    /// Applies the fixes decided by the user to the actual objects.
    /// </summary>
    void ApplyReferenceFixes();
}
