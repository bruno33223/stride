using Stride.Core.Assets.Editor.ViewModels;
// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.ViewModels;
using Stride.Editor.Preview;
using Stride.Editor.Preview.ViewModels;

namespace Stride.Assets.Presentation.ViewModel.Preview;

/// <summary>
/// Base implementation of <see cref="IAssetPreviewViewModel"/>.
/// </summary>
public abstract class AssetPreviewViewModel : DispatcherViewModel, IAssetPreviewViewModel
{
    public SessionViewModel Session { get; }

    protected AssetPreviewViewModel(SessionViewModel session)
        : base(session.SafeArgument(nameof(session)).ServiceProvider)
    {
        Session = session;
    }

    /// <inheritdoc/>
    public abstract void AttachPreview(IAssetPreview preview);
}
