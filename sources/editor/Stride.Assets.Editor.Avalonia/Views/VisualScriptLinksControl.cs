// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Scripts;
using AvaloniaPoint = Avalonia.Point;
using AvaloniaColor = Avalonia.Media.Color;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// Controls rendering of 2D cubic Bezier splines connecting block pins in the Visual Script Editor.
/// </summary>
public class VisualScriptLinksControl : Control
{
    public static readonly StyledProperty<VisualScriptMethodEditorViewModel?> MethodProperty =
        AvaloniaProperty.Register<VisualScriptLinksControl, VisualScriptMethodEditorViewModel?>(nameof(Method));

    private const double BlockWidth = 220.0;
    private const double HeaderHeight = 38.0;
    private const double SlotItemHeight = 24.0;
    private const double SlotHalfHeight = 12.0;

    public VisualScriptLinksControl()
    {
        ClipToBounds = false;
        IsHitTestVisible = true;
    }

    public VisualScriptMethodEditorViewModel? Method
    {
        get => GetValue(MethodProperty);
        set => SetValue(MethodProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == MethodProperty)
        {
            if (change.OldValue is VisualScriptMethodEditorViewModel oldMethod)
            {
                UnsubscribeMethod(oldMethod);
            }

            if (change.NewValue is VisualScriptMethodEditorViewModel newMethod)
            {
                SubscribeMethod(newMethod);
            }

            InvalidateVisual();
        }
    }

    private void SubscribeMethod(VisualScriptMethodEditorViewModel method)
    {
        method.Links.CollectionChanged += OnLinksCollectionChanged;
        method.Blocks.CollectionChanged += OnBlocksCollectionChanged;
        method.SelectedLinks.CollectionChanged += OnSelectedLinksChanged;

        foreach (var block in method.Blocks)
        {
            block.PropertyChanged += OnBlockPropertyChanged;
        }

        foreach (var link in method.Links)
        {
            link.PropertyChanged += OnLinkPropertyChanged;
        }
    }

    private void UnsubscribeMethod(VisualScriptMethodEditorViewModel method)
    {
        method.Links.CollectionChanged -= OnLinksCollectionChanged;
        method.Blocks.CollectionChanged -= OnBlocksCollectionChanged;
        method.SelectedLinks.CollectionChanged -= OnSelectedLinksChanged;

        foreach (var block in method.Blocks)
        {
            block.PropertyChanged -= OnBlockPropertyChanged;
        }

        foreach (var link in method.Links)
        {
            link.PropertyChanged -= OnLinkPropertyChanged;
        }
    }

    private void OnLinksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (LinkNodeEdge link in e.OldItems)
                link.PropertyChanged -= OnLinkPropertyChanged;
        }
        if (e.NewItems != null)
        {
            foreach (LinkNodeEdge link in e.NewItems)
                link.PropertyChanged += OnLinkPropertyChanged;
        }
        InvalidateVisual();
    }

    private void OnBlocksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (BlockNodeVertex block in e.OldItems)
                block.PropertyChanged -= OnBlockPropertyChanged;
        }
        if (e.NewItems != null)
        {
            foreach (BlockNodeVertex block in e.NewItems)
                block.PropertyChanged += OnBlockPropertyChanged;
        }
        InvalidateVisual();
    }

    private void OnSelectedLinksChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void OnBlockPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BlockNodeVertex.PositionX) ||
            e.PropertyName == nameof(BlockNodeVertex.PositionY) ||
            e.PropertyName == nameof(BlockNodeVertex.Position))
        {
            InvalidateVisual();
        }
    }

    private void OnLinkPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LinkNodeEdge.IsSelected))
        {
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var currentMethod = Method;
        if (currentMethod == null || currentMethod.Links.Count == 0)
            return;

        foreach (var link in currentMethod.Links)
        {
            var source = link.Source;
            var target = link.Target;
            if (source == null || target == null)
                continue;

            int sourceSlotIndex = source.OutputSlots.IndexOf(link.SourceSlot);
            if (sourceSlotIndex < 0) sourceSlotIndex = 0;

            int targetSlotIndex = target.InputSlots.IndexOf(link.TargetSlot);
            if (targetSlotIndex < 0) targetSlotIndex = 0;

            // Coordenadas centrais dos pinos
            double srcX = source.PositionX + BlockWidth - 6;
            double srcY = source.PositionY + HeaderHeight + (sourceSlotIndex * SlotItemHeight) + SlotHalfHeight;

            double tgtX = target.PositionX + 6;
            double tgtY = target.PositionY + HeaderHeight + (targetSlotIndex * SlotItemHeight) + SlotHalfHeight;

            var p0 = new AvaloniaPoint(srcX, srcY);
            var p3 = new AvaloniaPoint(tgtX, tgtY);

            // Cálculo dos pontos de controle da curva de Bezier Cúbica
            double dx = Math.Abs(tgtX - srcX);
            double offset = Math.Max(dx * 0.5, 45.0);
            if (tgtX < srcX)
            {
                offset = Math.Max(offset, 90.0);
            }

            var p1 = new AvaloniaPoint(srcX + offset, srcY);
            var p2 = new AvaloniaPoint(tgtX - offset, tgtY);

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(p0, false);
                ctx.CubicBezierTo(p1, p2, p3);
                ctx.EndFigure(false);
            }

            // Definição da cor do link
            AvaloniaColor baseColor;
            if (link.ViewModel.Diagnostics.Count > 0)
            {
                baseColor = AvaloniaColor.FromRgb(255, 107, 107); // Vermelho de erro
            }
            else if (link.Kind == SlotKind.Execution)
            {
                baseColor = AvaloniaColor.FromRgb(245, 158, 11); // Laranja de execução
            }
            else
            {
                baseColor = AvaloniaColor.FromRgb(56, 189, 248); // Azul celeste de dados/valores
            }

            if (link.IsSelected)
            {
                // Brilho exterior para link selecionado
                var glowPen = new ImmutablePen(new ImmutableSolidColorBrush(AvaloniaColor.FromArgb(120, 168, 85, 247)), 6.0, lineCap: PenLineCap.Round);
                context.DrawGeometry(null, glowPen, geometry);

                // Linha principal de destaque
                var selPen = new ImmutablePen(new ImmutableSolidColorBrush(AvaloniaColor.FromRgb(255, 255, 255)), 3.0, lineCap: PenLineCap.Round);
                context.DrawGeometry(null, selPen, geometry);
            }
            else
            {
                // Sombra suave sob a spline
                var shadowPen = new ImmutablePen(new ImmutableSolidColorBrush(AvaloniaColor.FromArgb(80, 0, 0, 0)), 3.5, lineCap: PenLineCap.Round);
                context.DrawGeometry(null, shadowPen, geometry);

                // Spline Bezier
                var linkPen = new ImmutablePen(new ImmutableSolidColorBrush(baseColor), 2.0, lineCap: PenLineCap.Round);
                context.DrawGeometry(null, linkPen, geometry);
            }

            // Pinos circulares nas extremidades da conexão
            var pinBrush = new ImmutableSolidColorBrush(baseColor);
            context.DrawEllipse(pinBrush, null, p0, 3.5, 3.5);
            context.DrawEllipse(pinBrush, null, p3, 3.5, 3.5);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var currentMethod = Method;
        if (currentMethod == null || currentMethod.Links.Count == 0)
            return;

        var clickPoint = e.GetPosition(this);
        LinkNodeEdge? clickedLink = null;
        double minFoundDistance = double.MaxValue;

        foreach (var link in currentMethod.Links)
        {
            var source = link.Source;
            var target = link.Target;
            if (source == null || target == null)
                continue;

            int sourceSlotIndex = source.OutputSlots.IndexOf(link.SourceSlot);
            if (sourceSlotIndex < 0) sourceSlotIndex = 0;

            int targetSlotIndex = target.InputSlots.IndexOf(link.TargetSlot);
            if (targetSlotIndex < 0) targetSlotIndex = 0;

            double srcX = source.PositionX + BlockWidth - 6;
            double srcY = source.PositionY + HeaderHeight + (sourceSlotIndex * SlotItemHeight) + SlotHalfHeight;

            double tgtX = target.PositionX + 6;
            double tgtY = target.PositionY + HeaderHeight + (targetSlotIndex * SlotItemHeight) + SlotHalfHeight;

            var p0 = new AvaloniaPoint(srcX, srcY);
            var p3 = new AvaloniaPoint(tgtX, tgtY);

            double dx = Math.Abs(tgtX - srcX);
            double offset = Math.Max(dx * 0.5, 45.0);
            if (tgtX < srcX) offset = Math.Max(offset, 90.0);

            var p1 = new AvaloniaPoint(srcX + offset, srcY);
            var p2 = new AvaloniaPoint(tgtX - offset, tgtY);

            double dist = DistanceToCubicBezier(clickPoint, p0, p1, p2, p3);
            if (dist < 10.0 && dist < minFoundDistance)
            {
                minFoundDistance = dist;
                clickedLink = link;
            }
        }

        if (clickedLink != null)
        {
            bool isCtrlOrShift = (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Shift)) != 0;
            if (!isCtrlOrShift)
            {
                foreach (var l in currentMethod.Links)
                {
                    if (l != clickedLink)
                        l.IsSelected = false;
                }
                foreach (var b in currentMethod.Blocks)
                {
                    b.IsSelected = false;
                }
            }

            clickedLink.IsSelected = !clickedLink.IsSelected;
            e.Handled = true;
            InvalidateVisual();
        }
    }

    private static double DistanceToCubicBezier(AvaloniaPoint pt, AvaloniaPoint p0, AvaloniaPoint p1, AvaloniaPoint p2, AvaloniaPoint p3)
    {
        double minDist = double.MaxValue;
        const int steps = 30;
        for (int i = 0; i <= steps; i++)
        {
            double t = (double)i / steps;
            double u = 1.0 - t;
            double tt = t * t;
            double uu = u * u;
            double uuu = uu * u;
            double ttt = tt * t;

            double x = uuu * p0.X + 3 * uu * t * p1.X + 3 * u * tt * p2.X + ttt * p3.X;
            double y = uuu * p0.Y + 3 * uu * t * p1.Y + 3 * u * tt * p2.Y + ttt * p3.Y;

            double dx = pt.X - x;
            double dy = pt.Y - y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < minDist)
                minDist = dist;
        }
        return minDist;
    }
}
