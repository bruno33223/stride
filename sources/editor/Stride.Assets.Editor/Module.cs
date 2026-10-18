// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Presentation;
using Stride.Core.Reflection;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Stride.Core.Assets.Editor.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Stride.Assets.Editor.Avalonia")]

namespace Stride.Assets.Editor;

internal class Module
{
    [Core.ModuleInitializer]
    public static void Initialize()
    {
        AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);
        AssetsPlugin.RegisterPlugin(typeof(StrideEditorPlugin));
    }
}
