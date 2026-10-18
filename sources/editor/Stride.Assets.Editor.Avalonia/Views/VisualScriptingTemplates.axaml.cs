// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Stride.Assets.Editor.Avalonia.Views;

public partial class VisualScriptingTemplates : ResourceDictionary
{
    public VisualScriptingTemplates()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
