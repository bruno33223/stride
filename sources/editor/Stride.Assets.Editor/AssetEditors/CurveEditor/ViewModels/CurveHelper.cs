// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;

namespace Stride.Assets.Editor.ViewModels;

internal static class CurveHelper
{
    public static ControlPointViewModelBase? GetClosestPoint([ItemNotNull, NotNull] IEnumerable<ControlPointViewModelBase> points, Vector2 position, double maximumDistance = double.PositiveInfinity)
    {
        ControlPointViewModelBase? closest = null;
        var closestDistance = double.MaxValue;
        foreach (var p in points)
        {
            var diff = p.ActualPoint - position;
            var distance = (double)diff.LengthSquared();
            if (distance < closestDistance && distance < maximumDistance * maximumDistance)
            {
                closest = p;
                closestDistance = distance;
            }
        }
        return closest;
    }
}
