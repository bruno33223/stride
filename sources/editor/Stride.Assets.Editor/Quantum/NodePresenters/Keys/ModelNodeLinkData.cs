// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Assets.Models;
using Stride.Core;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Keys;

public static class ModelNodeLinkData
{
    public const string AvailableNodes = nameof(AvailableNodes);
    public static readonly PropertyKey<IEnumerable<NodeInformation>> Key = new(AvailableNodes, typeof(ModelNodeLinkData));
}
