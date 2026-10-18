// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Microsoft.CodeAnalysis;
using Stride.Core.Presentation.Avalonia.Converters;

namespace Stride.Assets.Editor.Avalonia.Views;

public class MethodToDisplayName : OneWayValueConverter<MethodToDisplayName>
{
    private static readonly SymbolDisplayFormat MethodDisplayFormat
        = new SymbolDisplayFormat(
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
            memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
            parameterOptions: SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var span = new Span();
        var method = value as IMethodSymbol;
        if (method == null)
        {
            span.Inlines.Add(new Run("New method") { FontWeight = FontWeight.Bold });
        }
        else
        {
            var displayParts = method.ToDisplayParts(MethodDisplayFormat);

            // We want to highlight Method name, so separate them
            var groupedDisplayParts = GroupAdjacentBy(displayParts, (part1, part2) => GetDisplayCategory(part1) == GetDisplayCategory(part2));

            foreach (var displayPartGroup in groupedDisplayParts)
            {
                var displayPartGroupImmutable = displayPartGroup.ToImmutableArray();
                var run = new Run(displayPartGroupImmutable.ToDisplayString());

                ProcessRun(GetDisplayCategory(displayPartGroupImmutable.First()), run);

                span.Inlines.Add(run);
            }

            var typeRun = new Run($" (in {method.ContainingType.Name})");
            ProcessRun(SymbolDisplayStyle.Normal, typeRun);
            span.Inlines.Add(typeRun);
        }

        return span;
    }

    private void ProcessRun(SymbolDisplayStyle displayStyle, Run run)
    {
        switch (displayStyle)
        {
            case SymbolDisplayStyle.MethodName:
                run.FontWeight = FontWeight.Bold;
                break;
            default:
                if (Application.Current != null && Application.Current.TryGetResource("DisabledForegroundBrush", null, out var brush))
                    run.Foreground = brush as IBrush;
                break;
        }
    }

    private SymbolDisplayStyle GetDisplayCategory(SymbolDisplayPart symbolDisplayPart)
    {
        if (symbolDisplayPart.Kind == SymbolDisplayPartKind.MethodName)
            return SymbolDisplayStyle.MethodName;

        return SymbolDisplayStyle.Normal;
    }

    private static IEnumerable<IEnumerable<T>> GroupAdjacentBy<T>(IEnumerable<T> source, Func<T, T, bool> predicate)
    {
        using var e = source.GetEnumerator();
        if (!e.MoveNext()) yield break;
        var list = new List<T> { e.Current };
        var pred = e.Current;
        while (e.MoveNext())
        {
            if (predicate(pred, e.Current))
            {
                list.Add(e.Current);
            }
            else
            {
                yield return list;
                list = new List<T> { e.Current };
            }
            pred = e.Current;
        }
        yield return list;
    }

    private enum SymbolDisplayStyle
    {
        MethodName,
        Normal,
    }
}
