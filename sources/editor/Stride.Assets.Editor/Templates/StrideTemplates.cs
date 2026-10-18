// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.Templates;
using Stride.Core.Assets.Templates;

namespace Stride.Assets.Editor.Templates
{
    public static class StrideTemplates
    {
        public static void Register()
        {
            TemplateManager.Register(new DotNetNewTemplateGenerator());
            TemplateManager.Register(UpdatePlatformsTemplateGenerator.Default);
            TemplateManager.Register(AssetFactoryTemplateGenerator.Default);
            TemplateManager.Register(AssetFromFileTemplateGenerator.Default);
            TemplateManager.Register(HeightmapFactoryTemplateGenerator.Default);
            TemplateManager.Register(HullAssetFactoryTemplateGenerator.Default);
            TemplateManager.Register(ProceduralModelFactoryTemplateGenerator.Default);
            TemplateManager.Register(ColliderShapeStaticMeshFactoryTemplateGenerator.Default);
            TemplateManager.Register(ColliderShapeHullFactoryTemplateGenerator.Default);
            TemplateManager.Register(SkyboxFactoryTemplateGenerator.Default);
            TemplateManager.Register(GraphicsCompositorTemplateGenerator.Default);
            TemplateManager.Register(ScriptTemplateGenerator.Default);
            TemplateManager.Register(SkeletonFromFileTemplateGenerator.Default);
            TemplateManager.Register(AnimationFromFileTemplateGenerator.Default);
            TemplateManager.Register(VideoFromFileTemplateGenerator.Default);
            TemplateManager.Register(SoundFromFileTemplateGenerator.Default);
            TemplateManager.Register(ModelFromFileTemplateGenerator.Default);
            TemplateManager.Register(ProjectLibraryTemplateGenerator.Default);
        }
    }
}
