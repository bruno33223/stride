// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Microsoft.CodeAnalysis;

namespace Stride.Assets.Editor.Avalonia.Views;

public class SymbolToDisplayStringConverter : IValueConverter
{
    public static readonly SymbolToDisplayStringConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ISymbol symbol)
        {
            return symbol.ToDisplayString();
        }
        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public class SymbolToContainingTypeConverter : IValueConverter
{
    public static readonly SymbolToContainingTypeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ISymbol symbol)
        {
            if (symbol.ContainingType != null)
                return $"in {symbol.ContainingType.Name} ({symbol.ContainingAssembly?.Name})";
            if (symbol.ContainingNamespace != null)
                return $"in {symbol.ContainingNamespace.Name} ({symbol.ContainingAssembly?.Name})";
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public class MethodToDisplayNameConverter : IValueConverter
{
    private static readonly SymbolDisplayFormat MethodDisplayFormat = new(
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
        parameterOptions: SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    public static readonly MethodToDisplayNameConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IMethodSymbol method)
        {
            var methodDisplay = method.ToDisplayString(MethodDisplayFormat);
            var containing = method.ContainingType != null ? $" (in {method.ContainingType.Name})" : string.Empty;
            return $"{methodDisplay}{containing}";
        }
        return "+ New Method (Custom)";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public class NodeToSymbolNameConverter : IValueConverter
{
    public static readonly NodeToSymbolNameConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Stride.Core.Presentation.Quantum.ViewModels.NodeViewModel node)
        {
            var nameNode = node["Name_"] as Stride.Core.Presentation.Quantum.ViewModels.NodeViewModel;
            if (nameNode?.NodeValue != null)
                return nameNode.NodeValue.ToString();

            if (node.NodeValue is Stride.Assets.Scripts.Symbol symbol && !string.IsNullOrEmpty(symbol.Name))
                return symbol.Name;

            return node.Name;
        }
        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value;
}

public class NodeToSymbolTypeConverter : IValueConverter
{
    public static readonly NodeToSymbolTypeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Stride.Core.Presentation.Quantum.ViewModels.NodeViewModel node)
        {
            var typeNode = node["Type_"] as Stride.Core.Presentation.Quantum.ViewModels.NodeViewModel;
            if (typeNode?.NodeValue != null)
                return typeNode.NodeValue.ToString();

            if (node.NodeValue is Stride.Assets.Scripts.Symbol symbol && !string.IsNullOrEmpty(symbol.Type))
                return symbol.Type;

            return node.Type.Name;
        }
        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value;
}
