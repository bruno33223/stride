// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;

public class TemplateDescriptionViewModel : DispatcherViewModel, ITemplateDescriptionViewModel
{
    protected readonly TemplateDescription Template;

    public TemplateDescriptionViewModel(IViewModelServiceProvider serviceProvider, TemplateDescription template)
        : base(serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(template);
        Template = template;

        if (template.Icon is not null && !string.IsNullOrEmpty(template.Icon))
            Icon = GetPath(template.Icon);

        Screenshots = template.Screenshots.Select(GetPath).Where(File.Exists).Cast<object>().ToList();
    }

    public string Name => Template.Name ?? string.Empty;

    public string Description => Template.Description ?? string.Empty;

    public string FullDescription => Template.FullDescription ?? string.Empty;

    public string Group => Template.Group ?? string.Empty;

    public int Order => Template.Order;

    public Guid Id => Template.Id;

    public string DefaultOutputName => Template.DefaultOutputName ?? string.Empty;

    public object? Icon { get; set; }

    public IEnumerable<object> Screenshots { get; set; }

    public TemplateDescription GetTemplate() => Template;

    protected string GetPath(UFile imagePath)
    {
        return UPath.Combine(Template.FullPath.GetFullDirectory(), imagePath);
    }
}
