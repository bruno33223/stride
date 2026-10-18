// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;
using Stride.Rendering;

namespace Stride.Assets.Editor.Templates;

public class ImportModelFromFileViewModel : DispatcherViewModel
{
    [DataContract]
    public class DummyReferenceContainer : IPropertyProviderViewModel
    {
        private readonly IObjectNode? rootNode;

        public DummyReferenceContainer()
        {
            if (SessionViewModel.Instance?.AssetNodeContainer != null)
            {
                rootNode = SessionViewModel.Instance.AssetNodeContainer.GetOrCreateNode(this);
            }
        }

        public Skeleton? Skeleton { get; set; }

        bool IPropertyProviderViewModel.CanProvidePropertiesViewModel => true;

        IObjectNode IPropertyProviderViewModel.GetRootNode() => rootNode!;

        bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member) => true;

        bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => true;
    }

    private readonly DummyReferenceContainer referenceContainer = new DummyReferenceContainer();

    private bool importMaterials = true;
    private bool showDeduplicateMaterialsCheckBox = true;
    private bool showFbxDedupeNotSupportedWarning;
    private bool deduplicateMaterials = true;
    private bool importTextures = true;
    private bool importAnimations = true;
    private bool importSkeleton = true;
    private bool dontImportSkeleton;
    private bool reuseSkeleton;

    public ImportModelFromFileViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        ReferenceViewModel = GraphViewModel.Create(serviceProvider, new[] { referenceContainer });
    }

    public bool ImportMaterials { get => importMaterials; set => SetValue(ref importMaterials, value); }

    public bool ShowDeduplicateMaterialsCheckBox { get => showDeduplicateMaterialsCheckBox; set => SetValue(ref showDeduplicateMaterialsCheckBox, value); }

    public bool ShowFbxDedupeNotSupportedWarning { get => showFbxDedupeNotSupportedWarning; set => SetValue(ref showFbxDedupeNotSupportedWarning, value); }

    public bool DeduplicateMaterials { get => deduplicateMaterials; set => SetValue(ref deduplicateMaterials, value); }

    public bool ImportTextures { get => importTextures; set => SetValue(ref importTextures, value); }

    public bool ImportAnimations { get => importAnimations; set => SetValue(ref importAnimations, value); }

    public bool ImportSkeleton { get => importSkeleton; set => SetValue(ref importSkeleton, value); }

    public bool DontImportSkeleton { get => dontImportSkeleton; set => SetValue(ref dontImportSkeleton, value); }

    public bool ReuseSkeleton { get => reuseSkeleton; set => SetValue(ref reuseSkeleton, value); }

    public Skeleton? SkeletonToReuse { get => referenceContainer.Skeleton; set => referenceContainer.Skeleton = value; }

    public GraphViewModel ReferenceViewModel { get; }
}
