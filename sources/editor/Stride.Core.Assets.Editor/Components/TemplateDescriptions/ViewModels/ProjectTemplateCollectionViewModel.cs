// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Design;
using Stride.Core.IO;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;

public abstract class ProjectTemplateCollectionViewModel : TemplateDescriptionCollectionViewModel
{
    private UDirectory location = string.Empty;

    protected ProjectTemplateCollectionViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        BrowseDirectoryCommand = new AnonymousTaskCommand<string>(serviceProvider, BrowseDirectory);
    }

    public UDirectory Location
    {
        get => location;
        set => SetValue(ref location, value);
    }

    public ICommandBase BrowseDirectoryCommand { get; }

    public override bool ValidateProperties(out string error)
    {
        if (SelectedTemplate is TemplateDescriptionViewModel)
        {
            if (string.IsNullOrWhiteSpace(Location) || !UPath.IsValid(Location))
            {
                error = "Invalid output directory.";
                return false;
            }
            if (!UFile.IsValid(Name) || Name.Contains(UPath.DirectorySeparatorString) || Name.Contains(UPath.DirectorySeparatorStringAlt))
            {
                error = "Invalid name.";
                return false;
            }
            var outputPath = UPath.Combine<UDirectory>(Location, Name);
            if (Directory.Exists(outputPath))
            {
                error = "Cannot use the selected name because a folder with the same name already exists in the same location.";
                return false;
            }
        }
        error = string.Empty;
        return true;
    }

    protected string GenerateUniqueNameAtLocation(List<string>? conflictingNames = null)
    {
        conflictingNames ??= [];

        if (Directory.Exists(Location))
        {
            var existingFolders = Directory.GetDirectories(Location).Select(Path.GetFileName).Where(x => x is not null).Cast<string>();
            conflictingNames.AddRange(existingFolders);
        }
        var defaultName = SelectedTemplate?.DefaultOutputName ?? "Project";
        return NamingHelper.ComputeNewName(defaultName, conflictingNames, x => x, "{0}{1}");
    }

    private async Task BrowseDirectory(string variableName)
    {
        var directory = await ServiceProvider.Get<IDialogService>().OpenFolderPickerAsync(Location);
        if (directory is not null)
        {
            var property = GetType().GetProperty(variableName);
            property?.SetValue(this, directory);
        }
    }
}
