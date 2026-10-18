// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Linq;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Serialization;

namespace Stride.Assets.Editor;

/// <summary>
/// Helper to locate and open the default scene for a session, mirroring the canonical behavior of Stride WPF GameStudioWindow.OpenDefaultScene.
/// </summary>
public static class DefaultSceneHelper
{
    /// <summary>
    /// Locates the default scene asset in the session.
    /// </summary>
    public static AssetViewModel? FindDefaultScene(SessionViewModel session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var startupPackage = session.LocalPackages.OfType<ProjectViewModel>().SingleOrDefault(x => x.IsCurrentProject) ?? session.CurrentProject;
        AssetViewModel? gameSettingsAsset = null;

        if (startupPackage != null)
        {
            gameSettingsAsset = startupPackage.Assets.FirstOrDefault(x => x.Url == GameSettingsAsset.GameSettingsLocation);
            if (gameSettingsAsset == null)
            {
                // Scan dependencies for game settings
                foreach (var dependency in startupPackage.PackageContainer.FlattenedDependencies)
                {
                    if (dependency.Package == null)
                        continue;

                    var dependencyPackageViewModel = session.AllPackages.FirstOrDefault(x => x.Package == dependency.Package);
                    if (dependencyPackageViewModel == null)
                        continue;

                    gameSettingsAsset = dependencyPackageViewModel.Assets.FirstOrDefault(x => x.Url == GameSettingsAsset.GameSettingsLocation);
                    if (gameSettingsAsset != null)
                        break;
                }
            }
        }

        if (gameSettingsAsset == null)
        {
            gameSettingsAsset = session.AllAssets.FirstOrDefault(x => x.Url == GameSettingsAsset.GameSettingsLocation || x.Asset is GameSettingsAsset);
        }

        if (gameSettingsAsset != null)
        {
            var defaultScene = ((GameSettingsAsset)gameSettingsAsset.Asset)?.DefaultScene;
            if (defaultScene != null)
            {
                var defaultSceneReference = AttachedReferenceManager.GetAttachedReference(defaultScene);
                if (defaultSceneReference != null)
                {
                    var sceneAsset = session.GetAssetById(defaultSceneReference.Id);
                    if (sceneAsset != null)
                        return sceneAsset;
                }
            }
        }

        // Se não encontrar GameSettingsAsset (ou se não houver DefaultScene definida), abre o primeiro SceneViewModel encontrado em session.AllAssets
        return session.AllAssets.OfType<SceneViewModel>().FirstOrDefault();
    }

    /// <summary>
    /// Opens the default scene in the session's editor collection.
    /// </summary>
    public static bool OpenDefaultScene(SessionViewModel session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var scene = FindDefaultScene(session);
        if (scene != null)
        {
            session.ActiveAssetView.SelectAssets(scene.Yield());
            session.EditorCollection.OpenAssetEditor(scene);
            return true;
        }

        return false;
    }
}
