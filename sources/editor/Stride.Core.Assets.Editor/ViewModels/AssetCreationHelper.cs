// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

public static class AssetCreationHelper
{
    /// <summary>
    /// Attempts to find the closest location to create an asset of the given type, if the given location does not accept it.
    /// </summary>
    /// <param name="assetType">The type of asset to check</param>
    /// <param name="initialLocation">The initial location where to create the asset.</param>
    /// <param name="currentPackage">The current package view model.</param>
    /// <returns>A <see cref="DirectoryBaseViewModel"/> corresponding to a valid location to create the asset, if available. <c>Null</c> otherwise.</returns>
    [CanBeNull]
    public static DirectoryBaseViewModel? FindValidCreationLocation(Type assetType, [NotNull] DirectoryBaseViewModel initialLocation, PackageViewModel? currentPackage = null)
    {
        if (!AssetRegistry.IsAssetType(assetType)) throw new ArgumentException("The given type is not an asset type", nameof(assetType));

        if (initialLocation.Root.AcceptAssetType(assetType) && initialLocation.Root.Package.IsEditable)
            return initialLocation;

        if (currentPackage != null && currentPackage.AssetMountPoint.AcceptAssetType(assetType) && currentPackage.AssetMountPoint.Package.IsEditable)
            return currentPackage.AssetMountPoint;

        return initialLocation.Root.Package.MountPoints.FirstOrDefault(x => x.AcceptAssetType(assetType) && x.Package.IsEditable);
    }
}
