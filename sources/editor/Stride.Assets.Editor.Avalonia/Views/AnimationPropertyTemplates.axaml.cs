using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Stride.Assets.Editor.Avalonia.Views;

public sealed class AnimationPropertyTemplates : ResourceDictionary
{
    public AnimationPropertyTemplates()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
