# 🏛️ MASTER BLUEPRINT: Marco P28 - Editor Presentation Controls, View Behaviors & Scene Picking Infrastructure

**Marco:** P28  
**Objetivo:** Migrar e desacoplar para Linux (.NET 10 / Avalonia 12 / Vulkan RADV) os controles essenciais de apresentação do Editor, behaviors de interação com Viewports 3D, infraestrutura de picking/seleção de cena (`PickingSceneRenderer`), destacadores de assets e operações estruturais de hierarquia de entidades.  
**Orquestrador (Tech Leader):** Antigravity Staff Engineer  
**Padrão de Execução:** Gauntlet Loop (3 Builders vs 3 Reviewers Independentes)

---

## 🎯 Fatias Verticais

### 🔹 Fatia 1 (slice-1): Core Editor Diagnostics & Common Avalonia Controls
- **Builder:** `Editor Controls & Diagnostics Builder`
- **Reviewer:** `Editor Controls & Diagnostics Reviewer`
- **Escopo Técnico:**
  1. `sources/editor/Stride.Core.Assets.Editor/ArgumentCheck.cs` (~390 l): Verificação e asserção de pré-condições runtime de alta performance (`MethodImplOptions.AggressiveInlining`).
  2. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Controls/GridLogViewer.cs` (~250 l): Controle de apresentação de logs com filtragem reativa por nível (Debug, Verbose, Info, Warning, Error, Fatal), busca textual e binding de `ObservableList<ILogMessage>`.
  3. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Controls/EditableContentListBox.cs` (~200 l): ListBox com capacidade de edição in-place de texto ao pressionar F2 ou duplo-clique, commit no Enter e cancelamento no Escape.
  4. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Controls/DataGridEx.cs` (~110 l): Extensão do DataGrid do Avalonia para controle de seleção e manipulação de colunas.
  5. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/ToolTipHelper.cs` (~130 l): Helper anexado para controle refinado de abertura e tempo de exibição de tooltips em Avalonia.
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestEditorControlsAndDiagnosticsIntegration.cs` (RED -> GREEN).

---

### 🔹 Fatia 2 (slice-2): Viewport Behaviors, Scene Picking & Asset Highlighters
- **Builder:** `Viewport Behaviors & Picking Builder`
- **Reviewer:** `Viewport Behaviors & Picking Reviewer`
- **Escopo Técnico:**
  1. `sources/editor/Stride.Assets.Presentation/SceneEditor/PickingSceneRenderer.cs` (~245 l): Renderizador especializado em raycasting / picking geométrico para seleção e destaque de entidades em Viewport 3D.
  2. `sources/editor/Stride.Assets.Editor.Avalonia/Behaviors/ThumbLikeBehavior.cs` (~130 l): Behavior que emula operações de arraste / resize de Splitters ou nós do editor em Avalonia sem acoplamento a Win32.
  3. `sources/editor/Stride.Assets.Editor.Avalonia/Behaviors/ViewportPaneBehavior.cs` (~130 l): Behavior responsável pelo redimensionamento e docking do painel de viewport em janelas Avalonia.
  4. `sources/editor/Stride.Assets.Presentation/AssetEditors/AssetHighlighters/TextureAssetHighlighter.cs` (~130 l): Destaque e mapeamento de dependências de texturas.
  5. `sources/editor/Stride.Assets.Presentation/AssetEditors/AssetHighlighters/MaterialAssetHighlighter.cs` (~120 l): Destaque e mapeamento de dependências de materiais.
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestViewportBehaviorsAndPickingIntegration.cs` (RED -> GREEN).

---

### 🔹 Fatia 3 (slice-3): Entity Operations, Source Code Resolvers & Node Updaters
- **Builder:** `Entity Operations & Resolvers Builder`
- **Reviewer:** `Entity Operations & Resolvers Reviewer`
- **Escopo Técnico:**
  1. `sources/editor/Stride.Assets.Presentation/AssetEditors/EntityHierarchyEditor/ViewModels/EntityFolderOperation.cs` (~150 l): Operações de mover, agrupar e reorganizar pastas e entidades na árvore de cena.
  2. `sources/editor/Stride.Assets.Presentation/AssetEditors/EntityComponentSourceCodeResolver.cs` (~170 l): Resolução de tipos de script, metadados de componentes C# e mapeamento para abertura em editores externos.
  3. `sources/editor/Stride.Assets.Presentation/NodePresenters/Updaters/UIAssetNodeUpdater.cs` (~185 l): Updaters quânticos especializados para nós de interface visual (`UIAsset`, páginas e bibliotecas de UI).
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestEntityOperationsAndCodeResolversIntegration.cs` (RED -> GREEN).

---

## 🛡️ Regras e Protocolos Rígidos
1. Respostas em **PT-BR**.
2. **Sem Gambiarras:** Código limpo, 85-100% de paridade com o legado Stride e desacoplamento 100% de WPF/Win32.
3. **Protocolo Obrigatório de Bugs / TDD Iron Law:** Cada Builder deve obrigatoriamente criar o teste integrado e comprovar a fase RED antes de qualquer implementação de código. O código só é aceito com 100% de testes GREEN.
4. **Despacho Ordenado:** Os 3 Builders são despachados simultaneamente via `invoke_subagent`. Os 3 Reviewers só serão convocados após as 3 fatias estarem entregues com testes verdes.
