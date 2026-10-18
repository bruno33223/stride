// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Assets.Editor.Services;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Engine;

namespace Stride.Assets.Editor.Avalonia.Services
{
    /// <summary>
    /// Avalonia implementation of the <see cref="IStrideDialogService"/>.
    /// </summary>
    public class StrideDialogService : IStrideDialogService
    {
        /// <inheritdoc/>
        public IEntityPickerDialog CreateEntityPickerDialog(EntityHierarchyEditorViewModel editor)
        {
            ArgumentNullException.ThrowIfNull(editor);
            return new EntityPickerWindow(editor, null);
        }

        /// <inheritdoc/>
        public IEntityPickerDialog CreateEntityComponentPickerDialog(EntityHierarchyEditorViewModel editor, Type componentType)
        {
            ArgumentNullException.ThrowIfNull(editor);
            if (!typeof(EntityComponent).IsAssignableFrom(componentType) && !componentType.IsAbstract)
                throw new ArgumentException(@"The given component type does not inherit from EntityComponent.", nameof(componentType));

            return new EntityPickerWindow(editor, componentType);
        }

        /// <summary>
        /// Optional factory for creating script name dialogs, useful for testing or UI overrides.
        /// </summary>
        public static Func<string, string, Stride.Core.Assets.Templates.TemplateAssetDescription?, bool, System.Collections.Generic.IEnumerable<Stride.Core.Assets.Templates.TemplateAssetDescription>?, IScriptNameDialog>? ScriptNameDialogFactory { get; set; }

        /// <inheritdoc/>
        public IScriptNameDialog CreateScriptNameDialog(string defaultClassName, string defaultNamespace, Stride.Core.Assets.Templates.TemplateAssetDescription? defaultScriptTemplate, bool enableTemplateSelect, System.Collections.Generic.IEnumerable<Stride.Core.Assets.Templates.TemplateAssetDescription>? scriptTemplates)
        {
            if (ScriptNameDialogFactory != null)
                return ScriptNameDialogFactory(defaultClassName, defaultNamespace, defaultScriptTemplate, enableTemplateSelect, scriptTemplates);

            return new ScriptNameWindow(defaultClassName, defaultNamespace, defaultScriptTemplate, enableTemplateSelect, scriptTemplates);
        }

        /// <summary>
        /// Optional factory for creating update platforms dialogs, useful for testing or UI overrides.
        /// </summary>
        public static Func<System.Collections.Generic.ICollection<Stride.Core.PlatformType>, bool, IUpdatePlatformsDialog>? UpdatePlatformsDialogFactory { get; set; }

        /// <inheritdoc/>
        public IUpdatePlatformsDialog CreateUpdatePlatformsDialog(System.Collections.Generic.ICollection<Stride.Core.PlatformType> installedPlatforms, bool forcePlatformRegenerationVisible)
        {
            if (UpdatePlatformsDialogFactory != null)
                return UpdatePlatformsDialogFactory(installedPlatforms, forcePlatformRegenerationVisible);

            return new UpdatePlatformsWindow(installedPlatforms, forcePlatformRegenerationVisible);
        }

        /// <summary>
        /// Optional factory for creating dotnet new template parameters dialogs, useful for testing or UI overrides.
        /// </summary>
        public static Func<Microsoft.TemplateEngine.Abstractions.ITemplateInfo, IDotNetNewTemplateParametersDialog>? DotNetNewTemplateParametersDialogFactory { get; set; }

        /// <inheritdoc/>
        public IDotNetNewTemplateParametersDialog CreateDotNetNewTemplateParametersDialog(Microsoft.TemplateEngine.Abstractions.ITemplateInfo template)
        {
            if (DotNetNewTemplateParametersDialogFactory != null)
                return DotNetNewTemplateParametersDialogFactory(template);

            return new DotNetNewTemplateParametersWindow(template);
        }

        /// <summary>
        /// Optional factory for creating model asset template dialogs, useful for testing or UI overrides.
        /// </summary>
        public static Func<IModelAssetTemplateDialog>? ModelAssetTemplateDialogFactory { get; set; }

        /// <inheritdoc/>
        public IModelAssetTemplateDialog CreateModelAssetTemplateDialog()
        {
            if (ModelAssetTemplateDialogFactory != null)
                return ModelAssetTemplateDialogFactory();

            return new ModelAssetTemplateWindow();
        }

        /// <summary>
        /// Optional factory for creating project library dialogs, useful for testing or UI overrides.
        /// </summary>
        public static Func<string?, IProjectLibraryDialog>? ProjectLibraryDialogFactory { get; set; }

        /// <inheritdoc/>
        public IProjectLibraryDialog CreateProjectLibraryDialog(string? defaultLibraryName = null)
        {
            if (ProjectLibraryDialogFactory != null)
                return ProjectLibraryDialogFactory(defaultLibraryName);

            return new ProjectLibraryWindow(defaultLibraryName ?? string.Empty);
        }
    }
}
