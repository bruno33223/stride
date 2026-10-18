using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Stride.Assets.Editor.Avalonia.Views;

public sealed class UIPropertyTemplates : ResourceDictionary
{
    public UIPropertyTemplates()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
