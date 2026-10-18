// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.Graph.ViewModel
{
    /// <summary>
    /// Base edge used in node-based graphs.
    /// </summary>
    public class NodeEdge
    {
        public NodeEdge(NodeVertex source, NodeVertex target, double weight = 1)
        {
            Source = source;
            Target = target;
            Weight = weight;
        }

        public long ID { get; set; }

        public NodeVertex Source { get; set; }

        public NodeVertex Target { get; set; }

        public double Weight { get; set; }

        public object? SourceSlot { get; set; }

        public object? TargetSlot { get; set; }
    }
}
