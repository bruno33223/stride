// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Templates;
using Stride.Core.Presentation.Services;

namespace Stride.Assets.Editor.Services;

/// <summary>
/// Interface for the 3D model asset template dialog.
/// </summary>
public interface IModelAssetTemplateDialog : IModalDialog
{
    ImportModelFromFileViewModel Parameters { get; set; }

    DialogResult Result { get; }
}
