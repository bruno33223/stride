// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Data.Converters;

namespace Stride.Core.Presentation.Avalonia.Converters;

/// <summary>
/// An abstract implementation of <see cref="MultiValueConverterBase{T}"/> for one-way multi-value conversions.
/// </summary>
/// <typeparam name="T">The type of <see cref="IMultiValueConverter"/> being implemented.</typeparam>
public abstract class OneWayMultiValueConverter<T> : MultiValueConverterBase<T>
    where T : OneWayMultiValueConverter<T>, IMultiValueConverter, new()
{
}
