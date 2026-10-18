// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Assets;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.IO;

namespace Stride.Assets.Editor.Avalonia.AssetEditors.ScriptEditor;

public class BindScriptTextEditorWorkspaceProjectIdBehavior : Behavior<Control>
{
    public static readonly StyledProperty<PackageViewModel?> PackageProperty =
        AvaloniaProperty.Register<BindScriptTextEditorWorkspaceProjectIdBehavior, PackageViewModel?>(nameof(Package));

    public PackageViewModel? Package
    {
        get => GetValue(PackageProperty);
        set => SetValue(PackageProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
        {
            AssociatedObject.Loaded += OnAssociatedObjectLoaded;
            AssociatedObject.Unloaded += OnAssociatedObjectUnloaded;
            if (AssociatedObject.IsLoaded)
            {
                AttachWorkspaceAndProject();
            }
        }
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject != null)
        {
            AssociatedObject.Loaded -= OnAssociatedObjectLoaded;
            AssociatedObject.Unloaded -= OnAssociatedObjectUnloaded;
        }
        DetachWorkspaceAndProject();
        base.OnDetaching();
    }

    private void OnAssociatedObjectLoaded(object? sender, RoutedEventArgs e)
    {
        AttachWorkspaceAndProject();
    }

    private void OnAssociatedObjectUnloaded(object? sender, RoutedEventArgs e)
    {
        DetachWorkspaceAndProject();
    }

    private async void AttachWorkspaceAndProject()
    {
        var package = Package;
        if (package == null || AssociatedObject == null)
            return;

        try
        {
            var strideAssets = await StrideAssetsViewModel.InstanceTask;
            var code = strideAssets?.Code;
            if (code == null) return;

            var projectWatcher = await code.ProjectWatcher;
            if (projectWatcher == null) return;

            var projectFullPath = (package.Package.Container as SolutionProject)?.FullPath;
            var roslynProject = projectFullPath != null
                ? projectWatcher.TrackedAssemblies.FirstOrDefault(x => new UFile(x.Project.FilePath) == projectFullPath)?.Project
                : null;

            if (roslynProject == null)
                return;

            var workspace = await code.Workspace;
            SetMember(AssociatedObject, "Workspace", workspace);
            SetMember(AssociatedObject, "ProjectId", roslynProject.Id);
        }
        catch
        {
            // Silently ignore during headless or unit testing scenarios
        }
    }

    private void DetachWorkspaceAndProject()
    {
        if (AssociatedObject != null)
        {
            SetMember(AssociatedObject, "Workspace", null);
            SetMember(AssociatedObject, "ProjectId", null);
        }
    }

    private static void SetMember(object target, string propertyName, object? value)
    {
        var prop = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(target, value);
        }
    }
}
