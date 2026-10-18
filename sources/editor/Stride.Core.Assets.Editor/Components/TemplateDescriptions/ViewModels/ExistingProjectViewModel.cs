// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;

public class ExistingProjectViewModel : DispatcherViewModel, ITemplateDescriptionViewModel
{
    private readonly Action<ExistingProjectViewModel> removeAction;

    public ExistingProjectViewModel(IViewModelServiceProvider serviceProvider, UFile path, Action<ExistingProjectViewModel> removeAction)
        : base(serviceProvider)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Id = Guid.NewGuid();
        this.removeAction = removeAction ?? throw new ArgumentNullException(nameof(removeAction));
        ExploreCommand = new AnonymousCommand(serviceProvider, Explore);
        RemoveCommand = new AnonymousCommand(serviceProvider, Remove);
    }

    public string Name => Path.GetFileNameWithoutExtension();

    public string Description => Path.ToOSPath();

    public string FullDescription => string.Empty;

    public string Group => string.Empty;

    public Guid Id { get; }

    public string DefaultOutputName => string.Empty;

    public UFile Path { get; }

    public object? Icon => null;

    public IEnumerable<object> Screenshots => Enumerable.Empty<object>();

    public ICommandBase ExploreCommand { get; }

    public ICommandBase RemoveCommand { get; }

    public TemplateDescription? GetTemplate()
    {
        return null;
    }

    private void Explore()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var startInfo = new ProcessStartInfo("explorer.exe", $"/select,\"{Path.ToOSPath()}\"") { UseShellExecute = true };
                Process.Start(startInfo);
            }
            else if (OperatingSystem.IsMacOS())
            {
                var startInfo = new ProcessStartInfo("open", $"-R \"{Path.ToOSPath()}\"") { UseShellExecute = true };
                Process.Start(startInfo);
            }
            else
            {
                var dir = Path.GetFullDirectory();
                var startInfo = new ProcessStartInfo("xdg-open", $"\"{dir}\"") { UseShellExecute = true };
                Process.Start(startInfo);
            }
        }
        catch
        {
            // Ignore failure to open file explorer
        }
    }

    private void Remove()
    {
        removeAction(this);
    }
}
