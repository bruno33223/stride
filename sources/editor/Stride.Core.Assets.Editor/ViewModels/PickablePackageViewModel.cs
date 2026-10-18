// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using Stride.Core.Annotations;
using Stride.Core.Assets;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Packages;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

public abstract class PickablePackageViewModel : DispatcherViewModel
{
    protected PickablePackageViewModel([NotNull] IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    public abstract string Name { get; }

    public abstract DependencyRange DependencyRange { get; }
}

public class LoadedPickablePackageViewModel : PickablePackageViewModel
{
    public LoadedPickablePackageViewModel(PackageViewModel package) : base(package.ServiceProvider)
    {
        Package = package;
    }

    public PackageViewModel Package { get; }

    public override string Name => Package.Name;

    public override DependencyRange DependencyRange =>
        (Package.Package.Container is SolutionProject project)
            ? new DependencyRange(Package.Name, new PackageVersionRange(Package.Package.Meta.Version, true), DependencyType.Project)
            {
                MSBuildProject = project.FullPath,
            }
            : new DependencyRange(Package.Name, new PackageVersionRange(Package.Package.Meta.Version, true), DependencyType.Package);
}

public class UnloadedPickablePackageViewModel : PickablePackageViewModel
{
    private readonly SessionViewModel session;
    private readonly PackageName package;

    public UnloadedPickablePackageViewModel(SessionViewModel session, PackageName package) : base(session.ServiceProvider)
    {
        this.session = session;
        this.package = package;
    }

    public override string Name => package.Id;

    public override DependencyRange DependencyRange =>
        new DependencyRange(package.Id, new PackageVersionRange(package.Version, true), DependencyType.Package);
}
