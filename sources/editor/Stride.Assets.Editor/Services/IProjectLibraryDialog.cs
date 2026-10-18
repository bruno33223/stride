// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Presentation.Services;

namespace Stride.Assets.Editor.Services;

/// <summary>
/// Interface for the project library template dialog.
/// </summary>
public interface IProjectLibraryDialog : IModalDialog
{
    string LibraryName { get; set; }

    string Namespace { get; set; }

    bool HasError { get; set; }

    Func<string, bool> LibNameInputValidator { get; set; }

    DialogResult Result { get; }
}
