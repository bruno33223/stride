// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;

namespace Stride.Core.Presentation.Graph.ViewModel
{
    /// <summary>
    /// Base vertex used for node-based graphs.
    /// </summary>
    public class NodeVertex
    {
        public NodeVertex()
        {
            InputSlots = new ObservableCollection<object>();
            OutputSlots = new ObservableCollection<object>();
        }

        public long ID { get; set; }

        public virtual void AddOutgoing(NodeVertex target, object from, object to)
        {
        }

        /// <summary>
        /// Collection of input slots
        /// </summary>
        public virtual ObservableCollection<object> InputSlots { get; set; }

        /// <summary>
        /// Collection of output slots
        /// </summary>
        public virtual ObservableCollection<object> OutputSlots { get; set; }
    }
}
