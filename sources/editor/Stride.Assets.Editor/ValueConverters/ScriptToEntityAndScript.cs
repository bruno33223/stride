// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Globalization;
using Stride.Engine;

namespace Stride.Assets.Editor.ValueConverters
{
    public class ScriptToEntityAndScript
    {
        public static readonly ScriptToEntityAndScript Instance = new();

        public virtual object Convert(object? value, Type? targetType = null, object? parameter = null, CultureInfo? culture = null)
        {
            var script = value as ScriptComponent;
            return script != null && script.Entity != null ? $"{script.Entity.Name}.{script.GetType().Name}" : string.Empty;
        }
    }
}
