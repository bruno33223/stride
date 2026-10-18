// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.ViewModels;
using Stride.Core;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Keys;

public static class VisualScriptData
{
    public const string OwnerBlock = nameof(OwnerBlock);
    public static readonly PropertyKey<VisualScriptBlockViewModel> OwnerBlockKey = new(OwnerBlock, typeof(VisualScriptData));
}
