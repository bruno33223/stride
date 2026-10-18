// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

public class NewProjectTemplateCollectionViewModel : Components.TemplateDescriptions.ViewModels.NewProjectTemplateCollectionViewModel
{
    public NewProjectTemplateCollectionViewModel(IViewModelServiceProvider serviceProvider, SessionViewModel session)
        : base(serviceProvider, session)
    {
    }
}
