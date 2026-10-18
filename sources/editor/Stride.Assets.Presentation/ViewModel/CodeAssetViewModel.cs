// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Effect;
using Stride.Core.Assets;
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.IO;
using Stride.Editor.Build;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Assets.Presentation.ViewModel
{
    /// <summary>
    /// View model for <see cref="SourceCodeAsset"/>.
    /// </summary>
    /// <typeparam name="TSourceCodeAsset"></typeparam>
    public abstract class CodeAssetViewModel<TSourceCodeAsset> : AssetViewModel<TSourceCodeAsset> where TSourceCodeAsset : SourceCodeAsset
    {
        protected CodeAssetViewModel(ConstructorParameters parameters)
            : base(parameters)
        {
        }

        /// <inheritdoc/>
        protected override void OnSessionSaved()
        {
            if (Asset is EffectShaderAsset)
            {
                //recompile shaders...
                var shaderImporter = new StrideShaderImporter();
                var shaderBuildSteps = Session is SessionViewModel sessionVm ? shaderImporter.CreateUserShaderBuildSteps(sessionVm) : null;
                if (shaderBuildSteps != null)
                {
                    var builder = Session.ServiceProvider.Get<AssetBuilderService>();
                    builder.PushBuildUnit(new PrecompiledAssetBuildUnit(AssetBuildUnitIdentifier.Default, shaderBuildSteps, true));
                }
            }
        }
        
        /// <summary>
        /// The full path to the source code asset on disk
        /// </summary>
        public UFile FullPath => AssetItem.FullPath;
    }

    [AssetViewModel<SourceCodeAsset>]
    public class CodeAssetViewModel : CodeAssetViewModel<SourceCodeAsset>
    {
        public CodeAssetViewModel(ConstructorParameters parameters) : base(parameters)
        {
        }
    }
}
