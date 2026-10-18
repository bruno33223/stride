using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Stride.Assets.Editor.Avalonia.Views;

public sealed class SkeletonPropertyTemplates : ResourceDictionary
{
    public SkeletonPropertyTemplates()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
