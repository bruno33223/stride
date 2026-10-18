// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.UI;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// Represents a font family name.
/// </summary>
public class FontFamily
{
    public FontFamily(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public override string ToString() => Name;
}

/// <summary>
/// Specifies the weight of a font.
/// </summary>
public enum FontWeight
{
    Normal,
    Bold,
    SemiBold,
    Light,
    Medium,
    Black,
    ExtraBold,
    ExtraLight,
    Thin
}

/// <summary>
/// Provides predefined font weights.
/// </summary>
public static class FontWeights
{
    public static FontWeight Normal => FontWeight.Normal;
    public static FontWeight Bold => FontWeight.Bold;
}

/// <summary>
/// Interface for drawing primitives in the curve editor.
/// </summary>
public interface IDrawingContext
{
    /// <summary>
    /// Draws a straight line between two points.
    /// </summary>
    void DrawLine(Vector2 p1, Vector2 p2, Color strokeColor);

    /// <summary>
    /// Draws disjoint line segments between pairs of points.
    /// </summary>
    void DrawLineSegments(IList<Vector2> points, Color strokeColor, double thickness = 1.0);

    /// <summary>
    /// Draws a rectangle with fill and stroke colors.
    /// </summary>
    void DrawRectangle(RectangleF rect, Color fillColor, Color strokeColor);

    /// <summary>
    /// Draws a batch of text strings at specified screen positions.
    /// </summary>
    void DrawTexts(
        IList<Vector2> points,
        Color color,
        IList<string> texts,
        FontFamily fontFamily,
        double fontSize,
        FontWeight fontWeight,
        HorizontalAlignment hAlign = HorizontalAlignment.Left,
        VerticalAlignment vAlign = VerticalAlignment.Top);

    /// <summary>
    /// Measures the bounding size of a collection of texts.
    /// </summary>
    Size2F MeasureTexts(IList<string> texts, FontFamily fontFamily, double fontSize, FontWeight fontWeight);
}

public interface IDrawingModel
{
    void Attach(IDrawingView view);
    void Detach(IDrawingView view);
    void Render(IDrawingContext context, double width, double height);
    void Update(bool updateData);
}

public interface IDrawingView
{
    IDrawingModel? Model { get; }
    void InvalidateDrawing();
}
