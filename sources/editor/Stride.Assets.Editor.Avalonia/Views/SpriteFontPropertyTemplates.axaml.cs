using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Stride.Assets.Editor.Avalonia.Views;

public sealed class SpriteFontPropertyTemplates : ResourceDictionary
{
    public SpriteFontPropertyTemplates()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
