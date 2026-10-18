// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Stride.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Templates;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Translation;
using Stride.Engine;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// Interaction logic for ScriptNameWindow.axaml
/// </summary>
public partial class ScriptNameWindow : Window, IScriptNameDialog, IModalDialog, INotifyPropertyChanged
{
    private readonly ViewModelServiceProvider services;
    private static readonly List<string> ReservedNames = new();
    private readonly bool enableTemplateSelect;
    private readonly TemplateAssetDescription? defaultScriptTemplate;
    private DialogResult modalResult = DialogResult.Cancel;

    static ScriptNameWindow()
    {
        // Reserve the name of the base classes and interfaces
        ReservedNames.Add(nameof(AsyncScript));
        ReservedNames.Add(nameof(SyncScript));
        ReservedNames.Add(nameof(StartupScript));
        typeof(ScriptComponent).GetInterfaces().ForEach(x => ReservedNames.Add(x.Name));
        var type = typeof(ScriptComponent);
        while (type != typeof(object) && type != null)
        {
            ReservedNames.Add(type.Name);
            type = type.BaseType;
        }
    }

    public ScriptNameWindow() : this(string.Empty, string.Empty, null, false, null) { }

    public ScriptNameWindow(string defaultClassName, string defaultNamespace, TemplateAssetDescription? defaultScriptTemplate, bool enableTemplateSelect, IEnumerable<TemplateAssetDescription>? scriptTemplates)
    {
        var dispatcher = new NullDispatcherService();
        services = new ViewModelServiceProvider(new object[] { dispatcher });
        InitializeComponent();
        DataContext = this;

        if (ClassNameTextBox != null)
            ClassNameTextBox.Text = defaultClassName;
        if (NamespaceTextBox != null)
            NamespaceTextBox.Text = defaultNamespace;

        ClassName = defaultClassName;
        Namespace = defaultNamespace;
        this.defaultScriptTemplate = defaultScriptTemplate;
        this.enableTemplateSelect = enableTemplateSelect;
        ScriptTemplate = defaultScriptTemplate;

        if (TemplateComboBox != null)
        {
            TemplateComboBox.IsVisible = enableTemplateSelect;
            if (TemplateLabel != null)
                TemplateLabel.IsVisible = enableTemplateSelect;

            if (enableTemplateSelect && scriptTemplates != null)
            {
                var list = scriptTemplates.ToList();
                TemplateComboBox.ItemsSource = list;
                TemplateComboBox.SelectedItem = defaultScriptTemplate ?? list.FirstOrDefault();
            }
        }
    }

    public string ClassName { get; private set; } = string.Empty;

    public string Namespace { get; private set; } = string.Empty;

    public TemplateAssetDescription? ScriptTemplate { get; private set; }

    public DialogResult Result => modalResult;

    public void Validate()
    {
        var classText = ClassNameTextBox?.Text ?? ClassName;
        var nsText = NamespaceTextBox?.Text ?? Namespace;

        ClassName = Utilities.BuildValidClassName(classText, ReservedNames);
        Namespace = Utilities.BuildValidNamespaceName(nsText, ReservedNames);
        ScriptTemplate = enableTemplateSelect && TemplateComboBox?.SelectedItem is TemplateAssetDescription selected
            ? selected
            : defaultScriptTemplate;

        if (string.IsNullOrWhiteSpace(ClassName) || string.IsNullOrWhiteSpace(Namespace))
        {
            var dialogService = services.TryGet<IDialogService>() ?? SessionViewModel.Instance?.ServiceProvider?.TryGet<IDialogService>();
            if (dialogService != null)
            {
                dialogService.MessageBoxAsync(Tr._p("Message", "The names you entered are invalid or empty."), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            return;
        }

        modalResult = DialogResult.Ok;
        Close();
    }

    public void RequestClose(DialogResult result)
    {
        modalResult = result;
        if (modalResult != DialogResult.Ok)
        {
            ClassName = string.Empty;
            Namespace = string.Empty;
            ScriptTemplate = null;
        }
        Close();
    }

    public void Cancel()
    {
        RequestClose(DialogResult.Cancel);
    }

    private void ButtonOk(object? sender, RoutedEventArgs e)
    {
        Validate();
    }

    private void ButtonCancel(object? sender, RoutedEventArgs e)
    {
        Cancel();
    }

    private void TextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            Validate();
    }

    public async Task<DialogResult> ShowModal()
    {
        if (!Dispatcher.UIThread.CheckAccess())
            return await Dispatcher.UIThread.InvokeAsync(ShowModal);

        var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner != null && owner != this && owner.IsVisible)
        {
            await ShowDialog(owner);
        }
        else
        {
            var tcs = new TaskCompletionSource<bool>();
            Closed += (_, _) => tcs.TrySetResult(true);
            try
            {
                Show();
            }
            catch
            {
                return modalResult;
            }
            await tcs.Task;
        }

        return modalResult;
    }
}
