// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using Avalonia.Media;
using Stride.Core.Assets.Editor.Avalonia.View.Behaviors;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestPropertyValidationBehaviors
{
    [Fact]
    public void TestTextBoxPropertyValueValidationBehaviorProperties()
    {
        var behavior = new TextBoxPropertyValueValidationBehavior();
        Assert.NotNull(behavior.BorderBrush);
        Assert.Equal(3.0, behavior.BorderCornerRadius);
        Assert.Equal(2.0, behavior.BorderThickness);

        behavior.BorderCornerRadius = 5.0;
        Assert.Equal(5.0, behavior.BorderCornerRadius);

        behavior.BorderThickness = 1.5;
        Assert.Equal(1.5, behavior.BorderThickness);
    }

    [Fact]
    public void TestTextBoxVectorPropertyValueValidationBehaviorProperties()
    {
        var behavior = new TextBoxVectorPropertyValueValidationBehavior();
        Assert.NotNull(behavior.BorderBrush);
        Assert.Equal(3.0, behavior.BorderCornerRadius);
        Assert.Equal(2.0, behavior.BorderThickness);

        behavior.BorderCornerRadius = 4.0;
        Assert.Equal(4.0, behavior.BorderCornerRadius);
    }

    [Fact]
    public void TestListBoxHighlightedItemBehaviorProperties()
    {
        var behavior = new ListBoxHighlightedItemBehavior();
        Assert.True(behavior.IsEnabled);
        Assert.False(behavior.UseSelectedItemIfAvailable);
        Assert.Equal(0.0, behavior.DelayToUpdate);

        behavior.IsEnabled = false;
        Assert.False(behavior.IsEnabled);

        behavior.DelayToUpdate = 0.5;
        Assert.Equal(0.5, behavior.DelayToUpdate);

        var dummyItem = new object();
        behavior.HighlightedItem = dummyItem;
        Assert.Same(dummyItem, behavior.HighlightedItem);
    }

    [Fact]
    public void TestTreeViewAutoExpandBehaviorInstantiation()
    {
        var behavior = new TreeViewAutoExpandBehavior();
        Assert.NotNull(behavior);
    }
}
