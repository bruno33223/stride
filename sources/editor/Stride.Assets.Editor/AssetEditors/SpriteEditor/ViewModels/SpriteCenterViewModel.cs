// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Quantum;
using Stride.Assets.Sprite;

namespace Stride.Assets.Editor.ViewModels;

public class SpriteCenterViewModel : ResizableSpriteInfoPartViewModel
{
    private readonly MemberGraphNodeBinding<bool> centerFromMiddleBinding;
    private readonly MemberGraphNodeBinding<Vector2> spriteCenterNodeBinding;
    private readonly MemberGraphNodeBinding<Rectangle> textureRegionBinding;

    public SpriteCenterViewModel(SpriteInfoViewModel sprite, IObjectNode spriteNode)
        : base(sprite)
    {
        var spriteCenterNode = spriteNode[nameof(SpriteInfo.Center)];
        spriteCenterNodeBinding = new MemberGraphNodeBinding<Vector2>(spriteCenterNode, nameof(Center), OnPropertyChanging, OnPropertyChanged, UndoRedoService);
        var centerFromMiddleNode = spriteNode[nameof(SpriteInfo.CenterFromMiddle)];
        centerFromMiddleBinding = new MemberGraphNodeBinding<bool>(centerFromMiddleNode, nameof(CenterFromMiddle), OnPropertyChanging, OnPropertyChanged, UndoRedoService);

        var textureRegionNode = spriteNode[nameof(SpriteInfo.TextureRegion)];
        textureRegionBinding = new MemberGraphNodeBinding<Rectangle>(textureRegionNode, nameof(ActualCenter), OnPropertyChanging, OnPropertyChanged, UndoRedoService);

        DependentProperties.Add(nameof(Center), new[] { nameof(ActualCenter), nameof(ActualCenterVector), nameof(ActualCenterX), nameof(ActualCenterY) });
        DependentProperties.Add(nameof(CenterFromMiddle), new[] { nameof(ActualCenter), nameof(ActualCenterVector), nameof(ActualCenterX), nameof(ActualCenterY) });
        DependentProperties.Add(nameof(ScaleFactor), new[] { nameof(ActualCenter), nameof(ActualCenterVector), nameof(ActualCenterX), nameof(ActualCenterY) });
    }

    public Vector2 Center { get => spriteCenterNodeBinding.Value; set => spriteCenterNodeBinding.Value = value; }

    public bool CenterFromMiddle { get => centerFromMiddleBinding.Value; set => centerFromMiddleBinding.Value = value; }

    public double ActualCenterX =>
        CenterFromMiddle
            ? (Center.X + textureRegionBinding.Value.Width * 0.5) * ScaleFactor
            : (Center.X + 0.5) * ScaleFactor;

    public double ActualCenterY =>
        CenterFromMiddle
            ? (Center.Y + textureRegionBinding.Value.Height * 0.5) * ScaleFactor
            : (Center.Y + 0.5) * ScaleFactor;

    public Thickness ActualCenter => new(ActualCenterX, ActualCenterY, 0.0, 0.0);

    public Vector2 ActualCenterVector => new((float)ActualCenterX, (float)ActualCenterY);

    /// <inheritdoc/>
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(SpriteCenterViewModel));
        spriteCenterNodeBinding.Dispose();
        centerFromMiddleBinding.Dispose();
        textureRegionBinding.Dispose();
        base.Destroy();
    }

    protected override void OnResizeDelta(ResizingDirection direction, double horizontalChange, double verticalChange)
    {
        var scaledHorizontalChange = (float)Math.Round(horizontalChange / ScaleFactor, MidpointRounding.AwayFromZero);
        var scaledVerticalChange = (float)Math.Round(verticalChange / ScaleFactor, MidpointRounding.AwayFromZero);
        var newCenter = Center;
        if (direction.HasFlag(ResizingDirection.Center))
        {
            newCenter.X += scaledHorizontalChange;
            newCenter.Y += scaledVerticalChange;
        }
        Center = newCenter;
    }

    protected override string? ComputeTransactionName(ResizingDirection direction, double horizontalChange, double verticalChange)
    {
        return direction == ResizingDirection.Center ? "Move sprite center" : null;
    }
}
