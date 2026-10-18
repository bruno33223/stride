// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Assets.Editor.Services;

public interface IPackagePickerDialog : IModalDialog
{
    bool AllowMultiSelection { get; set; }

    Func<PickablePackageViewModel, bool>? Filter { get; set; }

    IReadOnlyCollection<PickablePackageViewModel> SelectedPackages { get; }
}
