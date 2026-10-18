// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.TemplateEngine.Abstractions;
using Stride.Assets.Editor.ViewModels;

namespace Stride.Assets.Editor.Services;

/// <summary>
/// This interface represents Stride-specific dialog service.
/// </summary>
public interface IStrideDialogService
{
    /// <summary>
    /// Creates an entity picker dialog.
    /// </summary>
    /// <param name="editor">The editor view model currently in use.</param>
    /// <returns>An instance of the <see cref="IEntityPickerDialog"/> interface.</returns>
    IEntityPickerDialog CreateEntityPickerDialog(EntityHierarchyEditorViewModel editor);

    /// <summary>
    /// Creates an entity component picker dialog.
    /// </summary>
    /// <param name="editor">The editor view model currently in use.</param>
    /// <param name="componentType">The type of component to pickup.</param>
    /// <returns>An instance of the <see cref="IEntityPickerDialog"/> interface.</returns>
    IEntityPickerDialog CreateEntityComponentPickerDialog(EntityHierarchyEditorViewModel editor, Type componentType);

    /// <summary>
    /// Creates a script name dialog.
    /// </summary>
    /// <param name="defaultClassName">The default class name to suggest.</param>
    /// <param name="defaultNamespace">The default namespace to suggest.</param>
    /// <param name="defaultScriptTemplate">The default script template description.</param>
    /// <param name="enableTemplateSelect">Whether template selection should be enabled.</param>
    /// <param name="scriptTemplates">Available script template descriptions.</param>
    /// <returns>An instance of the <see cref="IScriptNameDialog"/> interface.</returns>
    IScriptNameDialog CreateScriptNameDialog(string defaultClassName, string defaultNamespace, Stride.Core.Assets.Templates.TemplateAssetDescription? defaultScriptTemplate, bool enableTemplateSelect, System.Collections.Generic.IEnumerable<Stride.Core.Assets.Templates.TemplateAssetDescription>? scriptTemplates);

    /// <summary>
    /// Creates a dialog to select and update package target platforms.
    /// </summary>
    /// <param name="installedPlatforms">The collection of already installed platforms.</param>
    /// <param name="forcePlatformRegenerationVisible">Whether the force regeneration option is visible.</param>
    /// <returns>An instance of the <see cref="IUpdatePlatformsDialog"/> interface.</returns>
    IUpdatePlatformsDialog CreateUpdatePlatformsDialog(System.Collections.Generic.ICollection<Stride.Core.PlatformType> installedPlatforms, bool forcePlatformRegenerationVisible);

    /// <summary>
    /// Creates a parameters dialog for dotnet new templates.
    /// </summary>
    /// <param name="template">The dotnet new template information.</param>
    /// <returns>An instance of the <see cref="IDotNetNewTemplateParametersDialog"/> interface.</returns>
    IDotNetNewTemplateParametersDialog CreateDotNetNewTemplateParametersDialog(ITemplateInfo template);

    /// <summary>
    /// Creates a dialog for 3D model template parameters.
    /// </summary>
    /// <returns>An instance of the <see cref="IModelAssetTemplateDialog"/> interface.</returns>
    IModelAssetTemplateDialog CreateModelAssetTemplateDialog();

    /// <summary>
    /// Creates a dialog for project library template parameters.
    /// </summary>
    /// <param name="defaultLibraryName">The initial library name.</param>
    /// <returns>An instance of the <see cref="IProjectLibraryDialog"/> interface.</returns>
    IProjectLibraryDialog CreateProjectLibraryDialog(string? defaultLibraryName = null);
}


