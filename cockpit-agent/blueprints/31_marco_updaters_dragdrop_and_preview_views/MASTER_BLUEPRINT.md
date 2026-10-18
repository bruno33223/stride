# 🏛️ MASTER BLUEPRINT: Marco P31 - Asset Node Updaters, Drag-and-Drop & Preview Views Infrastructure

**Marco:** P31  
**Objetivo:** Migrar e desacoplar para Linux (.NET 10 / Avalonia 12 / Vulkan RADV) os Quantum Node Updaters restantes (`VideoAssetNodeUpdater`, `TextureAssetNodeUpdater`, `MaterialAssetNodeUpdater`, `ModelAssetNodeUpdater`), comandos de seleção e picking de cena, geradores de fábricas de colisão/malhas, infraestrutura completa de Drag-and-Drop do Editor, e contratos de Preview Views 3D e interfaces de janelas.  
**Orquestrador (Tech Leader):** Antigravity Staff Engineer  
**Padrão de Execução:** Gauntlet Loop (3 Builders vs 3 Reviewers Independentes)

---

## 🎯 Fatias Verticais

### 🔹 Fatia 1 (slice-1): Asset Node Updaters, Scene Picking Commands & Asset Factories
- **Builder:** `Asset Node Updaters & Factories Builder`
- **Reviewer:** `Asset Node Updaters & Factories Reviewer`
- **Escopo Técnico (12 arquivos - ~600 l):**
  1. `sources/editor/Stride.Assets.Presentation/SceneEditor/MaterialFilterRenderFeature.cs` (55 l)
  2. `sources/editor/Stride.Assets.Presentation/NodePresenters/Commands/FetchEntityCommand.cs` (58 l)
  3. `sources/editor/Stride.Assets.Presentation/ViewModel/Commands/MoveItemCommand.cs` (56 l)
  4. `sources/editor/Stride.Assets.Presentation/NodePresenters/Updaters/VideoAssetNodeUpdater.cs` (52 l)
  5. `sources/editor/Stride.Assets.Presentation/NodePresenters/Updaters/TextureAssetNodeUpdater.cs` (52 l)
  6. `sources/editor/Stride.Assets.Presentation/NodePresenters/Updaters/MaterialAssetNodeUpdater.cs` (51 l)
  7. `sources/editor/Stride.Assets.Presentation/NodePresenters/Updaters/ModelAssetNodeUpdater.cs` (49 l)
  8. `sources/editor/Stride.Assets.Presentation/Templates/ProceduralModelFactoryTemplateGenerator.cs` (48 l)
  9. `sources/editor/Stride.Assets.Presentation/Templates/ColliderShapeStaticMeshFactoryTemplateGenerator.cs` (48 l)
  10. `sources/editor/Stride.Assets.Presentation/Templates/ColliderShapeHullFactoryTemplateGenerator.cs` (48 l)
  11. `sources/editor/Stride.Assets.Presentation/NodePresenters/Commands/PickupSceneObjectCommandBase.cs` (48 l)
  12. `sources/editor/Stride.Assets.Presentation/Templates/SkyboxFactoryTemplateGenerator.cs` (47 l)
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestAssetNodeUpdatersAndFactoriesIntegration.cs` (RED -> GREEN).

---

### 🔹 Fatia 2 (slice-2): Drag-and-Drop Infrastructure, Asset ViewModels & Extensions
- **Builder:** `DragDrop & Asset ViewModels Builder`
- **Reviewer:** `DragDrop & Asset ViewModels Reviewer`
- **Escopo Técnico (12 arquivos - ~500 l):**
  1. `sources/editor/Stride.Core.Assets.Editor/ViewModel/DeletedPartsTrackingOperation.cs` (56 l)
  2. `sources/editor/Stride.Assets.Presentation/ViewModel/CodeAssetViewModel.cs` (52 l)
  3. `sources/editor/Stride.Core.Assets.Editor/Components/AddAssets/AddAssetTemplateCollectionViewModel.cs` (49 l)
  4. `sources/editor/Stride.Core.Assets.Editor/Components/TemplateDescriptions/ViewModels/UpdatePackageTemplateCollectionViewModel.cs` (47 l)
  5. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Behaviors/DragDrop/ListBoxDragDropBehavior.cs` (42 l)
  6. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Behaviors/DragDrop/InsertAdorner.cs` (42 l)
  7. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Behaviors/DragDrop/ReferenceHostDragDropBehavior.cs` (37 l)
  8. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Behaviors/DragDrop/TreeViewStopEditOnLostFocusBehavior.cs` (29 l)
  9. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Behaviors/DragDrop/FrameworkElementDragDropBehavior.cs` (22 l)
  10. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Behaviors/DragDrop/InsertPosition.cs` (10 l)
  11. `sources/editor/Stride.Assets.Presentation/Extensions/EnumerableExtensions.cs` (61 l)
  12. `sources/editor/Stride.Core.Assets.Editor/Extensions/ViewModelExtensions.cs` (26 l)
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestDragDropAndAssetViewModelsIntegration.cs` (RED -> GREEN).

---

### 🔹 Fatia 3 (slice-3): Editor Preview Views, Window Services & Template Providers
- **Builder:** `Preview Views & Window Services Builder`
- **Reviewer:** `Preview Views & Window Services Reviewer`
- **Escopo Técnico (18 arquivos - ~450 l):**
  1. `sources/editor/Stride.Assets.Editor.Avalonia/Preview/Views/IPreviewView.cs` (23 l)
  2. `sources/editor/Stride.Assets.Editor.Avalonia/Preview/Views/ModelPreviewView.cs` (21 l)
  3. `sources/editor/Stride.Assets.Editor.Avalonia/Preview/Views/SkyboxPreviewView.cs` (18 l)
  4. `sources/editor/Stride.Assets.Editor.Avalonia/Preview/Views/TexturePreviewView.cs` (17 l)
  5. `sources/editor/Stride.Assets.Editor.Avalonia/Preview/Views/ScenePreviewView.cs` (17 l)
  6. `sources/editor/Stride.Assets.Editor.Avalonia/Preview/Views/MaterialPreviewView.cs` (17 l)
  7. `sources/editor/Stride.Assets.Editor.Avalonia/Preview/Views/HeightmapPreviewView.cs` (17 l)
  8. `sources/editor/Stride.Assets.Editor.Avalonia/Preview/Views/EntityPreviewView.cs` (17 l)
  9. `sources/editor/Stride.Assets.Editor.Avalonia/AssetEditors/UIPageEditor/Views/UIPageEditorView.cs` (14 l)
  10. `sources/editor/Stride.Assets.Editor.Avalonia/AssetEditors/UILibraryEditor/Views/UILibraryEditorView.cs` (14 l)
  11. `sources/editor/Stride.Core.Assets.Editor/Services/IEditorView.cs` (33 l)
  12. `sources/editor/Stride.Core.Assets.Editor/Services/IAssetCreationView.cs` (13 l)
  13. `sources/editor/Stride.Core.Assets.Editor/Services/IItemTemplateDialog.cs` (12 l)
  14. `sources/editor/Stride.Assets.Editor.Avalonia/Behaviors/BindActualSizeBehavior.cs` (51 l)
  15. `sources/editor/Stride.Assets.Editor.Avalonia/AssetEditors/ScriptEditor/BindScriptTextEditorWorkspaceProjectIdBehavior.cs` (51 l)
  16. `sources/editor/Stride.Core.Assets.Editor.Avalonia/TemplateProviders/TypeMatchTemplateProvider.cs` (53 l)
  17. `sources/editor/Stride.Core.Assets.Editor.Avalonia/TemplateProviders/DictionaryNumberKeyTemplateProvider.cs` (50 l)
  18. `sources/editor/Stride.Core.Assets.Editor.Avalonia/TemplateProviders/EnumTemplateProvider.cs` (47 l)
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestPreviewViewsAndWindowServicesIntegration.cs` (RED -> GREEN).

---

## 🛡️ Regras e Protocolos Rígidos
1. Respostas em **PT-BR**.
2. **Sem Gambiarras:** Código limpo, 85-100% de paridade com o legado Stride e desacoplamento 100% de WPF/Win32.
3. **Protocolo Obrigatório de Bugs / TDD Iron Law:** Cada Builder deve obrigatoriamente criar o teste integrado e comprovar a fase RED antes de qualquer implementação de código. O código só é aceito com 100% de testes GREEN.
4. **Despacho Ordenado:** Os 3 Builders são despachados simultaneamente via `invoke_subagent`. Os 3 Reviewers só serão convocados após as 3 fatias estarem entregues com testes verdes.
