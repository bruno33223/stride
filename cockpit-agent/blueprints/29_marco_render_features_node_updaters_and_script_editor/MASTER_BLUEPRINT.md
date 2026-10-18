# 🏛️ MASTER BLUEPRINT: Marco P29 - Editor Rendering Features, Node Presenter Updaters & Script Text Editing Infrastructure

**Marco:** P29  
**Objetivo:** Migrar e desacoplar para Linux (.NET 10 / Avalonia 12 / Vulkan RADV) as features de renderização do editor de cena (`WireframeRenderFeature`, `HighlightRenderFeature`, `PickingRenderFeature`, `ModelAssetHighlighter`), o grafo de renderização do Graphics Compositor e os Quantum Updaters de cena (`EntityHierarchyAssetNodeUpdater`, `GraphicsCompositorAssetNodeUpdater`, `CameraSlotNodeUpdater`), e os controles de edição de texto de script e ferramentas de depuração (`SimpleCodeTextEditor`, `ScriptTextEditor`, `EditorDebugTools`, `DebugUndoRedoViewModel`, `AssetViewModelConstructionParameters`, `ReorderCollectionItemViewModel`).  
**Orquestrador (Tech Leader):** Antigravity Staff Engineer  
**Padrão de Execução:** Gauntlet Loop (3 Builders vs 3 Reviewers Independentes)

---

## 🎯 Fatias Verticais

### 🔹 Fatia 1 (slice-1): Scene & Graphics Render Features & Highlighting Infrastructure
- **Builder:** `Scene Render Features & Highlighting Builder`
- **Reviewer:** `Scene Render Features & Highlighting Reviewer`
- **Escopo Técnico:**
  1. `sources/editor/Stride.Assets.Presentation/SceneEditor/WireframeRenderFeature.cs` (~111 l): Feature de renderização em modo wireframe para inspeção e depuração de malhas 3D no editor.
  2. `sources/editor/Stride.Assets.Presentation/SceneEditor/HighlightRenderFeature.cs` (~100 l): Feature de renderização de highlight para destacar entidades e materiais selecionados com cores customizáveis.
  3. `sources/editor/Stride.Assets.Presentation/SceneEditor/PickingRenderFeature.cs` (~80 l): Feature de renderização especializada na passagem de picking geométrico com rasterização em texture buffer.
  4. `sources/editor/Stride.Assets.Presentation/AssetEditors/AssetHighlighters/ModelAssetHighlighter.cs` (~100 l): Rastreador e destacador de dependências de modelos 3D (`ModelAsset`) com sincronização de cores no `HighlightRenderFeature`.
  5. `sources/editor/Stride.Assets.Presentation/SceneEditor/Services/IPickedEntity.cs` (~22 l): Interface de entidade selecionada via raycasting/picking no editor de cena.
  6. `sources/editor/Stride.Assets.Presentation/SceneEditor/Services/IPickedDebugger.cs` (~15 l): Interface para inspeção e depuração de objetos selecionados na viewport.
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestSceneRenderFeaturesAndHighlightingIntegration.cs` (RED -> GREEN).

---

### 🔹 Fatia 2 (slice-2): Graphics Compositor Graph & Presentation Quantum Node Updaters
- **Builder:** `Graphics Compositor & Node Updaters Builder`
- **Reviewer:** `Graphics Compositor & Node Updaters Reviewer`
- **Escopo Técnico:**
  1. `sources/editor/Stride.Assets.Presentation/AssetEditors/GraphicsCompositorEditor/Views/GraphicsCompositorGraph.cs` (~210 l): Grafo visual do editor de Graphics Compositor com manipulação de vértices, conexões e render passes.
  2. `sources/editor/Stride.Assets.Presentation/AssetEditors/GraphicsCompositorEditor/Views/GraphicsCompositorNodeVertex.cs` (~160 l): Vértice/nó de render pass com pinos de entrada e saída, slots de câmera e compositing stages.
  3. `sources/editor/Stride.Assets.Presentation/NodePresenters/Updaters/EntityHierarchyAssetNodeUpdater.cs` (~90 l): Updater Quantum especializado na hierarquia de entidades e partes da cena.
  4. `sources/editor/Stride.Assets.Presentation/NodePresenters/Updaters/GraphicsCompositorAssetNodeUpdater.cs` (~85 l): Updater Quantum para propriedades e nós do Graphics Compositor.
  5. `sources/editor/Stride.Assets.Presentation/NodePresenters/Updaters/CameraSlotNodeUpdater.cs` (~75 l): Updater Quantum para slots de câmera da cena e composição de renderização.
  6. `sources/editor/Stride.Assets.Presentation/NodePresenters/Keys/EntityHierarchyData.cs` (~20 l): Chaves e identificadores de dados da hierarquia de entidades.
  7. `sources/editor/Stride.Assets.Presentation/NodePresenters/Keys/CameraSlotData.cs` (~20 l): Chaves de metadados para slots de câmera.
  8. `sources/editor/Stride.Assets.Presentation/NodePresenters/Keys/MaterialData.cs` (~15 l): Chaves de metadados para nós de material.
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestGraphicsCompositorAndNodeUpdatersIntegration.cs` (RED -> GREEN).

---

### 🔹 Fatia 3 (slice-3): Script Text Editing Infrastructure, Debug Tools & Collection ViewModels
- **Builder:** `Script Editor & Debug Tools Builder`
- **Reviewer:** `Script Editor & Debug Tools Reviewer`
- **Escopo Técnico:**
  1. `sources/editor/Stride.Assets.Presentation/AssetEditors/ScriptEditor/SimpleCodeTextEditor.cs` (~280 l): Editor de texto de código desacoplado de Win32/WPF com realce de sintaxe básico e navegação de linha/coluna.
  2. `sources/editor/Stride.Assets.Presentation/AssetEditors/ScriptEditor/ScriptTextEditor.cs` (~155 l): Controle de edição de scripts com integração a serviços de diagnóstico e histórico de modificações.
  3. `sources/editor/Stride.Core.Assets.Editor/Services/EditorDebugTools.cs` (~105 l): Utilitários e serviços de debug do editor Stride desacoplados de WPF.
  4. `sources/editor/Stride.Core.Assets.Editor/Components/DebugTools/UndoRedo/DebugUndoRedoViewModel.cs` (~70 l): ViewModel de inspeção e depuração da pilha de Undo/Redo do editor.
  5. `sources/editor/Stride.Core.Assets.Editor/ViewModel/AssetViewModelConstructionParameters.cs` (~70 l): Parâmetros tipados para instanciação dinâmica e desacoplada de ViewModels de assets.
  6. `sources/editor/Stride.Core.Assets.Editor/ViewModel/ReorderCollectionItemViewModel.cs` (~85 l): ViewModel para reordenação de itens em coleções e listas com suporte a Undo/Redo.
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestScriptEditorAndDebugToolsIntegration.cs` (RED -> GREEN).

---

## 🛡️ Regras e Protocolos Rígidos
1. Respostas em **PT-BR**.
2. **Sem Gambiarras:** Código limpo, 85-100% de paridade com o legado Stride e desacoplamento 100% de WPF/Win32.
3. **Protocolo Obrigatório de Bugs / TDD Iron Law:** Cada Builder deve obrigatoriamente criar o teste integrado e comprovar a fase RED antes de qualquer implementação de código. O código só é aceito com 100% de testes GREEN.
4. **Despacho Ordenado:** Os 3 Builders são despachados simultaneamente via `invoke_subagent`. Os 3 Reviewers só serão convocados após as 3 fatias estarem entregues com testes verdes.
