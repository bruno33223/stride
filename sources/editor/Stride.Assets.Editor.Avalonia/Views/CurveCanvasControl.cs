// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Stride.Animations;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Services;
using Stride.Core.Transactions;
using Avalonia.Threading;
using AvaloniaPoint = Avalonia.Point;
using AvaloniaRect = Avalonia.Rect;
using AvaloniaColor = Avalonia.Media.Color;

namespace Stride.Assets.Editor.Avalonia.Views;

public class CurveCanvasControl : Control, IDrawingView
{
    public static readonly StyledProperty<CurveEditorViewModel?> ViewModelProperty =
        AvaloniaProperty.Register<CurveCanvasControl, CurveEditorViewModel?>(nameof(ViewModel));

    public static readonly StyledProperty<bool> IsTrackerActiveProperty =
        AvaloniaProperty.Register<CurveCanvasControl, bool>(nameof(IsTrackerActive), defaultValue: true);

    private bool isPanning;
    private AvaloniaPoint lastPanPoint;
    private bool isSpacePressed;

    private bool isDraggingPoint;
    private AvaloniaPoint dragStartPoint;
    private readonly Dictionary<ControlPointViewModelBase, Vector2> initialDragPoints = new();
    private ITransaction? dragTransaction;

    private bool isSelecting;
    private AvaloniaPoint selectionStart;
    private AvaloniaRect? selectionRect;

    private AvaloniaPoint? currentMousePos;
    private ControlPointViewModelBase? hoveredPoint;
    private AvaloniaPoint lastRightClickPoint;

    public CurveCanvasControl()
    {
        ClipToBounds = true;
        Focusable = true;
        try
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                ContextMenu = CreateContextMenu();
            }
        }
        catch (InvalidOperationException)
        {
            // Headless unit test environment
        }
        LostFocus += (_, _) => isSpacePressed = false;
    }

    private ContextMenu CreateContextMenu()
    {
        var menu = new ContextMenu();

        var addKeyframeItem = new MenuItem
        {
            Header = "Add Keyframe at Cursor"
        };
        addKeyframeItem.Click += (_, _) =>
        {
            if (ViewModel != null)
            {
                var pt = new Vector2((float)lastRightClickPoint.X, (float)lastRightClickPoint.Y);
                ViewModel.AddPoint(pt);
            }
        };
        menu.Items.Add(addKeyframeItem);

        menu.Items.Add(new Separator());

        var linearItem = new MenuItem
        {
            Header = "Tangent: Linear"
        };
        linearItem.Click += (_, _) => ViewModel?.SetSelectedPointsTangentType(AnimationKeyTangentType.Linear);
        menu.Items.Add(linearItem);

        var flatItem = new MenuItem
        {
            Header = "Tangent: Flat"
        };
        flatItem.Click += (_, _) => ViewModel?.SetSelectedPointsTangentType(AnimationKeyTangentType.Flat);
        menu.Items.Add(flatItem);

        var smoothItem = new MenuItem
        {
            Header = "Tangent: Smooth"
        };
        smoothItem.Click += (_, _) => ViewModel?.SetSelectedPointsTangentType(AnimationKeyTangentType.Smooth);
        menu.Items.Add(smoothItem);

        menu.Items.Add(new Separator());

        var deleteItem = new MenuItem
        {
            Header = "Delete Keyframe(s)"
        };
        deleteItem.Click += (_, _) => ViewModel?.DeleteSelectedPointsCommand.Execute(null);
        menu.Items.Add(deleteItem);

        menu.Opening += (_, _) =>
        {
            bool hasCurve = ViewModel?.SelectedCurve != null;
            bool hasSelection = ViewModel?.SelectedControlPoints.Count > 0;
            addKeyframeItem.IsEnabled = hasCurve;
            linearItem.IsEnabled = hasSelection;
            flatItem.IsEnabled = hasSelection;
            smoothItem.IsEnabled = hasSelection;
            deleteItem.IsEnabled = hasSelection;
        };

        return menu;
    }

    public CurveEditorViewModel? ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public bool IsTrackerActive
    {
        get => GetValue(IsTrackerActiveProperty);
        set => SetValue(IsTrackerActiveProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ViewModelProperty)
        {
            if (change.OldValue is CurveEditorViewModel oldVm)
            {
                oldVm.ViewInvalidated -= OnViewInvalidated;
            }
            if (change.NewValue is CurveEditorViewModel newVm)
            {
                newVm.ViewInvalidated += OnViewInvalidated;
            }
            InvalidateVisual();
        }
        else if (change.Property == IsTrackerActiveProperty)
        {
            InvalidateVisual();
        }
    }

    private void OnViewInvalidated()
    {
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        double width = bounds.Width;
        double height = bounds.Height;

        if (width <= 0 || height <= 0)
            return;

        // Fundo do gráfico
        var bgBrush = new ImmutableSolidColorBrush(AvaloniaColor.FromRgb(30, 30, 34));
        var borderPen = new ImmutablePen(new ImmutableSolidColorBrush(AvaloniaColor.FromRgb(45, 45, 50)), 1);
        context.DrawRectangle(bgBrush, borderPen, new AvaloniaRect(0, 0, width, height));

        var vm = ViewModel;
        if (vm == null || vm.SelectedCurve == null)
        {
            var emptyText = new FormattedText(
                "Nenhuma curva selecionada",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                14,
                new ImmutableSolidColorBrush(AvaloniaColor.FromRgb(120, 120, 130)));
            context.DrawText(emptyText, new AvaloniaPoint(Math.Max(10, (width - emptyText.Width) / 2), Math.Max(10, (height - emptyText.Height) / 2)));
            return;
        }

        var selectedCurve = vm.SelectedCurve;
        var xAxis = selectedCurve.XAxis;
        var yAxis = selectedCurve.YAxis;

        if (xAxis == null || yAxis == null)
            return;

        float fWidth = (float)width;
        float fHeight = (float)height;
        var curveArea = new RectangleF(0, 0, fWidth, fHeight);

        // Atualizar transformações e intervalos baseados nas dimensões da tela
        xAxis.UpdateTransform(curveArea);
        yAxis.UpdateTransform(curveArea);
        xAxis.UpdateIntervals(curveArea);
        yAxis.UpdateIntervals(curveArea);
        vm.UpdateBounds(curveArea);

        // Renderizar Grade e Ticks dos Eixos
        RenderGridAndAxes(context, xAxis, yAxis, fWidth, fHeight);

        // Renderizar Séries de Curvas (sem dupla transformação)
        RenderCurves(context, vm);

        // Renderizar Pontos de Controle e Alças de Tangentes (sem dupla transformação)
        RenderControlPoints(context, vm);

        // Renderizar Marquee de Seleção
        if (isSelecting && selectionRect.HasValue)
        {
            var selectFill = new ImmutableSolidColorBrush(AvaloniaColor.FromArgb(45, 0, 122, 204));
            var selectPen = new ImmutablePen(new ImmutableSolidColorBrush(AvaloniaColor.FromRgb(0, 122, 204)), 1);
            context.DrawRectangle(selectFill, selectPen, selectionRect.Value);
        }

        // Renderizar Crosshair Tracker
        if (IsTrackerActive && currentMousePos.HasValue)
        {
            RenderTracker(context, xAxis, yAxis, currentMousePos.Value, fWidth, fHeight);
        }
    }

    private void RenderGridAndAxes(DrawingContext context, AxisBase xAxis, AxisBase yAxis, float width, float height)
    {
        var gridPen = new ImmutablePen(new ImmutableSolidColorBrush(AvaloniaColor.FromRgb(42, 42, 48)), 1);
        var zeroPen = new ImmutablePen(new ImmutableSolidColorBrush(AvaloniaColor.FromRgb(85, 85, 95)), 1.5);
        var textBrush = new ImmutableSolidColorBrush(AvaloniaColor.FromRgb(115, 115, 125));

        // Linhas de grade verticais (Eixo X - Tempo/Chave)
        float xStep = xAxis.ActualMajorStep > 0 ? (float)xAxis.ActualMajorStep : 1.0f;
        float xStart = (float)Math.Floor(xAxis.ActualMinimum / xStep) * xStep;
        for (float x = xStart; x <= xAxis.ActualMaximum + xStep * 0.5f; x += xStep)
        {
            float screenX = (float)xAxis.Transform(x);
            if (screenX < 0 || screenX > width)
                continue;

            var pen = Math.Abs(x) < xStep * 0.001f ? zeroPen : gridPen;
            context.DrawLine(pen, new AvaloniaPoint(screenX, 0), new AvaloniaPoint(screenX, height));

            var label = new FormattedText(
                x.ToString("0.##", CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                10,
                textBrush);
            context.DrawText(label, new AvaloniaPoint(screenX + 2, height - 14));
        }

        // Linhas de grade horizontais (Eixo Y - Valor)
        float yStep = yAxis.ActualMajorStep > 0 ? (float)yAxis.ActualMajorStep : 1.0f;
        float yStart = (float)Math.Floor(yAxis.ActualMinimum / yStep) * yStep;
        for (float y = yStart; y <= yAxis.ActualMaximum + yStep * 0.5f; y += yStep)
        {
            float screenY = (float)yAxis.Transform(y);
            if (screenY < 0 || screenY > height)
                continue;

            var pen = Math.Abs(y) < yStep * 0.001f ? zeroPen : gridPen;
            context.DrawLine(pen, new AvaloniaPoint(0, screenY), new AvaloniaPoint(width, screenY));

            var label = new FormattedText(
                y.ToString("0.##", CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                10,
                textBrush);
            context.DrawText(label, new AvaloniaPoint(4, screenY - 12));
        }
    }

    private void RenderCurves(DrawingContext context, CurveEditorViewModel vm)
    {
        if (vm.SelectedCurve == null)
            return;

        var seriesList = vm.SelectedCurve.GetRenderSeries();
        foreach (var series in seriesList)
        {
            if (series.Points.Count < 2)
                continue;

            var seriesColor = series.Color;
            var avaloniaColor = AvaloniaColor.FromArgb(seriesColor.A, seriesColor.R, seriesColor.G, seriesColor.B);
            var pen = new ImmutablePen(new ImmutableSolidColorBrush(avaloniaColor), 2.0);

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                bool isFirst = true;
                foreach (var pt in series.Points)
                {
                    // pt já vem transformado para pixels de tela por CurveViewModelBase.TransformPoint
                    var screenPt = new AvaloniaPoint(pt.X, pt.Y);

                    if (isFirst)
                    {
                        ctx.BeginFigure(screenPt, false);
                        isFirst = false;
                    }
                    else
                    {
                        ctx.LineTo(screenPt);
                    }
                }
                ctx.EndFigure(false);
            }

            context.DrawGeometry(null, pen, geometry);
        }
    }

    private void RenderControlPoints(DrawingContext context, CurveEditorViewModel vm)
    {
        if (vm.SelectedCurve == null)
            return;

        var editableCurves = new List<IEditableCurveViewModel>();

        if (vm.SelectedCurve is IEditableCurveViewModel singleEditable)
        {
            editableCurves.Add(singleEditable);
        }
        if (vm.SelectedCurve.Children.Count > 0)
        {
            editableCurves.AddRange(vm.SelectedCurve.Children.OfType<IEditableCurveViewModel>());
        }

        var selectedFill = new ImmutableSolidColorBrush(AvaloniaColor.FromRgb(255, 215, 0)); // Dourado
        var selectedBorderPen = new ImmutablePen(new ImmutableSolidColorBrush(AvaloniaColor.FromRgb(255, 255, 255)), 1.5);
        var hoverFill = new ImmutableSolidColorBrush(AvaloniaColor.FromRgb(255, 255, 255));
        var defaultBorderPen = new ImmutablePen(new ImmutableSolidColorBrush(AvaloniaColor.FromRgb(20, 20, 24)), 1);

        // 1º Passo: Renderizar alças de tangentes nos pontos selecionados
        foreach (var curve in editableCurves)
        {
            foreach (var cp in curve.ControlPoints)
            {
                if (cp.IsSelected)
                {
                    RenderTangentHandles(context, cp, cp.ActualPoint.X, cp.ActualPoint.Y);
                }
            }
        }

        // 2º Passo: Renderizar os keyframes (losango / diamond)
        foreach (var curve in editableCurves)
        {
            var curveColor = curve is CurveViewModelBase cvb ? cvb.Color : Stride.Core.Mathematics.Color.CornflowerBlue;
            var regularFill = new ImmutableSolidColorBrush(AvaloniaColor.FromArgb(curveColor.A, curveColor.R, curveColor.G, curveColor.B));

            foreach (var cp in curve.ControlPoints)
            {
                // cp.ActualPoint já vem transformado para coordenadas de tela (pixels)
                float sx = cp.ActualPoint.X;
                float sy = cp.ActualPoint.Y;

                const double r = 5.0;
                var diamondGeom = new StreamGeometry();
                using (var ctx = diamondGeom.Open())
                {
                    ctx.BeginFigure(new AvaloniaPoint(sx, sy - r), true);
                    ctx.LineTo(new AvaloniaPoint(sx + r, sy));
                    ctx.LineTo(new AvaloniaPoint(sx, sy + r));
                    ctx.LineTo(new AvaloniaPoint(sx - r, sy));
                    ctx.EndFigure(true);
                }

                IBrush fill = cp.IsSelected ? selectedFill : (cp == hoveredPoint ? hoverFill : regularFill);
                IPen stroke = cp.IsSelected ? selectedBorderPen : defaultBorderPen;

                context.DrawGeometry(fill, stroke, diamondGeom);
            }
        }
    }

    private void RenderTangentHandles(DrawingContext context, ControlPointViewModelBase cp, float sx, float sy)
    {
        var tangentType = cp.TangentType ?? AnimationKeyTangentType.Linear;
        const double handleLength = 28.0;

        AvaloniaPoint leftEnd;
        AvaloniaPoint rightEnd;

        switch (tangentType)
        {
            case AnimationKeyTangentType.Flat:
            {
                leftEnd = new AvaloniaPoint(sx - handleLength, sy);
                rightEnd = new AvaloniaPoint(sx + handleLength, sy);
                break;
            }

            case AnimationKeyTangentType.Smooth:
            {
                double dx = 1.0;
                double dy = 0.0;

                if (cp.Previous != null && cp.Next != null)
                {
                    dx = cp.Next.ActualPoint.X - cp.Previous.ActualPoint.X;
                    dy = cp.Next.ActualPoint.Y - cp.Previous.ActualPoint.Y;
                }
                else if (cp.Next != null)
                {
                    dx = cp.Next.ActualPoint.X - sx;
                    dy = cp.Next.ActualPoint.Y - sy;
                }
                else if (cp.Previous != null)
                {
                    dx = sx - cp.Previous.ActualPoint.X;
                    dy = sy - cp.Previous.ActualPoint.Y;
                }

                double len = Math.Sqrt(dx * dx + dy * dy);
                if (len > 0.0001)
                {
                    dx /= len;
                    dy /= len;
                }
                else
                {
                    dx = 1.0;
                    dy = 0.0;
                }

                leftEnd = new AvaloniaPoint(sx - dx * handleLength, sy - dy * handleLength);
                rightEnd = new AvaloniaPoint(sx + dx * handleLength, sy + dy * handleLength);
                break;
            }

            case AnimationKeyTangentType.Linear:
            default:
            {
                if (cp.Previous != null)
                {
                    double dx = cp.Previous.ActualPoint.X - sx;
                    double dy = cp.Previous.ActualPoint.Y - sy;
                    double len = Math.Sqrt(dx * dx + dy * dy);
                    if (len > 0.0001)
                        leftEnd = new AvaloniaPoint(sx + (dx / len) * handleLength, sy + (dy / len) * handleLength);
                    else
                        leftEnd = new AvaloniaPoint(sx - handleLength, sy);
                }
                else
                {
                    leftEnd = new AvaloniaPoint(sx - handleLength, sy);
                }

                if (cp.Next != null)
                {
                    double dx = cp.Next.ActualPoint.X - sx;
                    double dy = cp.Next.ActualPoint.Y - sy;
                    double len = Math.Sqrt(dx * dx + dy * dy);
                    if (len > 0.0001)
                        rightEnd = new AvaloniaPoint(sx + (dx / len) * handleLength, sy + (dy / len) * handleLength);
                    else
                        rightEnd = new AvaloniaPoint(sx + handleLength, sy);
                }
                else
                {
                    rightEnd = new AvaloniaPoint(sx + handleLength, sy);
                }
                break;
            }
        }

        var stemPen = new ImmutablePen(new ImmutableSolidColorBrush(AvaloniaColor.FromArgb(200, 255, 215, 0)), 1.2);
        var handleBrush = new ImmutableSolidColorBrush(AvaloniaColor.FromRgb(255, 255, 255));
        var handlePen = new ImmutablePen(new ImmutableSolidColorBrush(AvaloniaColor.FromRgb(255, 215, 0)), 1.0);

        var center = new AvaloniaPoint(sx, sy);
        context.DrawLine(stemPen, center, leftEnd);
        context.DrawLine(stemPen, center, rightEnd);

        // Pontos terminais das alças
        const double handleRadius = 2.5;
        context.DrawEllipse(handleBrush, handlePen, leftEnd, handleRadius, handleRadius);
        context.DrawEllipse(handleBrush, handlePen, rightEnd, handleRadius, handleRadius);
    }

    private void RenderTracker(DrawingContext context, AxisBase xAxis, AxisBase yAxis, AvaloniaPoint mousePos, float width, float height)
    {
        var trackerPen = new Pen(new SolidColorBrush(AvaloniaColor.FromArgb(120, 200, 200, 220)), 1, DashStyle.Dash);
        context.DrawLine(trackerPen, new AvaloniaPoint(mousePos.X, 0), new AvaloniaPoint(mousePos.X, height));
        context.DrawLine(trackerPen, new AvaloniaPoint(0, mousePos.Y), new AvaloniaPoint(width, mousePos.Y));

        float key = (float)xAxis.InverseTransform(mousePos.X);
        float val = (float)yAxis.InverseTransform(mousePos.Y);

        string tooltipText = $"Chave: {key:0.000} | Valor: {val:0.000}";
        var tooltipFormatted = new FormattedText(
            tooltipText,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            11,
            new ImmutableSolidColorBrush(AvaloniaColor.FromRgb(240, 240, 245)));

        double tipX = Math.Min(mousePos.X + 12, width - tooltipFormatted.Width - 12);
        double tipY = Math.Max(mousePos.Y - 24, 6);

        var tipRect = new AvaloniaRect(tipX - 4, tipY - 2, tooltipFormatted.Width + 8, tooltipFormatted.Height + 4);
        var tipBg = new ImmutableSolidColorBrush(AvaloniaColor.FromArgb(220, 25, 25, 30));
        var tipBorder = new ImmutablePen(new ImmutableSolidColorBrush(AvaloniaColor.FromRgb(70, 70, 80)), 1);
        context.DrawRectangle(tipBg, tipBorder, tipRect);
        context.DrawText(tooltipFormatted, new AvaloniaPoint(tipX, tipY));
    }

    #region Interações de Teclado

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Space)
        {
            isSpacePressed = true;
            if (!isPanning && !isDraggingPoint && !isSelecting)
            {
                Cursor = new Cursor(StandardCursorType.Hand);
            }
        }
        else if (e.Key == Key.Delete || e.Key == Key.Back)
        {
            ViewModel?.DeleteSelectedPointsCommand.Execute(null);
            e.Handled = true;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        if (e.Key == Key.Space)
        {
            isSpacePressed = false;
            if (!isPanning && !isDraggingPoint && !isSelecting)
            {
                Cursor = hoveredPoint != null ? new Cursor(StandardCursorType.Hand) : new Cursor(StandardCursorType.Arrow);
            }
        }
    }

    #endregion

    #region Interações de Ponteiro (Mouse & Pan & Zoom & Drag)

    protected override void OnDoubleTapped(TappedEventArgs e)
    {
        base.OnDoubleTapped(e);

        var vm = ViewModel;
        if (vm?.SelectedCurve == null)
            return;

        var pos = e.GetPosition(this);
        var cp = FindNearestControlPoint(pos, 8.0f);
        if (cp == null)
        {
            vm.AddPoint(new Vector2((float)pos.X, (float)pos.Y));
            e.Handled = true;
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        Focus();

        var vm = ViewModel;
        if (vm?.SelectedCurve == null)
            return;

        var xAxis = vm.SelectedCurve.XAxis;
        var yAxis = vm.SelectedCurve.YAxis;
        if (xAxis == null || yAxis == null)
            return;

        var pos = e.GetPosition(this);
        var pointerProps = e.GetCurrentPoint(this).Properties;

        // Pan: Botão do Meio ou Espaço / Alt + Botão Esquerdo
        bool isMiddlePan = pointerProps.IsMiddleButtonPressed || pointerProps.PointerUpdateKind == PointerUpdateKind.MiddleButtonPressed;
        bool isSpacePan = pointerProps.IsLeftButtonPressed && (isSpacePressed || e.KeyModifiers.HasFlag(KeyModifiers.Alt));

        if (isMiddlePan || isSpacePan)
        {
            isPanning = true;
            lastPanPoint = pos;
            e.Pointer.Capture(this);
            e.Handled = true;
            Cursor = new Cursor(StandardCursorType.SizeAll);
            return;
        }

        // Clique com o Botão Direito: Salva posição para ContextMenu e seleciona keyframe se sob o cursor
        if (pointerProps.IsRightButtonPressed)
        {
            if (ContextMenu == null && Dispatcher.UIThread.CheckAccess())
            {
                ContextMenu = CreateContextMenu();
            }
            lastRightClickPoint = pos;
            var cp = FindNearestControlPoint(pos, 10.0f);
            if (cp != null && !cp.IsSelected)
            {
                if (!e.KeyModifiers.HasFlag(KeyModifiers.Control) && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    vm.ClearPointSelection();
                }
                cp.IsSelected = true;
                if (!vm.SelectedControlPoints.Contains(cp))
                    vm.SelectedControlPoints.Add(cp);
                InvalidateVisual();
            }
            return;
        }

        // Duplo-clique para adicionar keyframe
        if (pointerProps.IsLeftButtonPressed && e.ClickCount == 2)
        {
            var cp = FindNearestControlPoint(pos, 8.0f);
            if (cp == null)
            {
                vm.AddPoint(new Vector2((float)pos.X, (float)pos.Y));
                e.Handled = true;
                return;
            }
        }

        // Botão Esquerdo: Seleção ou Movimentação de Keyframe
        if (pointerProps.IsLeftButtonPressed)
        {
            var cp = FindNearestControlPoint(pos, 10.0f);
            if (cp != null)
            {
                bool isMulti = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Shift);
                if (isMulti)
                {
                    cp.IsSelected = !cp.IsSelected;
                    if (cp.IsSelected)
                    {
                        if (!vm.SelectedControlPoints.Contains(cp))
                            vm.SelectedControlPoints.Add(cp);
                    }
                    else
                    {
                        vm.SelectedControlPoints.Remove(cp);
                    }
                }
                else if (!cp.IsSelected)
                {
                    vm.ClearPointSelection();
                    cp.IsSelected = true;
                    if (!vm.SelectedControlPoints.Contains(cp))
                        vm.SelectedControlPoints.Add(cp);
                }

                if (cp.IsSelected)
                {
                    // Iniciar arrasto de ponto(s)
                    isDraggingPoint = true;
                    dragStartPoint = pos;
                    initialDragPoints.Clear();
                    dragTransaction = vm.UndoRedoService?.CreateTransaction();
                    foreach (var selected in vm.SelectedControlPoints.OfType<ControlPointViewModelBase>())
                    {
                        initialDragPoints[selected] = new Vector2((float)selected.ActualKey, (float)selected.ActualValue);
                    }

                    e.Pointer.Capture(this);
                    e.Handled = true;
                    InvalidateVisual();
                    return;
                }
            }

            // Clicou no vazio: desselecionar se não for multi e iniciar seleção retangular
            if (!e.KeyModifiers.HasFlag(KeyModifiers.Control) && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                vm.ClearPointSelection();
            }

            isSelecting = true;
            selectionStart = pos;
            selectionRect = new AvaloniaRect(pos, pos);
            e.Pointer.Capture(this);
            e.Handled = true;
            InvalidateVisual();
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var vm = ViewModel;
        if (vm?.SelectedCurve == null)
            return;

        var xAxis = vm.SelectedCurve.XAxis;
        var yAxis = vm.SelectedCurve.YAxis;
        if (xAxis == null || yAxis == null)
            return;

        var pos = e.GetPosition(this);
        currentMousePos = pos;

        if (isPanning)
        {
            var prevVec = new Vector2((float)lastPanPoint.X, (float)lastPanPoint.Y);
            var curVec = new Vector2((float)pos.X, (float)pos.Y);
            xAxis.Pan(prevVec, curVec);
            yAxis.Pan(prevVec, curVec);
            lastPanPoint = pos;
            InvalidateVisual();
            return;
        }

        if (isDraggingPoint)
        {
            double deltaKey = xAxis.InverseTransform(pos.X) - xAxis.InverseTransform(dragStartPoint.X);
            double deltaVal = yAxis.InverseTransform(pos.Y) - yAxis.InverseTransform(dragStartPoint.Y);

            foreach (var kvp in initialDragPoints)
            {
                var point = kvp.Key;
                var initialPos = kvp.Value;
                point.ActualKey = initialPos.X + deltaKey;
                point.ActualValue = initialPos.Y + deltaVal;
            }

            InvalidateVisual();
            return;
        }

        if (isSelecting)
        {
            double minX = Math.Min(selectionStart.X, pos.X);
            double minY = Math.Min(selectionStart.Y, pos.Y);
            double selW = Math.Abs(pos.X - selectionStart.X);
            double selH = Math.Abs(pos.Y - selectionStart.Y);
            selectionRect = new AvaloniaRect(minX, minY, selW, selH);

            // Seleção Marquee: passar retângulo diretamente em coordenadas de tela (pixels)
            var area = new RectangleF((float)minX, (float)minY, (float)selW, (float)selH);

            SelectionModifiers modifiers = SelectionModifiers.None;
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift)) modifiers |= SelectionModifiers.Shift;
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control)) modifiers |= SelectionModifiers.Control;
            if (e.KeyModifiers.HasFlag(KeyModifiers.Alt)) modifiers |= SelectionModifiers.Alt;
            vm.ActiveModifiers = modifiers;

            vm.Select(area);

            InvalidateVisual();
            return;
        }

        // Testar hover de ponto de controle
        var oldHovered = hoveredPoint;
        hoveredPoint = FindNearestControlPoint(pos, 10.0f);
        if (hoveredPoint != oldHovered || IsTrackerActive)
        {
            if (!isPanning && !isDraggingPoint && !isSelecting)
            {
                Cursor = isSpacePressed ? new Cursor(StandardCursorType.Hand) : (hoveredPoint != null ? new Cursor(StandardCursorType.Hand) : new Cursor(StandardCursorType.Arrow));
            }
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (isPanning)
        {
            isPanning = false;
            e.Pointer.Capture(null);
            e.Handled = true;
            Cursor = isSpacePressed ? new Cursor(StandardCursorType.Hand) : (hoveredPoint != null ? new Cursor(StandardCursorType.Hand) : new Cursor(StandardCursorType.Arrow));
            InvalidateVisual();
            return;
        }

        if (isDraggingPoint)
        {
            isDraggingPoint = false;
            initialDragPoints.Clear();
            if (dragTransaction != null)
            {
                ViewModel?.UndoRedoService?.SetName(dragTransaction, "Move control point(s)");
                dragTransaction.Dispose();
                dragTransaction = null;
            }
            e.Pointer.Capture(null);
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (isSelecting)
        {
            isSelecting = false;
            selectionRect = null;
            e.Pointer.Capture(null);
            e.Handled = true;
            InvalidateVisual();
            return;
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        var vm = ViewModel;
        if (vm?.SelectedCurve == null)
            return;

        var xAxis = vm.SelectedCurve.XAxis;
        var yAxis = vm.SelectedCurve.YAxis;
        if (xAxis == null || yAxis == null)
            return;

        var pos = e.GetPosition(this);
        double worldX = xAxis.InverseTransform(pos.X);
        double worldY = yAxis.InverseTransform(pos.Y);

        // Fatores de zoom perfeitamente simétricos: 1.15 no zoom in e 1.0 / 1.15 no zoom out
        double factor = e.Delta.Y > 0 ? 1.15 : (1.0 / 1.15);

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            // Zoom apenas horizontal (tempo)
            xAxis.ZoomAt(factor, worldX);
        }
        else if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            // Zoom apenas vertical (valor)
            yAxis.ZoomAt(factor, worldY);
        }
        else
        {
            // Zoom uniforme em ambos os eixos centrado no cursor
            xAxis.ZoomAt(factor, worldX);
            yAxis.ZoomAt(factor, worldY);
        }

        e.Handled = true;
        InvalidateVisual();
    }

    private ControlPointViewModelBase? FindNearestControlPoint(AvaloniaPoint mousePos, float maxDistance)
    {
        var vm = ViewModel;
        if (vm?.SelectedCurve == null)
            return null;

        var editableCurves = new List<IEditableCurveViewModel>();
        if (vm.SelectedCurve is IEditableCurveViewModel single)
            editableCurves.Add(single);
        if (vm.SelectedCurve.Children.Count > 0)
            editableCurves.AddRange(vm.SelectedCurve.Children.OfType<IEditableCurveViewModel>());

        ControlPointViewModelBase? closest = null;
        float minDistSq = maxDistance * maxDistance;

        foreach (var curve in editableCurves)
        {
            foreach (var cp in curve.ControlPoints)
            {
                // cp.ActualPoint já vem em coordenadas de tela (pixels)
                float sx = cp.ActualPoint.X;
                float sy = cp.ActualPoint.Y;

                float dx = (float)mousePos.X - sx;
                float dy = (float)mousePos.Y - sy;
                float distSq = dx * dx + dy * dy;

                if (distSq <= minDistSq)
                {
                    minDistSq = distSq;
                    closest = cp;
                }
            }
        }

        return closest;
    }

    #endregion

    IDrawingModel? IDrawingView.Model => ViewModel as IDrawingModel;

    public void InvalidateDrawing()
    {
        InvalidateVisual();
    }
}
