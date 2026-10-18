using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Stride.Assets.Editor.Avalonia.Views;

public sealed class MaterialPropertyTemplates : ResourceDictionary
{
    public MaterialPropertyTemplates()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
