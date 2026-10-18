// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Stride.Assets.Editor.Services;
using Stride.Assets.Scripts;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Templates;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;
using Stride.Core.Reflection;
using Stride.Core.Translation;

namespace Stride.Assets.Editor.Templates;

public class ScriptTemplateGenerator : AssetTemplateGenerator
{
    private const string AssetTypeName = "ScriptSourceFileAsset";

    public static readonly ScriptTemplateGenerator Default = new();

    private static readonly PropertyKey<string> ClassNameKey = new("ClassNameKey", typeof(ScriptTemplateGenerator));
    private static readonly PropertyKey<string> DefaultClassNameKey = new(nameof(DefaultClassNameKey), typeof(ScriptTemplateGenerator));
    private static readonly PropertyKey<bool> EnableTemplateSelectKey = new(nameof(EnableTemplateSelectKey), typeof(ScriptTemplateGenerator));
    private static readonly PropertyKey<bool> SaveSessionKey = new("SaveSessionKey", typeof(ScriptTemplateGenerator));

    public static Func<string, string, TemplateAssetDescription?, bool, IEnumerable<TemplateAssetDescription>?, IScriptNameDialog>? CreateScriptNameDialogDelegate { get; set; }

    public static void SetClassName(AssetTemplateGeneratorParameters parameters, string className) => parameters.Tags.Set(ClassNameKey, className);

    public static IEnumerable<TemplateAssetDescription> GetScriptTemplateAssetDescriptions(IEnumerable<TemplateDescription> templates)
    {
        if (templates == null) throw new ArgumentNullException(nameof(templates));
        return templates.OfType<TemplateAssetDescription>().Where(t => t.AssetTypeName == AssetTypeName);
    }

    public static PropertyContainer GetAssetOverrideParameters(string? defaultClassName, bool enableTemplateSelect = true)
    {
        var customParameters = new PropertyContainer();

        if (!string.IsNullOrEmpty(defaultClassName))
        {
            customParameters[DefaultClassNameKey] = defaultClassName;
        }

        customParameters[EnableTemplateSelectKey] = enableTemplateSelect;
        return customParameters;
    }

    public static IScriptNameDialog? CreateScriptNameDialog(string defaultClassName, string defaultNamespace, TemplateAssetDescription? defaultScriptTemplate, bool enableTemplateSelect, IEnumerable<TemplateAssetDescription>? scriptTemplates)
    {
        if (CreateScriptNameDialogDelegate != null)
            return CreateScriptNameDialogDelegate(defaultClassName, defaultNamespace, defaultScriptTemplate, enableTemplateSelect, scriptTemplates);

        var dialogService = SessionViewModel.Instance?.ServiceProvider?.TryGet<IStrideDialogService>();
        if (dialogService != null)
            return dialogService.CreateScriptNameDialog(defaultClassName, defaultNamespace, defaultScriptTemplate, enableTemplateSelect, scriptTemplates);

        var scriptNameWinType = Type.GetType("Stride.Assets.Editor.Avalonia.Views.ScriptNameWindow, Stride.Assets.Editor.Avalonia");
        if (scriptNameWinType != null)
        {
            return (IScriptNameDialog?)Activator.CreateInstance(scriptNameWinType, defaultClassName, defaultNamespace, defaultScriptTemplate, enableTemplateSelect, scriptTemplates);
        }

        return null;
    }

    public override bool IsSupportingTemplate(TemplateDescription templateDescription)
    {
        if (templateDescription == null) throw new ArgumentNullException(nameof(templateDescription));
        if (templateDescription is not TemplateAssetDescription assetTemplate)
            return false;

        var assetType = assetTemplate.GetAssetType();
        return assetType != null && typeof(ScriptSourceFileAsset).IsAssignableFrom(assetType);
    }

    protected override async Task<bool> PrepareAssetCreation(AssetTemplateGeneratorParameters parameters)
    {
        if (!parameters.Unattended)
        {
            string defaultClassName = parameters.Description.DefaultOutputName;

            if (parameters.Tags.TryGetValue(DefaultClassNameKey, out string? className) && !string.IsNullOrEmpty(className))
            {
                defaultClassName = className;
            }

            bool enableTemplateSelect = parameters.TryGetTag(EnableTemplateSelectKey);

            IEnumerable<TemplateAssetDescription>? scriptTemplates = null;

            if (enableTemplateSelect)
            {
                scriptTemplates = GetScriptTemplateAssetDescriptions(TemplateManager.FindTemplates(TemplateScope.Asset, parameters.Package.Session));
            }

            var window = CreateScriptNameDialog(defaultClassName, parameters.Namespace, parameters.Description as TemplateAssetDescription, enableTemplateSelect, scriptTemplates);
            if (window != null)
            {
                var dialogResult = await window.ShowModal();

                if (dialogResult == DialogResult.Cancel)
                    return false;

                parameters.Namespace = window.Namespace;
                parameters.Tags.Set(ClassNameKey, window.ClassName);
                parameters.Description = window.ScriptTemplate;
            }
            else
            {
                parameters.Tags.Set(ClassNameKey, defaultClassName);
            }

            var ask = Stride.Core.Assets.Editor.Settings.EditorSettings.AskBeforeSavingNewScripts.GetValue();
            if (ask)
            {
                var buttons = DialogHelper.CreateButtons(new[]
                {
                    Tr._p("Button", "Save"),
                    Tr._p("Button", "Cancel")
                }, 1, 2);
                var message = Tr._p("Message", "You can't use scripts until you save them. Do you want to save now?");
                var checkedMessage = Stride.Core.Assets.Editor.Settings.EditorSettings.AlwaysSaveNewScriptsWithoutAsking;
                var dialogService = SessionViewModel.Instance?.ServiceProvider?.TryGet<IDialogService>();
                if (dialogService != null)
                {
                    var result = await dialogService.CheckedMessageBoxAsync(message, false, checkedMessage, buttons, MessageBoxImage.Question);

                    if (result.IsChecked == true)
                    {
                        Stride.Core.Assets.Editor.Settings.EditorSettings.AskBeforeSavingNewScripts.SetValue(false);
                        Stride.Core.Assets.Editor.Settings.EditorSettings.Save();
                    }
                    parameters.Tags.Set(SaveSessionKey, result.Result == 1);
                }
                else
                {
                    parameters.Tags.Set(SaveSessionKey, true);
                }
            }
            else
            {
                parameters.Tags.Set(SaveSessionKey, true);
            }
        }
        else
        {
            // TODO: Some templates save, some don't (and behavior can even change in Unattended mode)
            // For consistency, should this be moved to a common parameters.Saving?
            parameters.Tags.Set(SaveSessionKey, false);
        }

        return true;
    }

    protected override IEnumerable<AssetItem> CreateAssets(AssetTemplateGeneratorParameters parameters)
    {
        var desc = parameters.Description;
        var scriptFile = Path.ChangeExtension(desc.FullPath, ScriptSourceFileAsset.Extension);

        var scriptContent = File.Exists(scriptFile) ? File.ReadAllText(scriptFile) : string.Empty;
        parameters.Name = parameters.Tags.Get(ClassNameKey);
        var location = GenerateLocation(parameters);
        scriptContent = scriptContent.Replace("##Namespace##", parameters.Namespace);
        scriptContent = scriptContent.Replace("##Scriptname##", location.GetFileNameWithoutExtension());

        var asset = ObjectFactoryRegistry.NewInstance<ScriptSourceFileAsset>();
        asset.Id = SourceCodeAsset.GenerateIdFromLocation(parameters.Package.Meta.Name, location);
        asset.Text = scriptContent;
        yield return new AssetItem(location, asset);
    }

    protected override void PostAssetCreation(AssetTemplateGeneratorParameters parameters, AssetItem assetItem)
    {
        parameters.RequestSessionSave = parameters.TryGetTag(SaveSessionKey);
    }
}
