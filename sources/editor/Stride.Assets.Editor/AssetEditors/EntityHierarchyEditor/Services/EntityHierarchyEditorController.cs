// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets.Quantum.Visitors;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Editor.EditorGame.Game;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services
{
    using EntityHierarchyElementViewModel = Stride.Assets.Presentation.ViewModels.EntityHierarchyItemViewModel;
    using EntityHierarchyEditorViewModel = Stride.Assets.Editor.ViewModels.EntityHierarchyEditorViewModel;

    public abstract class EntityHierarchyEditorController : AssetCompositeHierarchyEditorController<EntityHierarchyEditorGame, EntityDesign, Entity, EntityHierarchyElementViewModel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityHierarchyEditorController"/> class.
        /// </summary>
        /// <param name="asset">The scene associated with this instance.</param>
        /// <param name="editor">The editor associated with this instance.</param>
        /// <param name="gameFactory">The factory to create the editor game.</param>
        protected EntityHierarchyEditorController([NotNull] AssetViewModel asset, [NotNull] EntityHierarchyEditorViewModel editor, [NotNull] EditorGameFactory<EntityHierarchyEditorGame> gameFactory)
            : base(asset, editor, gameFactory)
        {
        }

        protected new EntityHierarchyEditorViewModel Editor => (EntityHierarchyEditorViewModel)base.Editor;

        /// <inheritdoc />
        public override async Task<bool> CreateScene()
        {
            if (Game != null)
            {
                await InvokeAsync(Game.InitializeContentScene);
            }
            if (RecoveryService != null)
            {
                RecoveryService.IsActive = true;
            }
            return true;
        }

        public abstract AbsoluteId GetAbsoluteId([NotNull] Entity entity);

        /// <inheritdoc/>
        protected override void InitializeServices(EditorGameServiceRegistry services)
        {
            base.InitializeServices(services);
        }

        /// <inheritdoc/>
        protected override Dictionary<Guid, IIdentifiable> CollectIdentifiableObjects()
        {
            if (Game?.ContentScene == null)
                return new Dictionary<Guid, IIdentifiable>();

            var allEntities = Game.ContentScene.Yield().BreadthFirst(x => x.Children).SelectMany(x => x.Entities).BreadthFirst(x => x.Transform.Children.Select(y => y.Entity));
            var definition = AssetQuantumRegistry.GetDefinition(Asset.Asset.GetType());
            var identifiableObjects = new Dictionary<Guid, IIdentifiable>();
            foreach (var entityNode in allEntities.Select(x => GameSideNodeContainer.GetOrCreateNode(x)))
            {
                foreach (var identifiable in IdentifiableObjectCollector.Collect(definition, entityNode))
                {
                    identifiableObjects[identifiable.Key] = identifiable.Value;
                }
            }
            return identifiableObjects;
        }
    }
}

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Services
{
    using EntityHierarchyElementViewModel = Stride.Assets.Presentation.ViewModels.EntityHierarchyItemViewModel;
    using EntityHierarchyEditorViewModel = Stride.Assets.Editor.ViewModels.EntityHierarchyEditorViewModel;

    public abstract class EntityHierarchyEditorController : Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services.EntityHierarchyEditorController
    {
        protected EntityHierarchyEditorController(AssetViewModel asset, EntityHierarchyEditorViewModel editor, EditorGameFactory<EntityHierarchyEditorGame> gameFactory)
            : base(asset, editor, gameFactory)
        {
        }
    }
}
