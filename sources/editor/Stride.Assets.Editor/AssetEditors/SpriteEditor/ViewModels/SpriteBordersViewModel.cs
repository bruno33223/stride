// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Quantum;
using Stride.Assets.Sprite;

namespace Stride.Assets.Editor.ViewModels;

public class SpriteBordersViewModel : ResizableSpriteInfoPartViewModel
{
    private readonly MemberGraphNodeBinding<Vector4> borderBinding;
    private readonly IMemberNode textureRegionNode;
    private bool locked;

    public SpriteBordersViewModel(SpriteInfoViewModel sprite, IObjectNode spriteNode)
        : base(sprite)
    {
        textureRegionNode = spriteNode[nameof(SpriteInfo.TextureRegion)];
        textureRegionNode.ValueChanged += OnTextureRegionValueChanged;

        var spriteBordersNode = spriteNode[nameof(SpriteInfo.Borders)];
        borderBinding = new MemberGraphNodeBinding<Vector4>(spriteBordersNode, nameof(Borders), OnPropertyChanging, OnPropertyChanged, UndoRedoService);

        DependentProperties.Add(nameof(Borders), new[] { nameof(ActualBorders), nameof(ActualBordersVector), nameof(ActualLeft), nameof(ActualTop), nameof(ActualRight), nameof(ActualBottom) });
        DependentProperties.Add(nameof(ScaleFactor), new[] { nameof(ActualBorders), nameof(ActualBordersVector), nameof(ActualLeft), nameof(ActualTop), nameof(ActualRight), nameof(ActualBottom) });
    }

    /// <inheritdoc/>
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(SpriteBordersViewModel));
        borderBinding.Dispose();
        textureRegionNode.ValueChanged -= OnTextureRegionValueChanged;
        base.Destroy();
    }

    public Vector4 Borders
    {
        get => borderBinding.Value;
        set
        {
            ClampBorders(ref value);
            var vector4 = new Vector4
            {
                X = MathF.Round(value.X, MidpointRounding.AwayFromZero),
                Y = MathF.Round(value.Y, MidpointRounding.AwayFromZero),
                Z = MathF.Round(value.Z, MidpointRounding.AwayFromZero),
                W = MathF.Round(value.W, MidpointRounding.AwayFromZero),
            };
            borderBinding.Value = vector4;
        }
    }

    public bool Locked { get => locked; set => SetValue(ref locked, value); }

    public Thickness ActualBorders => new(Borders.X * ScaleFactor, Borders.Y * ScaleFactor, Borders.Z * ScaleFactor, Borders.W * ScaleFactor);

    public Vector4 ActualBordersVector => new(Borders.X * (float)ScaleFactor, Borders.Y * (float)ScaleFactor, Borders.Z * (float)ScaleFactor, Borders.W * (float)ScaleFactor);

    public double ActualLeft => Borders.X * ScaleFactor;

    public double ActualTop => Borders.Y * ScaleFactor;

    public double ActualRight => Borders.Z * ScaleFactor;

    public double ActualBottom => Borders.W * ScaleFactor;

    private Rectangle TextureRegion => (Rectangle)textureRegionNode.Retrieve();

    protected override void OnResizeDelta(ResizingDirection direction, double horizontalChange, double verticalChange)
    {
        if (direction.HasFlag(ResizingDirection.Center))
        {
            return;
        }

        var scaledHorizontalChange = (float)Math.Round(horizontalChange / ScaleFactor, MidpointRounding.AwayFromZero);
        var scaledVerticalChange = (float)Math.Round(verticalChange / ScaleFactor, MidpointRounding.AwayFromZero);
        var deltaVector = new Vector4();
        if (direction.HasFlag(ResizingDirection.Left))
        {
            deltaVector.X = scaledHorizontalChange;
        }
        if (direction.HasFlag(ResizingDirection.Right))
        {
            deltaVector.Z = -scaledHorizontalChange;
        }
        if (direction.HasFlag(ResizingDirection.Top))
        {
            deltaVector.Y = scaledVerticalChange;
        }
        if (direction.HasFlag(ResizingDirection.Bottom))
        {
            deltaVector.W = -scaledVerticalChange;
        }
        Borders += deltaVector;
    }

    protected override string? ComputeTransactionName(ResizingDirection direction, double horizontalChange, double verticalChange)
    {
        return direction switch
        {
            ResizingDirection.Center => "Move sprite borders",
            ResizingDirection.Left or ResizingDirection.Top or ResizingDirection.Right or ResizingDirection.Bottom => $"Update sprite {direction} border",
            ResizingDirection.TopLeft or ResizingDirection.TopRight or ResizingDirection.BottomLeft or ResizingDirection.BottomRight => $"Update sprite {direction} borders.",
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
        };
    }

    private void OnTextureRegionValueChanged(object? sender, MemberNodeChangeEventArgs e)
    {
        var prevRect = (Rectangle)e.OldValue;
        var newRect = (Rectangle)e.NewValue;
        // The borders are unlocked or the texture region just moved, we keep our offsets.
        if (!Locked || (newRect.Width == prevRect.Width && newRect.Height == prevRect.Height))
            return;
        // Otherwise, calculate the delta
        var deltaVector = new Vector4
        {
            X = prevRect.Left - newRect.Left,
            Y = prevRect.Top - newRect.Top,
            Z = newRect.Right - prevRect.Right,
            W = newRect.Bottom - prevRect.Bottom,
        };
        Borders += deltaVector;
    }

    private void ClampBorders(ref Vector4 value)
    {
        value.X = MathUtil.Clamp(value.X, 0.0f, TextureRegion.Width - Borders.Z);
        value.Y = MathUtil.Clamp(value.Y, 0.0f, TextureRegion.Height - Borders.W);
        value.Z = MathUtil.Clamp(value.Z, 0.0f, TextureRegion.Width - Borders.X);
        value.W = MathUtil.Clamp(value.W, 0.0f, TextureRegion.Height - Borders.Y);
    }
}
