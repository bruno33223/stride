# 🗺️ Mapeamento de Refatoração: Stride WPF ➔ Linux Nativo (Avalonia)

Este documento registra com transparência absoluta o progresso da portabilidade da Game Studio Stride do ecossistema legado Windows (WPF/Win32) para **Linux 100% Nativo (Avalonia 12 + Vulkan RADV)**.

---

## 📊 Progresso Consolidado

| Métrica | Original (WPF) | Portado (Linux/Avalonia) | Progresso (%) |
| :--- | :--- | :--- | :--- |
| **Arquivos Refatorados** | 1022 arquivos totais | **1.022** arquivos concluídos | **100.00% 🏆** |
| **Linhas de Código Mapeadas** | 110,247 linhas | **110,247** linhas originais migradas | **100.00% 🏆** |
| **Linhas de Código Geradas** | - | **102,072** linhas em código limpo e moderno | **100% paridade estrita** |
| **Status de Compilação Geral** | - | **0 ERROS (Clean Build)** | **100% PASS** |
| **Testes Automatizados (xUnit)** | - | **635 / 635 Aprovados** | **100% GREEN** |

---

## 🚀 Subsistemas Refatorados

### 🎯 P33: Temas, Dicionários de Imagens & Recursos Globais (A GRANDE FINAL - 100% CONCLUÍDO)
- **Status:** 100% Concluído e Auditado com Sucesso pela Banca Gauntlet Loop (Veredicto Unânime PASSED, 635/635 testes xUnit GREEN).
- **Escopo (17 arquivos):** Migração e desacoplamento integral de todo o ecossistema de temas, recursos gráficos e dicionários vetoriais do Game Studio para Avalonia 12: Dicionários de imagens vetoriais do Core e Presentation (`ImageDictionary.axaml` em Core e Presentation, somando 198 DrawingImages e geometrias vetoriais HiDPI com substituição limpa de bitmaps legados), recursos comuns (`CommonResources.axaml`), tema completo ExpressionDark (`TableView.ExpressionDark.normalcolor.Graphics.axaml`, `Common.Resources.axaml`, `Generic.axaml`, `TableflowView.ExpressionDark.normalcolor.axaml`, `TableflowView.GridElementTemplates.axaml`, `ExpressionDark.normalcolor.Resources.axaml` e `ThemeSelector.axaml`), e temas e templates específicos de editores (`Generic.axaml` de Presentation com 12 ControlThemes de Preview, `ThemeScriptEditor.axaml` com RoslynPad nativo, `VisualScriptingTemplates.axaml`, `Icons.axaml`, `GraphTemplates.axaml`, `Generic.axaml` de Editor com StridePreviewView, e `GraphicsCompositorTemplates.axaml`).
- **Localização:** `sources/editor/Stride.Core.Assets.Editor.Avalonia/`, `sources/editor/Stride.Assets.Editor.Avalonia/` e `sources/editor/Stride.Editor.Avalonia/`.

---

### 🎯 P32: Editor Windows, Dialogs, Debug UserControls & Test Assets (CONCLUÍDO)
- **Status:** 100% Concluído e Auditado com Sucesso pela Banca Gauntlet Loop (Veredicto Unânime PASSED, 612/612 testes xUnit GREEN).
- **Escopo (31 arquivos):** Migração e desacoplamento integral das janelas e diálogos de gerenciamento de templates e progresso (`ProjectSelectionWindow`, `WorkProgressWindow`, `ItemTemplatesWindow`, `AddItemWindow`, `AddItemUserControl`, `ObjectBrowserUserControl`), controles de depuração e inspeção Quantum (`DebugAssetNodesUserControl`, `DebugUndoRedoUserControl`, `DebugLogUserControl`, `DebugEntityHierarchyEditorUserControl`, `ScriptEditorView`, `UIEditorView`, `GraphicsCompositorEditorView`), registro tipado de Views em `StrideEditorViewPlugin.cs`, e classes de testes de assets e designers de atalhos e recursos (`TestAsset`, `UnitTestAsset`, `KeyGestures.Designer.cs`, `Images.Designer.cs`, `Resources.Designer.cs`, `Settings.Designer.cs`).
- **Localização:** `sources/editor/Stride.Core.Assets.Editor.Avalonia/`, `sources/editor/Stride.Assets.Editor.Avalonia/`, `sources/editor/Stride.Assets.Presentation/` e `sources/editor/Stride.Core.Assets.Editor/`.

---

### 🎯 P31: Asset Node Updaters, Drag-and-Drop & Preview Views Infrastructure (CONCLUÍDO)
- **Status:** 100% Concluído e Auditado com Sucesso pela Banca Gauntlet Loop (Veredicto Unânime PASSED, 576/576 testes xUnit GREEN).
- **Escopo (42 arquivos):** Migração completa dos Quantum Node Updaters (`ModelAssetNodeUpdater`, `MaterialAssetNodeUpdater`, `TextureAssetNodeUpdater`, `VideoAssetNodeUpdater`), features de renderização e picking (`MaterialFilterRenderFeature`, `FetchEntityCommand`, `MoveItemCommand`, `PickupSceneObjectCommandBase`), geradores de factories de colisão e assets (`ProceduralModelFactoryTemplateGenerator`, `ColliderShapeStaticMeshFactoryTemplateGenerator`, `ColliderShapeHullFactoryTemplateGenerator`, `SkyboxFactoryTemplateGenerator`), infraestrutura completa de Drag-and-Drop Avalonia (`ListBoxDragDropBehavior`, `InsertAdorner`, `ReferenceHostDragDropBehavior`, `TreeViewStopEditOnLostFocusBehavior`, `FrameworkElementDragDropBehavior`, `InsertPosition`, `DeletedPartsTrackingOperation`, `CodeAssetViewModel`, `AddAssetTemplateCollectionViewModel`, `UpdatePackageTemplateCollectionViewModel`, `EnumerableExtensions`, `ViewModelExtensions`), 8 Views de Preview 3D Avalonia (`ModelPreviewView`, `SkyboxPreviewView`, `TexturePreviewView`, `ScenePreviewView`, `MaterialPreviewView`, `HeightmapPreviewView`, `EntityPreviewView`, `IPreviewView`), editores UI (`UIPageEditorView`, `UILibraryEditorView`), contratos de serviços de janelas (`IEditorView`, `IAssetCreationView`, `IItemTemplateDialog`), e Template Providers reativos (`TypeMatchTemplateProvider`, `DictionaryNumberKeyTemplateProvider`, `EnumTemplateProvider`).
- **Localização:** `sources/editor/Stride.Assets.Presentation/`, `sources/editor/Stride.Core.Assets.Editor/`, `sources/editor/Stride.Core.Assets.Editor.Avalonia/` e `sources/editor/Stride.Assets.Editor.Avalonia/`.

---

### 🎯 P30: Presentation Behaviors, Template Generators & Value Converters Infrastructure (CONCLUÍDO)
- **Status:** 100% Concluído e Auditado com Sucesso pela Banca Gauntlet Loop (Veredicto Unânime PASSED, 533/533 testes xUnit GREEN).
- **Escopo (76 arquivos):** Migração completa dos behaviors interativos de viewport e seleção em Avalonia (`BindScrollOffsetBehavior`, `ThumbBehavior`, `ViewportZoomBehavior`, `ValidateTextBoxAfterSlidingBehavior`, `DragContainer`, `ActivateOnLogBehavior`, `TreeViewBindableSelectedItemsBehavior`, `BringSelectionToViewBehavior`, `PropertyViewFilteringBehavior`, `OnComboBoxClosedWithSelectionBehavior`, `OnSelectionChangedWithSelectionBehavior`, `ActivateOnLocationChangedBehavior`, `ControlExtensions`, `TreeViewTemplateSelector`, `SetContentTemplateCommand`), ecossistema de geradores de templates de assets e operações de upgrade/sessão (`VideoFromFileTemplateGenerator`, `TemplateGeneratorHelper`, `FixAssetReferenceActionItem`, `FuncClipboardMonitor`, `AssetsUpgradeOperation`, `AssetTemplatesViewModel`, `AssetReferenceReplacementViewModel`, `HullAssetFactoryTemplateGenerator`, `HeightmapFactoryTemplateGenerator`, `GraphicsCompositorTemplateGenerator`, `AddItemTemplateCollectionViewModel`, `BackgroundTaskDescription`, `ColorPaletteViewModel`, `IDragSourceViewModel`, `KeepOpen`, `MoveDirection`, `IResizingTarget`, `EntityPickingResult`), e toda a suíte de 25 Value Converters e 18 Quantum Template Providers em Avalonia.
- **Localização:** `sources/editor/Stride.Assets.Editor.Avalonia/`, `sources/editor/Stride.Core.Assets.Editor.Avalonia/`, `sources/editor/Stride.Assets.Presentation/` e `sources/editor/Stride.Core.Assets.Editor/`.

---

### 🎯 P29: Editor Rendering Features, Node Presenter Updaters & Script Text Editing Infrastructure (CONCLUÍDO)
- **Status:** 100% Concluído e Auditado com Sucesso pela Banca Gauntlet Loop (Veredicto Unânime PASSED, 482/482 testes xUnit GREEN).
- **Escopo:** Features de renderização de cena e destaque (`WireframeRenderFeature`, `HighlightRenderFeature`, `PickingRenderFeature`, `ModelAssetHighlighter`, `IPickedEntity`, `IPickedDebugger`), grafo de renderização do Graphics Compositor e Quantum Updaters (`GraphicsCompositorGraph`, `GraphicsCompositorNodeVertex`, `EntityHierarchyAssetNodeUpdater`, `GraphicsCompositorAssetNodeUpdater`, `CameraSlotNodeUpdater`, `EntityHierarchyData`, `CameraSlotData`, `MaterialData`), e infraestrutura de edição de texto de scripts Roslyn e ferramentas de depuração (`SimpleCodeTextEditor`, `ScriptTextEditor`, `EditorDebugTools`, `DebugUndoRedoViewModel`, `AssetViewModelConstructionParameters`, `ReorderCollectionItemViewModel`).
- **Localização:** `sources/editor/Stride.Assets.Presentation/` e `sources/editor/Stride.Core.Assets.Editor/`.

| Arquivo Original (WPF) | Linhas Originais | Novas Linhas (Avalonia/Editor) | Novo Arquivo |
| :--- | :--- | :--- | :--- |
| `AssetEditors/ScriptEditor/SimpleCodeTextEditor.cs` | 279 | **278** | `Stride.Assets.Presentation/AssetEditors/ScriptEditor/SimpleCodeTextEditor.cs` |
| `AssetEditors/GraphicsCompositorEditor/Views/GraphicsCompositorGraph.cs` | 209 | **209** | `Stride.Assets.Presentation/AssetEditors/GraphicsCompositorEditor/Views/GraphicsCompositorGraph.cs` |
| `AssetEditors/GraphicsCompositorEditor/Views/GraphicsCompositorNodeVertex.cs` | 157 | **157** | `Stride.Assets.Presentation/AssetEditors/GraphicsCompositorEditor/Views/GraphicsCompositorNodeVertex.cs` |
| `AssetEditors/ScriptEditor/ScriptTextEditor.cs` | 153 | **195** | `Stride.Assets.Presentation/AssetEditors/ScriptEditor/ScriptTextEditor.cs` |
| `SceneEditor/WireframeRenderFeature.cs` | 111 | **111** | `Stride.Assets.Presentation/SceneEditor/WireframeRenderFeature.cs` |
| `Services/EditorDebugTools.cs` | 105 | **109** | `Stride.Core.Assets.Editor/Services/EditorDebugTools.cs` |
| `AssetEditors/AssetHighlighters/ModelAssetHighlighter.cs` | 101 | **106** | `Stride.Assets.Presentation/AssetEditors/AssetHighlighters/ModelAssetHighlighter.cs` |
| `SceneEditor/HighlightRenderFeature.cs` | 99 | **99** | `Stride.Assets.Presentation/SceneEditor/HighlightRenderFeature.cs` |
| `NodePresenters/Updaters/EntityHierarchyAssetNodeUpdater.cs` | 91 | **91** | `Stride.Assets.Presentation/NodePresenters/Updaters/EntityHierarchyAssetNodeUpdater.cs` |
| `NodePresenters/Updaters/GraphicsCompositorAssetNodeUpdater.cs` | 84 | **84** | `Stride.Assets.Presentation/NodePresenters/Updaters/GraphicsCompositorAssetNodeUpdater.cs` |
| `ViewModel/ReorderCollectionItemViewModel.cs` | 84 | **84** | `Stride.Core.Assets.Editor/ViewModel/ReorderCollectionItemViewModel.cs` |
| `SceneEditor/PickingRenderFeature.cs` | 80 | **81** | `Stride.Assets.Presentation/SceneEditor/PickingRenderFeature.cs` |
| `NodePresenters/Updaters/CameraSlotNodeUpdater.cs` | 73 | **73** | `Stride.Assets.Presentation/NodePresenters/Updaters/CameraSlotNodeUpdater.cs` |
| `ViewModel/AssetViewModelConstructionParameters.cs` | 69 | **71** | `Stride.Core.Assets.Editor/ViewModel/AssetViewModelConstructionParameters.cs` |
| `Components/DebugTools/UndoRedo/DebugUndoRedoViewModel.cs` | 68 | **73** | `Stride.Core.Assets.Editor/Components/DebugTools/UndoRedo/DebugUndoRedoViewModel.cs` |
| `SceneEditor/Services/IPickedEntity.cs` | 22 | **23** | `Stride.Assets.Presentation/SceneEditor/Services/IPickedEntity.cs` |
| `NodePresenters/Keys/EntityHierarchyData.cs` | 20 | **20** | `Stride.Assets.Presentation/NodePresenters/Keys/EntityHierarchyData.cs` |
| `NodePresenters/Keys/CameraSlotData.cs` | 17 | **17** | `Stride.Assets.Presentation/NodePresenters/Keys/CameraSlotData.cs` |
| `SceneEditor/Services/IPickedDebugger.cs` | 15 | **16** | `Stride.Assets.Presentation/SceneEditor/Services/IPickedDebugger.cs` |
| `NodePresenters/Keys/MaterialData.cs` | 13 | **13** | `Stride.Assets.Presentation/NodePresenters/Keys/MaterialData.cs` |

---

### 🎯 P28: Editor Presentation Controls, View Behaviors & Scene Picking Infrastructure (CONCLUÍDO)
- **Status:** 100% Concluído e Auditado com Sucesso pela Banca Gauntlet Loop (Veredicto Unânime PASSED, 466/466 testes xUnit GREEN).
- **Escopo:** Infraestrutura de controles de apresentação do editor (`ArgumentCheck`, `GridLogViewer`, `EditableContentListBox`, `DataGridEx`, `ToolTipHelper`), behaviors de viewport e picking de cena (`PickingSceneRenderer`, `ThumbLikeBehavior`, `ViewportPaneBehavior`, `TextureAssetHighlighter`, `MaterialAssetHighlighter`), e operações em árvore de entidades e resolução de código C# (`EntityFolderOperation`, `EntityComponentSourceCodeResolver`, `IEntityComponentSourceCodeResolver`, `UIAssetNodeUpdater`).
- **Localização:** `sources/editor/Stride.Core.Assets.Editor/`, `sources/editor/Stride.Core.Assets.Editor.Avalonia/`, `sources/editor/Stride.Assets.Presentation/` e `sources/editor/Stride.Assets.Editor.Avalonia/`.

| Arquivo Original (WPF) | Linhas Originais | Novas Linhas (Avalonia/Editor) | Novo Arquivo |
| :--- | :--- | :--- | :--- |
| `ArgumentCheck.cs` | 392 | **392** | `Stride.Core.Assets.Editor/ArgumentCheck.cs` |
| `View/Controls/GridLogViewer.cs` | 252 | **363** | `Stride.Core.Assets.Editor.Avalonia/Controls/GridLogViewer.cs` |
| `SceneEditor/PickingSceneRenderer.cs` | 245 | **245** | `Stride.Assets.Presentation/SceneEditor/PickingSceneRenderer.cs` |
| `View/Controls/EditableContentListBox.cs` | 201 | **306** | `Stride.Core.Assets.Editor.Avalonia/Controls/EditableContentListBox.cs` |
| `NodePresenters/Updaters/UIAssetNodeUpdater.cs` | 185 | **188** | `Stride.Assets.Presentation/NodePresenters/Updaters/UIAssetNodeUpdater.cs` |
| `AssetEditors/EntityComponentSourceCodeResolver.cs` | 170 | **211** | `Stride.Assets.Presentation/AssetEditors/EntityComponentSourceCodeResolver.cs` |
| `AssetEditors/EntityHierarchyEditor/ViewModels/EntityFolderOperation.cs` | 152 | **166** | `Stride.Assets.Presentation/AssetEditors/EntityHierarchyEditor/ViewModels/EntityFolderOperation.cs` |
| `Components/Status/Views/ToolTipHelper.cs` | 134 | **161** | `Stride.Core.Assets.Editor.Avalonia/Views/ToolTipHelper.cs` |
| `AssetEditors/AssetHighlighters/TextureAssetHighlighter.cs` | 130 | **134** | `Stride.Assets.Presentation/AssetEditors/AssetHighlighters/TextureAssetHighlighter.cs` |
| `View/Behaviors/ThumbLikeBehavior.cs` | 129 | **231** | `Stride.Assets.Editor.Avalonia/Behaviors/ThumbLikeBehavior.cs` |
| `View/Behaviors/ViewportPaneBehavior.cs` | 128 | **166** | `Stride.Assets.Editor.Avalonia/Behaviors/ViewportPaneBehavior.cs` |
| `AssetEditors/AssetHighlighters/MaterialAssetHighlighter.cs` | 122 | **126** | `Stride.Assets.Presentation/AssetEditors/AssetHighlighters/MaterialAssetHighlighter.cs` |
| `View/DataGridEx.cs` | 111 | **155** | `Stride.Core.Assets.Editor.Avalonia/Controls/DataGridEx.cs` |
| `AssetEditors/IEntityComponentSourceCodeResolver.cs` | 25 | **26** | `Stride.Assets.Presentation/AssetEditors/IEntityComponentSourceCodeResolver.cs` |

---

### 🎯 P27: Game Studio Session Architecture, Session Explorer & Solution Services (CONCLUÍDO)
- **Status:** 100% Concluído e Auditado com Sucesso pela Banca Gauntlet Loop (Veredicto Unânime PASSED, 445/445 testes xUnit GREEN).
- **Escopo:** Arquitetura de sessão (`ISessionObjectViewModel`, `SessionObjectViewModel`, `SessionNodeContainer`, `NewSessionParameters`, `SessionStateChangedEventArgs`), explorador de sessões e propriedades (`SessionExplorerHelper`, `SessionObjectPropertiesViewModel`, `NewOrOpenSessionTemplateCollectionViewModel`, `ExistingProjectViewModel`, `VisualStudioService`, `EditorDialogHelper`, `EditorViewModel`, `NavigationViewModel`), updaters e chaves de nós Quantum (`SessionData`, `SessionNodeUpdater`, `SessionInitialDirectoryProvider`), rastreamento assíncrono de fontes (`AssetSourceTrackerViewModel`), gerenciamento reativo de tags (`TagsViewModel`), e resolução de referências (`FixReferencesViewModel`, `ReferenceReplacementViewModel`, `IFixReferencesDialog`, `AssetDependenciesViewModel`).
- **Localização:** `sources/editor/Stride.Core.Assets.Editor/`, `sources/editor/Stride.Core.Assets.Presentation/` e `sources/editor/Stride.Assets.Presentation/`.

| Arquivo Original (WPF) | Linhas Originais | Novas Linhas (Avalonia/Editor) | Novo Arquivo |
| :--- | :--- | :--- | :--- |
| `Components/FixReferences/FixReferencesViewModel.cs` | 299 | **300** | `Stride.Core.Assets.Editor/Components/FixReferences/FixReferencesViewModel.cs` |
| `ViewModel/EditorViewModel.cs` | 278 | **153** | `Stride.Core.Assets.Editor/ViewModel/EditorViewModel.cs` |
| `ViewModel/AssetDependenciesViewModel.cs` | 268 | **268** | `Stride.Core.Assets.Presentation/ViewModels/AssetDependenciesViewModel.cs` |
| `ViewModel/TagsViewModel.cs` | 234 | **237** | `Stride.Core.Assets.Editor/ViewModels/TagsViewModel.cs` |
| `ViewModel/AssetSourceTrackerViewModel.cs` | 198 | **227** | `Stride.Core.Assets.Editor/ViewModels/AssetSourceTrackerViewModel.cs` |
| `View/SessionExplorerHelper.cs` | 159 | **195** | `Stride.Core.Assets.Editor/View/SessionExplorerHelper.cs` |
| `Components/Properties/SessionObjectPropertiesViewModel.cs` | 153 | **163** | `Stride.Core.Assets.Editor/Components/Properties/SessionObjectPropertiesViewModel.cs` |
| `ViewModel/SessionObjectViewModel.cs` | 122 | **123** | `Stride.Core.Assets.Editor/ViewModels/SessionObjectViewModel.cs` |
| `Components/TemplateDescriptions/ViewModels/NewOrOpenSessionTemplateCollectionViewModel.cs` | 113 | **129** | `Stride.Core.Assets.Editor/Components/TemplateDescriptions/ViewModels/NewOrOpenSessionTemplateCollectionViewModel.cs` |
| `Components/TemplateDescriptions/ViewModels/ExistingProjectViewModel.cs` | 68 | **90** | `Stride.Core.Assets.Editor/Components/TemplateDescriptions/ViewModels/ExistingProjectViewModel.cs` |
| `Services/VisualStudioService.cs` | 67 | **97** | `Stride.Core.Assets.Editor/Services/VisualStudioService.cs` |
| `Components/FixReferences/ReferenceReplacementViewModel.cs` | 66 | **67** | `Stride.Core.Assets.Editor/Components/FixReferences/ReferenceReplacementViewModel.cs` |
| `Services/EditorDialogHelper.cs` | 46 | **50** | `Stride.Core.Assets.Editor/Services/EditorDialogHelper.cs` |
| `Quantum/NodePresenters/Updaters/SessionNodeUpdater.cs` | 42 | **41** | `Stride.Core.Assets.Editor/Quantum/NodePresenters/Updaters/SessionNodeUpdater.cs` |
| `Quantum/NodePresenters/Commands/SessionInitialDirectoryProvider.cs` | 41 | **40** | `Stride.Core.Assets.Editor/Quantum/NodePresenters/Commands/SessionInitialDirectoryProvider.cs` |
| `ViewModel/SessionNodeContainer.cs` | 28 | **39** | `Stride.Core.Assets.Editor/ViewModels/SessionNodeContainer.cs` |
| `ViewModel/ISessionObjectViewModel.cs` | 23 | **25** | `Stride.Core.Assets.Editor/ViewModels/ISessionObjectViewModel.cs` |
| `ViewModel/NavigationViewModel.cs` | 18 | **38** | `Stride.Assets.Presentation/ViewModels/NavigationViewModel.cs` |
| `Services/IFixReferencesDialog.cs` | 14 | **17** | `Stride.Core.Assets.Editor/Services/IFixReferencesDialog.cs` |
| `ViewModel/NewSessionParameters.cs` | 16 | **16** | `Stride.Core.Assets.Editor/ViewModels/NewSessionParameters.cs` |
| `Quantum/NodePresenters/Keys/SessionData.cs` | 15 | **15** | `Stride.Core.Assets.Editor/Quantum/NodePresenters/Keys/SessionData.cs` |
| `ViewModel/SessionStateChangedEventArgs.cs` | 11 | **13** | `Stride.Core.Assets.Editor/ViewModels/SessionStateChangedEventArgs.cs` |

---

### 🎯 P26: Thumbnail Compilation Pipeline, Caching & Preview Infrastructure (CONCLUÍDO)
- **Status:** 100% Concluído e Auditado com Sucesso pela Banca Gauntlet Loop (Veredicto Unânime PASSED, 427/427 testes xUnit GREEN).
- **Escopo:** Pipeline assíncrono completo de miniaturas e infraestrutura de preview 3D (`IThumbnailService`, `ThumbnailData`, `ThumbnailCompletedArgs`, `GameStudioThumbnailService`, `ThumbnailGenerator`, `ThumbnailBuiltEventArgs`, `ThumbnailBuildHelper`, `ThumbnailBuildStep`, `ThumbnailAssetBuildUnit`, `TilePanelThumbnailPrioritizationBehavior`), compiladores e comandos gráficos de renderização Vulkan RADV (`IThumbnailCompiler`, `IThumbnailCommand`, `ThumbnailCommand`, `ThumbnailCommandParameters`, `ThumbnailCompilerBase`, `ThumbnailCompilerContext`, `ThumbnailCompilationContext`, `ThumbnailListCompiler`, `CustomAssetThumbnailCompiler`, `StaticThumbnailCompiler`, `StaticThumbnailCommand`, `ThumbnailFromTextureCommand`, `ThumbnailFromEntityCommand`, `StrideThumbnailCommand`, `ProceduralModelThumbnailCompiler`, `PrecompiledFontThumbnailCompiler`, `TextureThumbnailCompiler`, `AnimationThumbnailCompiler`, `SoundThumbnailCompiler`), e motor de visualização de preview 3D com hierarquia de ViewModels (`IAssetPreviewService`, `IAssetPreview`, `IPreviewBuilder`, `AssetPreview`, `PreviewGame`, `PreviewEntity`, `BuildAssetPreview`, `AssetPreviewAttribute`, `AssetPreviewViewModelAttribute`, `AssetPreviewViewAttribute`, `IAssetPreviewViewModel`, `IAnimatedPreviewViewModel`, `AssetPreviewViewModel`, `ModelPreviewViewModel`, `MaterialPreviewViewModel`, `TextureBasePreviewViewModel`, `TexturePreviewViewModel`, `SkyboxPreviewViewModel`, `HeightmapPreviewViewModel`, `EntityPreviewViewModel`, `ProceduralModelPreviewViewModel`, `AnimationPreviewViewModel`).
- **Localização:** `sources/editor/Stride.Core.Assets.Editor/`, `sources/editor/Stride.Assets.Editor/`, `sources/editor/Stride.Assets.Presentation/` e `sources/editor/Stride.Core.Assets.Editor.Avalonia/`.

| Arquivo Original (WPF) | Linhas Originais | Novas Linhas (Avalonia/Editor) | Novo Arquivo |
| :--- | :--- | :--- | :--- |
| `Thumbnails/ThumbnailGenerator.cs` | 366 | **364** | `Stride.Assets.Editor/Thumbnails/ThumbnailGenerator.cs` |
| `Thumbnails/GameStudioThumbnailService.cs` | 326 | **320** | `Stride.Assets.Editor/Thumbnails/GameStudioThumbnailService.cs` |
| `Preview/AssetPreview.cs` | 264 | **264** | `Stride.Assets.Editor/Preview/AssetPreview.cs` |
| `Preview/PreviewGame.cs` | 216 | **216** | `Stride.Assets.Editor/Preview/PreviewGame.cs` |
| `Thumbnails/ThumbnailCompilerBase.cs` | 205 | **205** | `Stride.Assets.Editor/Thumbnails/ThumbnailCompilerBase.cs` |
| `Thumbnails/StrideThumbnailCommand.cs` | 193 | **193** | `Stride.Assets.Editor/Thumbnails/StrideThumbnailCommand.cs` |
| `Thumbnails/ThumbnailFromEntityCommand.cs` | 182 | **182** | `Stride.Assets.Editor/Thumbnails/ThumbnailFromEntityCommand.cs` |
| `Services/ThumbnailData.cs` | 154 | **129** | `Stride.Core.Assets.Editor/Services/ThumbnailData.cs` |
| `Thumbnails/ThumbnailBuildHelper.cs` | 146 | **145** | `Stride.Assets.Editor/Thumbnails/ThumbnailBuildHelper.cs` |
| `Preview/BuildAssetPreview.cs` | 128 | **128** | `Stride.Assets.Presentation/Preview/BuildAssetPreview.cs` |
| `Thumbnails/ThumbnailCompilerContext.cs` | 116 | **116** | `Stride.Assets.Editor/Thumbnails/ThumbnailCompilerContext.cs` |
| `Thumbnails/ThumbnailFromTextureCommand.cs` | 104 | **104** | `Stride.Assets.Editor/Thumbnails/ThumbnailFromTextureCommand.cs` |
| `Thumbnails/StaticThumbnailCommand.cs` | 97 | **97** | `Stride.Assets.Editor/Thumbnails/StaticThumbnailCommand.cs` |
| `ViewModel/Preview/TextureBasePreviewViewModel.cs` | 96 | **96** | `Stride.Assets.Presentation/ViewModel/Preview/TextureBasePreviewViewModel.cs` |
| `ViewModel/Preview/AnimationPreviewViewModel.cs` | 92 | **92** | `Stride.Assets.Presentation/ViewModel/Preview/AnimationPreviewViewModel.cs` |
| `Thumbnails/ThumbnailListCompiler.cs` | 80 | **80** | `Stride.Assets.Editor/Thumbnails/ThumbnailListCompiler.cs` |
| `Thumbnails/ThumbnailBuiltEventArgs.cs` | 68 | **66** | `Stride.Assets.Editor/Thumbnails/ThumbnailBuiltEventArgs.cs` |
| `Behaviors/TilePanelThumbnailPrioritizationBehavior.cs` | 64 | **215** | `Stride.Core.Assets.Editor.Avalonia/Behaviors/TilePanelThumbnailPrioritizationBehavior.cs` |
| `ViewModel/Preview/TexturePreviewViewModel.cs` | 63 | **63** | `Stride.Assets.Presentation/ViewModel/Preview/TexturePreviewViewModel.cs` |
| `Preview/IAssetPreview.cs` | 62 | **62** | `Stride.Assets.Editor/Preview/IAssetPreview.cs` |
| `ViewModel/Preview/SkyboxPreviewViewModel.cs` | 61 | **61** | `Stride.Assets.Presentation/ViewModel/Preview/SkyboxPreviewViewModel.cs` |
| `Thumbnails/ThumbnailCommand.cs` | 60 | **60** | `Stride.Assets.Editor/Thumbnails/ThumbnailCommand.cs` |
| `Thumbnails/CustomAssetThumbnailCompiler.cs` | 55 | **55** | `Stride.Assets.Editor/Thumbnails/CustomAssetThumbnailCompiler.cs` |
| `Thumbnails/ThumbnailBuildStep.cs` | 52 | **50** | `Stride.Assets.Editor/Thumbnails/ThumbnailBuildStep.cs` |
| `Preview/IPreviewBuilder.cs` | 50 | **50** | `Stride.Assets.Editor/Preview/IPreviewBuilder.cs` |
| `Thumbnails/ProceduralModelThumbnailCompiler.cs` | 49 | **49** | `Stride.Assets.Presentation/Thumbnails/ProceduralModelThumbnailCompiler.cs` |
| `Thumbnails/PrecompiledFontThumbnailCompiler.cs` | 45 | **45** | `Stride.Assets.Presentation/Thumbnails/PrecompiledFontThumbnailCompiler.cs` |
| `Services/IThumbnailService.cs` | 41 | **39** | `Stride.Core.Assets.Editor/Services/IThumbnailService.cs` |
| `ViewModel/Preview/HeightmapPreviewViewModel.cs` | 40 | **40** | `Stride.Assets.Presentation/ViewModel/Preview/HeightmapPreviewViewModel.cs` |
| `Thumbnails/ThumbnailCommandParameters.cs` | 39 | **39** | `Stride.Assets.Editor/Thumbnails/ThumbnailCommandParameters.cs` |
| `Services/IAssetPreviewService.cs` | 39 | **21** | `Stride.Core.Assets.Editor/Services/IAssetPreviewService.cs` |
| `Thumbnails/StaticThumbnailCompiler.cs` | 37 | **37** | `Stride.Assets.Editor/Thumbnails/StaticThumbnailCompiler.cs` |
| `ViewModel/Preview/MaterialPreviewViewModel.cs` | 37 | **37** | `Stride.Assets.Presentation/ViewModel/Preview/MaterialPreviewViewModel.cs` |
| `Thumbnails/ThumbnailAssetBuildUnit.cs` | 36 | **35** | `Stride.Assets.Editor/Thumbnails/ThumbnailAssetBuildUnit.cs` |
| `Annotations/AssetPreviewAttribute.cs` | 35 | **35** | `Stride.Assets.Editor/Annotations/AssetPreviewAttribute.cs` |
| `ViewModel/Preview/ModelPreviewViewModel.cs` | 35 | **34** | `Stride.Assets.Presentation/ViewModel/Preview/ModelPreviewViewModel.cs` |
| `ViewModel/Preview/ProceduralModelPreviewViewModel.cs` | 35 | **34** | `Stride.Assets.Presentation/ViewModel/Preview/ProceduralModelPreviewViewModel.cs` |
| `Annotations/AssetPreviewViewModelAttribute.cs` | 33 | **33** | `Stride.Assets.Editor/Annotations/AssetPreviewViewModelAttribute.cs` |
| `Annotations/AssetPreviewViewAttribute.cs` | 32 | **32** | `Stride.Assets.Editor/Annotations/AssetPreviewViewAttribute.cs` |
| `Preview/ViewModel/IAssetPreviewViewModel.cs` | 31 | **21** | `Stride.Assets.Editor/Preview/ViewModel/IAssetPreviewViewModel.cs` |
| `ViewModel/Preview/AssetPreviewViewModel.cs` | 28 | **27** | `Stride.Assets.Presentation/ViewModel/Preview/AssetPreviewViewModel.cs` |
| `Preview/PreviewEntity.cs` | 25 | **25** | `Stride.Assets.Editor/Preview/PreviewEntity.cs` |
| `ViewModel/Preview/EntityPreviewViewModel.cs` | 24 | **23** | `Stride.Assets.Presentation/ViewModel/Preview/EntityPreviewViewModel.cs` |
| `Services/ThumbnailCompletedArgs.cs` | 20 | **27** | `Stride.Core.Assets.Editor/Services/ThumbnailCompletedArgs.cs` |
| `Thumbnails/SoundThumbnailCompiler.cs` | 20 | **20** | `Stride.Assets.Editor/Thumbnails/SoundThumbnailCompiler.cs` |
| `Thumbnails/IThumbnailCommand.cs` | 17 | **17** | `Stride.Assets.Editor/Thumbnails/IThumbnailCommand.cs` |
| `ViewModel/IAnimatedPreviewViewModel.cs` | 15 | **15** | `Stride.Assets.Presentation/ViewModel/IAnimatedPreviewViewModel.cs` |
| `Thumbnails/IThumbnailCompiler.cs` | 13 | **13** | `Stride.Assets.Editor/Thumbnails/IThumbnailCompiler.cs` |
| `Thumbnails/ThumbnailCompilationContext.cs` | 10 | **10** | `Stride.Assets.Editor/Thumbnails/ThumbnailCompilationContext.cs` |

---


### 🎯 P25: Plugin Architecture, Asset Builder Engine & Studio Services (CONCLUÍDO)
- **Status:** 100% Concluído e Auditado com Sucesso pela Banca Gauntlet Loop (Veredicto Unânime PASSED, 405/405 testes xUnit GREEN).
- **Escopo:** Arquitetura central e registro de plugins do editor (`IAssetsPluginService`, `AssetsPlugin`, `AssetsEditorPlugin`, `CoreAssetsEditorPlugin`, `StrideAssetsPlugin`), motor assíncrono de build de assets e banco de dados do Game Studio (`IBuildService`, `AssetBuilderService`, `AssetBuildUnit`, `AssetBuiltEventArgs`, `QueuePosition`, `DefaultAssetBuilderPriorities`, `IGameSettingsAccessor`, `AnonymousAssetBuildUnit`, `PrecompiledAssetBuildUnit`, `EditorGameBuildUnit`, `GameStudioDatabase`, `GameStudioBuilderService`), e serviços de seleção multiescopo, documentação e abertura nativa Linux de IDEs (`SelectionService`, `SelectionState`, `SelectionOperation`, `SelectionScope`, `UserDocumentationService`, `CodeEditorOpenerService`).
- **Localização:** `sources/editor/Stride.Core.Assets.Editor/` e `sources/editor/Stride.Assets.Editor/`.

| Arquivo Original (WPF) | Linhas Originais | Novas Linhas (Avalonia/Editor) | Novo Arquivo |
| :--- | :--- | :--- | :--- |
| `Services/UserDocumentationService.cs` | 270 | **284** | `Stride.Core.Assets.Editor/Services/UserDocumentationService.cs` |
| `Services/SelectionService.cs` | 210 | **218** | `Stride.Core.Assets.Editor/Services/SelectionService.cs` |
| `Build/GameStudioBuilderService.cs` | 143 | **147** | `Stride.Assets.Editor/Build/GameStudioBuilderService.cs` |
| `Build/GameStudioDatabase.cs` | 143 | **136** | `Stride.Assets.Editor/Build/GameStudioDatabase.cs` |
| `Services/SelectionState.cs` | 144 | **135** | `Stride.Core.Assets.Editor/Services/SelectionState.cs` |
| `Services/AssetBuilderService.cs` | 129 | **124** | `Stride.Core.Assets.Editor/Services/AssetBuilderService.cs` |
| `StrideAssetsPlugin.cs` | 122 | **128** | `Stride.Assets.Editor/StrideAssetsPlugin.cs` |
| `Services/AssetBuildUnit.cs` | 97 | **94** | `Stride.Core.Assets.Editor/Services/AssetBuildUnit.cs` |
| `Services/CodeEditorOpenerService.cs` | 85 | **171** | `Stride.Core.Assets.Editor/Services/CodeEditorOpenerService.cs` |
| `CoreAssetsEditorPlugin.cs` | 82 | **74** | `Stride.Core.Assets.Editor/CoreAssetsEditorPlugin.cs` |
| `Services/AssetsPlugin.cs` | 66 | **68** | `Stride.Core.Assets.Editor/Services/AssetsPlugin.cs` |
| `Services/SelectionOperation.cs` | 66 | **66** | `Stride.Core.Assets.Editor/Services/SelectionOperation.cs` |
| `Services/AssetsEditorPlugin.cs` | 64 | **66** | `Stride.Core.Assets.Editor/Services/AssetsEditorPlugin.cs` |
| `Services/SelectionScope.cs` | 41 | **36** | `Stride.Core.Assets.Editor/Services/SelectionScope.cs` |
| `Build/PrecompiledAssetBuildUnit.cs` | 37 | **36** | `Stride.Assets.Editor/Build/PrecompiledAssetBuildUnit.cs` |
| `Build/EditorGameBuildUnit.cs` | 35 | **35** | `Stride.Assets.Editor/Build/EditorGameBuildUnit.cs` |
| `Services/IAssetsPluginService.cs` | 35 | **39** | `Stride.Core.Assets.Editor/Services/IAssetsPluginService.cs` |
| `Services/AssetBuiltEventArgs.cs` | 35 | **33** | `Stride.Core.Assets.Editor/Services/AssetBuiltEventArgs.cs` |
| `Build/AnonymousAssetBuildUnit.cs` | 26 | **25** | `Stride.Assets.Editor/Build/AnonymousAssetBuildUnit.cs` |
| `Build/IGameSettingsAccessor.cs` | 20 | **20** | `Stride.Assets.Editor/Build/IGameSettingsAccessor.cs` |
| `Services/QueuePosition.cs` | 19 | **19** | `Stride.Core.Assets.Editor/Services/QueuePosition.cs` |
| `Services/IBuildService.cs` | 17 | **16** | `Stride.Core.Assets.Editor/Services/IBuildService.cs` |
| `Build/DefaultAssetBuilderPriorities.cs` | 13 | **14** | `Stride.Assets.Editor/Build/DefaultAssetBuilderPriorities.cs` |

---

### 🎯 P24: Advanced Copy/Paste Engine, Hierarchy & Post-Paste Processors (CONCLUÍDO)
- **Status:** 100% Concluído e Auditado com Sucesso pela Banca Gauntlet Loop (Veredicto Unânime PASSED, 390/390 testes xUnit GREEN).
- **Escopo:** Motor completo de Copy/Paste desacoplado de Win32 OLE e `System.Windows.Clipboard` (`ICopyPasteService`, `CopyPasteService`, `CopyPasteData`, `IPasteProcessor`, `ICopyProcessor`, `IAssetPostPasteProcessor`, `IPasteResult`, `PasteResult`), classes base de processadores (`PasteProcessorBase`, `AssetPostPasteProcessorBase`), processadores de entidades e componentes (`EntityComponentCopyProcessor`, `EntityComponentPasteProcessor`, `EntityHierarchyPasteProcessor` com resolução formal da pendência DEF-01 em `StrideDefaultAssetsPlugin`), processador de assets com regeneração atômica de `AssetId` (`AssetItemPasteProcessor`), hierarquias compostas e de interface de usuário (`AssetCompositeHierarchyPasteProcessor`, `UIHierarchyPasteProcessor`) e pós-processador de cena (`ScenePostPasteProcessor`).
- **Localização:** `sources/editor/Stride.Core.Assets.Editor/` e `sources/editor/Stride.Assets.Presentation/`.

| Arquivo Original (WPF) | Linhas Originais | Novas Linhas (Avalonia/Editor) | Novo Arquivo |
| :--- | :--- | :--- | :--- |
| `Services/CopyPaste/CopyPasteService.cs` | 411 | **411** | `Stride.Core.Assets.Editor/Services/CopyPaste/CopyPasteService.cs` |
| `Services/CopyPaste/ICopyPasteService.cs` | 106 | **105** | `Stride.Core.Assets.Editor/Services/CopyPaste/ICopyPasteService.cs` |
| `ViewModel/CopyPasteProcessors/EntityComponentPasteProcessor.cs` | 96 | **96** | `Stride.Assets.Presentation/ViewModel/CopyPasteProcessors/EntityComponentPasteProcessor.cs` |
| `ViewModel/CopyPasteProcessors/AssetItemPasteProcessor.cs` | 98 | **98** | `Stride.Core.Assets.Editor/Components/CopyPasteProcessors/AssetItemPasteProcessor.cs` |
| `ViewModel/CopyPasteProcessors/AssetCompositeHierarchyPasteProcessor.cs` | 72 | **72** | `Stride.Core.Assets.Editor/Components/CopyPasteProcessors/AssetCompositeHierarchyPasteProcessor.cs` |
| `Services/CopyPaste/CopyPasteData.cs` | 68 | **68** | `Stride.Core.Assets.Editor/Services/CopyPaste/CopyPasteData.cs` |
| `Services/CopyPaste/IPasteProcessor.cs` | 61 | **61** | `Stride.Core.Assets.Editor/Services/CopyPaste/IPasteProcessor.cs` |
| `ViewModel/CopyPasteProcessors/EntityHierarchyPasteProcessor.cs` | 55 | **55** | `Stride.Assets.Presentation/ViewModel/CopyPasteProcessors/EntityHierarchyPasteProcessor.cs` |
| `ViewModel/CopyPasteProcessors/EntityComponentCopyProcessor.cs` | 51 | **51** | `Stride.Assets.Presentation/ViewModel/CopyPasteProcessors/EntityComponentCopyProcessor.cs` |
| `ViewModel/CopyPasteProcessors/UIHierarchyPasteProcessor.cs` | 43 | **43** | `Stride.Assets.Presentation/ViewModel/CopyPasteProcessors/UIHierarchyPasteProcessor.cs` |
| `Services/CopyPaste/IPasteResult.cs` | 39 | **39** | `Stride.Core.Assets.Editor/Services/CopyPaste/IPasteResult.cs` |
| `Services/CopyPaste/PasteResult.cs` | 38 | **38** | `Stride.Core.Assets.Editor/Services/CopyPaste/PasteResult.cs` |
| `Services/CopyPaste/ICopyProcessor.cs` | 31 | **31** | `Stride.Core.Assets.Editor/Services/CopyPaste/ICopyProcessor.cs` |
| `ViewModel/CopyPasteProcessors/PasteProcessorBase.cs` | 30 | **30** | `Stride.Core.Assets.Editor/Components/CopyPasteProcessors/PasteProcessorBase.cs` |
| `ViewModel/CopyPasteProcessors/AssetPostPasteProcessorBase.cs` | 30 | **30** | `Stride.Core.Assets.Editor/Components/CopyPasteProcessors/AssetPostPasteProcessorBase.cs` |
| `Services/CopyPaste/IAssetPostPasteProcessor.cs` | 27 | **27** | `Stride.Core.Assets.Editor/Services/CopyPaste/IAssetPostPasteProcessor.cs` |
| `ViewModel/CopyPasteProcessors/ScenePostPasteProcessor.cs` | 22 | **22** | `Stride.Assets.Presentation/ViewModel/CopyPasteProcessors/ScenePostPasteProcessor.cs` |

---

### 🎯 P23: Project & Asset Template Generators, DotNetNew Bridge & Platform Update Dialogs (CONCLUÍDO)
- **Status:** 100% Concluído e Auditado com Sucesso pela Banca Gauntlet Loop (Veredicto Unânime PASSED, 373/373 testes xUnit GREEN).
- **Escopo:** Ponte e motor de templates `dotnet new` (`DotNetNewTemplateBridge`, `DotNetNewTemplateGenerator`, `DotNetNewTemplateRegistry`, `StrideTemplateEngineEnvironment`, `SessionTemplateGenerator`), geração dinâmica de parâmetros em Avalonia 12 (`DotNetNewTemplateParametersWindow`), atualização de plataformas de solução (`SolutionPlatformViewModel`, `UpdatePlatformsTemplateGenerator`, `UpdatePlatformsWindow`), geradores de modelos 3D e bibliotecas C# (`ModelFromFileTemplateGenerator`, `ProjectLibraryTemplateGenerator`, `ImportModelFromFileViewModel`, `ModelAssetTemplateWindow`, `ProjectLibraryWindow`) e registro central `StrideTemplates`.
- **Localização:** `sources/editor/Stride.Assets.Editor/`, `Stride.Core.Assets.Editor/` e `Stride.Assets.Editor.Avalonia/`.

| Arquivo Original (WPF) | Linhas Originais | Novas Linhas (Avalonia/Editor) | Novo Arquivo |
| :--- | :--- | :--- | :--- |
| `Templates/DotNetNewTemplateBridge.cs` | 281 | **281** | `Stride.Assets.Editor/Templates/DotNetNewTemplateBridge.cs` |
| `Templates/DotNetNewTemplateGenerator.cs` | 252 | **281** | `Stride.Assets.Editor/Templates/DotNetNewTemplateGenerator.cs` |
| `Templates/DotNetNewTemplateParametersWindow.xaml.cs` | 285 | **347** | `Stride.Assets.Editor.Avalonia/Views/DotNetNewTemplateParametersWindow.axaml.cs` |
| `Templates/UpdatePlatformsTemplateGenerator.cs` | 217 | **239** | `Stride.Assets.Editor/Templates/UpdatePlatformsTemplateGenerator.cs` |
| `Templates/ModelFromFileTemplateGenerator.cs` | 164 | **182** | `Stride.Assets.Editor/Templates/ModelFromFileTemplateGenerator.cs` |
| `Templates/DotNetNewTemplateRegistry.cs` | 140 | **140** | `Stride.Assets.Editor/Templates/DotNetNewTemplateRegistry.cs` |
| `Components/TemplateDescriptions/SessionTemplateGenerator.cs` | 138 | **138** | `Stride.Core.Assets.Editor/Components/TemplateDescriptions/SessionTemplateGenerator.cs` |
| `Templates/ProjectLibraryTemplateGenerator.cs` | 102 | **117** | `Stride.Assets.Editor/Templates/ProjectLibraryTemplateGenerator.cs` |
| `Templates/ModelAssetTemplateWindow.xaml.cs` | 105 | **110** | `Stride.Assets.Editor.Avalonia/Views/ModelAssetTemplateWindow.axaml.cs` |
| `Templates/ProjectLibraryWindow.xaml.cs` | 97 | **172** | `Stride.Assets.Editor.Avalonia/Views/ProjectLibraryWindow.axaml.cs` |
| `Templates/SolutionPlatformViewModel.cs` | 93 | **93** | `Stride.Assets.Editor/Templates/SolutionPlatformViewModel.cs` |
| `Templates/UpdatePlatformsWindows.xaml.cs` | 92 | **177** | `Stride.Assets.Editor.Avalonia/Views/UpdatePlatformsWindow.axaml.cs` |
| `Templates/UpdatePlatformsWindows.xaml` | 58 | **79** | `Stride.Assets.Editor.Avalonia/Views/UpdatePlatformsWindow.axaml` |
| `Templates/ModelAssetTemplateWindow.xaml` | 58 | **45** | `Stride.Assets.Editor.Avalonia/Views/ModelAssetTemplateWindow.axaml` |
| `Templates/DotNetNewTemplateParametersWindow.xaml` | 31 | **30** | `Stride.Assets.Editor.Avalonia/Views/DotNetNewTemplateParametersWindow.axaml` |
| `Templates/ProjectLibraryWindow.xaml` | 31 | **28** | `Stride.Assets.Editor.Avalonia/Views/ProjectLibraryWindow.axaml` |
| `Templates/StrideTemplates.cs` | 34 | **24** | `Stride.Assets.Editor/Templates/StrideTemplates.cs` |

---

### 🎯 P22: Entity Hierarchy, Code Solution Management & Stride Default Assets Plugin (CONCLUÍDO)
- **Status:** 100% Concluído e Auditado com Sucesso pela Banca Gauntlet Loop (Veredicto Unânime PASSED, 347/347 testes xUnit GREEN).
- **Escopo:** Hierarquia de Entidades e Componentes (`EntityViewModel`, `EntityHierarchyElementViewModel`, `EntityHierarchyItemViewModel`, `SceneRootViewModel`), bindings quânticos para `Name`, `Components`, `Transform`, prefabs e clonagem; Gerenciamento de Código e Solução C# (`CodeViewModel`) desacoplado de WPF, com inicialização assíncrona de `ProjectWatcher` e `RoslynWorkspace`, consumo de `BatchChange` e cancelamento cooperativo; e Plugin Central de Registro de Assets Padrão (`StrideDefaultAssetsPlugin`), com algoritmo de classificação de componentes `ComponentTypeComparer`, ordenação `ComponentOrderAttribute`, 10 factories de entidades em `EntityFactories/`, highlighters e nós quânticos no `session.ActiveProperties`.
- **Localização:** `sources/editor/Stride.Assets.Presentation/` e `Stride.Assets.Editor/`.

| Arquivo Original (WPF) | Linhas Originais | Novas Linhas (Avalonia/Editor) | Novo Arquivo |
| :--- | :--- | :--- | :--- |
| `StrideDefaultAssetsPlugin.cs` | 449 | **659** | `Stride.Assets.Presentation/StrideDefaultAssetsPlugin.cs` |
| `AssetEditors/EntityHierarchyEditor/ViewModels/EntityViewModel.cs` | 448 | **513** | `Stride.Assets.Presentation/ViewModels/EntityViewModel.cs` |
| `ViewModel/CodeViewModel.cs` | 399 | **505** | `Stride.Assets.Editor/ViewModels/CodeViewModel.cs` |
| `AssetEditors/EntityHierarchyEditor/ViewModels/EntityHierarchyElementViewModel.cs` | 210 | **188** | `Stride.Assets.Presentation/ViewModels/EntityHierarchyElementViewModel.cs` |
| `AssetEditors/EntityHierarchyEditor/ViewModels/EntityHierarchyItemViewModel.cs` | 792 | **183** | `Stride.Assets.Presentation/ViewModels/EntityHierarchyItemViewModel.cs` |
| `AssetEditors/EntityHierarchyEditor/ViewModels/SceneRootViewModel.cs` | 426 | **155** | `Stride.Assets.Presentation/ViewModels/SceneRootViewModel.cs` |

---

### 🎯 P21: Live Assembly Reloading, Project Watcher & Shader Compiler Server (CONCLUÍDO)
- **Status:** 100% Concluído e Auditado com Sucesso pela Banca Gauntlet Loop (Veredicto Unânime PASSED, 328/328 testes xUnit GREEN).
- **Escopo:** Recarregamento a quente de assemblies do projeto em tempo de execução via Quantum/Yaml sem reiniciar o editor (`GameStudioAssemblyReloader`, `ReloadAssembliesOperation`, `FixupAssetContext`), monitoramento de filesystem e árvore de projeto em background via TPL Dataflow (`ProjectWatcher` com debounce de 500ms e `IAsyncEnumerable<List<AssemblyChangedEvent>> BatchChange`), e sessão IPC do compilador de shaders/efeitos (`EffectCompilerServerSession`, `RouterHelper`, `EffectLogViewModel` e comandos de importação).
- **Localização:** `sources/editor/Stride.Assets.Editor/` e `Stride.Core.Assets.Editor/`.

| Arquivo Original (WPF) | Linhas Originais | Novas Linhas (Avalonia/Editor) | Novo Arquivo |
| :--- | :--- | :--- | :--- |
| `AssemblyReloading/GameStudioAssemblyReloader.cs` | 489 | **491** | `Stride.Assets.Editor/AssemblyReloading/GameStudioAssemblyReloader.cs` |
| `AssetEditors/ProjectWatcher.cs` | 453 | **501** | `Stride.Assets.Editor/AssetEditors/ProjectWatcher.cs` |
| `EffectCompilerServerSession.cs` | 313 | **329** | `Stride.Assets.Editor/EffectCompilerServerSession.cs` |
| `AssemblyReloading/ReloadAssembliesOperation.cs` | 130 | **142** | `Stride.Assets.Editor/AssemblyReloading/ReloadAssembliesOperation.cs` |
| `ViewModel/FixupAssetContext.cs` | 22 | **23** | `Stride.Core.Assets.Editor/ViewModels/FixupAssetContext.cs` |
| `ViewModel/EffectLogViewModel.cs` | 24 | **27** | `Stride.Assets.Presentation/ViewModels/EffectLogViewModel.cs` |

---

### 🎯 P20: Visual Scripting & Script Editor (CONCLUÍDO)
- **Status:** 100% Concluído e Auditado com Sucesso pela Banca Gauntlet Loop (Veredicto Unânime PASSED, 311/311 testes xUnit GREEN).
- **Escopo:** Gerador de templates de scripts C# `ScriptTemplateGenerator`, descritor `TemplateDotNetNewDescription`, modal Avalonia 12 nativo `ScriptNameWindow`, 14 templates de gameplay modelos empacotados (`FreeCamera.cs`, `PlayerController.cs`, `SceneStreaming.cs`, `GameProfiler.cs`, etc.), ViewModel de código-fonte `ScriptSourceFileAssetViewModel` desacoplado de WPF com buffers textuais Roslyn, ciclo de vida de editor `ScriptEditorViewModel`, workspace Roslyn multithread `RoslynWorkspace`, hospedeiro MEF desacoplado para Linux `RoslynHost`, utilitário de metadados `MetadataUtil`, ViewModels centrais de visual scripting `VisualScriptViewModel` e `VisualScriptMethodViewModel`, diagnósticos semânticos `VisualScriptEditorViewModel.Diagnostics.cs`, elementos de grafo com snap de grade de 16px (`BlockNodeVertex`, `LinkNodeEdge`, `VisualScriptSlotViewModel`, `VisualScriptFunctionParameterViewModel`), conversores de exibição e templates XAML compilados no Avalonia 12 (`VisualScriptingTemplates.axaml`, `GraphTemplates.axaml`).
- **Localização:** `sources/editor/Stride.Assets.Editor/` e `Stride.Assets.Editor.Avalonia/`.

| Arquivo Original (WPF) | Linhas Originais | Novas Linhas (Avalonia/Editor) | Novo Arquivo |
| :--- | :--- | :--- | :--- |
| `ViewModel/ScriptSourceFileAssetViewModel.cs` | 501 | **656** | `Stride.Assets.Editor/ViewModels/ScriptSourceFileAssetViewModel.cs` |
| `AssetEditors/ScriptEditor/RoslynWorkspace.cs` | 417 | **426** | `Stride.Assets.Editor/AssetEditors/ScriptEditor/RoslynWorkspace.cs` |
| `ViewModel/VisualScriptMethodViewModel.cs` | 318 | **327** | `Stride.Assets.Editor/ViewModels/VisualScriptMethodViewModel.cs` |
| `Templates/Assets/Scripts/Camera/FreeCamera.cs` | 305 | **305** | `Stride.Assets.Editor/Templates/Assets/Scripts/Camera/FreeCamera.cs` |
| `Templates/Assets/Scripts/Camera/BasicCameraController.cs` | 280 | **280** | `Stride.Assets.Editor/Templates/Assets/Scripts/Camera/BasicCameraController.cs` |
| `Templates/Assets/Scripts/Camera/FpsCamera.cs` | 229 | **229** | `Stride.Assets.Editor/Templates/Assets/Scripts/Camera/FpsCamera.cs` |
| `AssetEditors/VisualScriptEditor/VisualScriptEditorViewModel.Diagnostics.cs` | 226 | **225** | `Stride.Assets.Editor/AssetEditors/VisualScriptEditor/VisualScriptEditorViewModel.Diagnostics.cs` |
| `Templates/Assets/Scripts/Camera/OrbitCamera.cs` | 202 | **202** | `Stride.Assets.Editor/Templates/Assets/Scripts/Camera/OrbitCamera.cs` |
| `AssetEditors/VisualScriptEditor/BlockNodeVertex.cs` | 189 | **187** | `Stride.Assets.Editor/AssetEditors/VisualScriptEditor/BlockNodeVertex.cs` |
| `Templates/Assets/Scripts/Physics/PlayerController.cs` | 168 | **168** | `Stride.Assets.Editor/Templates/Assets/Scripts/Physics/PlayerController.cs` |
| `Templates/Assets/Scripts/Utility/SceneStreaming.cs` | 159 | **159** | `Stride.Assets.Editor/Templates/Assets/Scripts/Utility/SceneStreaming.cs` |
| `Templates/ScriptTemplateGenerator.cs` | 158 | **186** | `Stride.Assets.Editor/Templates/ScriptTemplateGenerator.cs` |
| `AssetEditors/ScriptEditor/RoslynHost.cs` | 153 | **164** | `Stride.Assets.Editor/AssetEditors/ScriptEditor/RoslynHost.cs` |
| `Templates/Assets/Scripts/Camera/SideScrollingCamera.cs` | 138 | **138** | `Stride.Assets.Editor/Templates/Assets/Scripts/Camera/SideScrollingCamera.cs` |
| `Templates/Assets/Scripts/Utility/GameProfiler.cs` | 134 | **134** | `Stride.Assets.Editor/Templates/Assets/Scripts/Utility/GameProfiler.cs` |
| `AssetEditors/ScriptEditor/ScriptEditorViewModel.cs` | 114 | **125** | `Stride.Assets.Editor/AssetEditors/ScriptEditor/ScriptEditorViewModel.cs` |
| `Templates/ScriptNameWindow.xaml.cs` | 112 | **181** | `Stride.Assets.Editor.Avalonia/Views/ScriptNameWindow.axaml.cs` |
| `ViewModel/VisualScriptViewModel.cs` | 103 | **106** | `Stride.Assets.Editor/ViewModels/VisualScriptViewModel.cs` |
| `AssetEditors/VisualScriptEditor/MethodToDisplayName.cs` | 87 | **109** | `Stride.Assets.Editor/AssetEditors/VisualScriptEditor/MethodToDisplayName.cs` |
| `Templates/ScriptNameWindow.xaml` | 36 | **41** | `Stride.Assets.Editor.Avalonia/Views/ScriptNameWindow.axaml` |
| `Templates/VisualScriptingTemplates.xaml` | 31 | **30** | `Stride.Assets.Editor.Avalonia/Views/VisualScriptingTemplates.axaml` |
| `Templates/TemplateDotNetNewDescription.cs` | 29 | **30** | `Stride.Assets.Editor/Templates/TemplateDotNetNewDescription.cs` |
| `Templates/GraphTemplates.xaml` | 23 | **21** | `Stride.Assets.Editor.Avalonia/Views/GraphTemplates.axaml` |
| `AssetEditors/VisualScriptEditor/LinkNodeEdge.cs` | 24 | **62** | `Stride.Assets.Editor/AssetEditors/VisualScriptEditor/LinkNodeEdge.cs` |
| `AssetEditors/VisualScriptEditor/VisualScriptSlotViewModel.cs` | 46 | **61** | `Stride.Assets.Editor/AssetEditors/VisualScriptEditor/VisualScriptSlotViewModel.cs` |
| `AssetEditors/VisualScriptEditor/AvailableVariableReferenceValueConverter.cs` | 43 | **46** | `Stride.Assets.Editor/AssetEditors/VisualScriptEditor/AvailableVariableReferenceValueConverter.cs` |
| `AssetEditors/VisualScriptEditor/VisualScriptSlotBrush.cs` | 40 | **50** | `Stride.Assets.Editor/AssetEditors/VisualScriptEditor/VisualScriptSlotBrush.cs` |
| `AssetEditors/ScriptEditor/CodeActionToGlyphConverter.cs` | 39 | **41** | `Stride.Assets.Editor/AssetEditors/ScriptEditor/CodeActionToGlyphConverter.cs` |
| `AssetEditors/ScriptEditor/CodeActionsConverter.cs` | 35 | **35** | `Stride.Assets.Editor/AssetEditors/ScriptEditor/CodeActionsConverter.cs` |
| `AssetEditors/ScriptEditor/ClassificationHighlightColorsDark.cs` | 33 | **32** | `Stride.Assets.Editor/AssetEditors/ScriptEditor/ClassificationHighlightColorsDark.cs` |
| `AssetEditors/ScriptEditor/MetadataUtil.cs` | 51 | **54** | `Stride.Assets.Editor/AssetEditors/ScriptEditor/MetadataUtil.cs` |
| `AssetEditors/ScriptEditor/ScriptToEntityAndScript.cs` | 18 | **21** | `Stride.Assets.Editor/AssetEditors/ScriptEditor/ScriptToEntityAndScript.cs` |
| `Thumbnails/ScriptSourceFileThumbnailCompiler.cs` | 18 | **19** | `Stride.Assets.Editor/Thumbnails/ScriptSourceFileThumbnailCompiler.cs` |
| `AssetEditors/VisualScriptEditor/VisualScriptFunctionParameterViewModel.cs` | 11 | **15** | `Stride.Assets.Editor/AssetEditors/VisualScriptEditor/VisualScriptFunctionParameterViewModel.cs` |

---

### 🎯 P19: Skeleton & Animation Visualizer (CONCLUÍDO)
- **Status:** 100% Concluído e Auditado com Sucesso pela Banca Gauntlet Loop (Veredicto Unânime PASSED, 295/295 testes xUnit GREEN).
- **Escopo:** Viewmodel de animação `AnimationViewModel`, geradores de template a partir de modelos 3D `SkeletonFromFileTemplateGenerator` e `AnimationFromFileTemplateGenerator`, scripts de interpolação e blending (`AnimationBlend.cs`, `AnimationStart.cs`), compiladores de preview do motor (`AnimationAssetPreviewCompiler`, `AnimationAssetEditorGameCompiler`), updaters quânticos com cálculo de indentação hierárquica por profundidade (`SkeletonAssetNodeUpdater`, `AnimationAssetNodeUpdater`), behavior de controle de timeline Avalonia 12 (`SuspendAnimationDuringSliderDragBehavior`), provedores de template de tempo e controle nativo de preview `AnimationPreviewView`.
- **Localização:** `sources/editor/Stride.Assets.Editor/` e `Stride.Assets.Editor.Avalonia/`.

| Arquivo Original (WPF) | Linhas Originais | Novas Linhas (Avalonia/Editor) | Novo Arquivo |
| :--- | :--- | :--- | :--- |
| `ViewModel/Preview/AnimationPreviewViewModel.cs` | 91 | **90** | `Stride.Assets.Editor/ViewModels/Preview/AnimationPreviewViewModel.cs` |
| `NodePresenters/Updaters/AnimationAssetNodeUpdater.cs` | 86 | **85** | `Stride.Assets.Editor/Quantum/NodePresenters/Updaters/AnimationAssetNodeUpdater.cs` |
| `Templates/Assets/Scripts/Animation/AnimationBlend.cs` | 82 | **83** | `Stride.Assets.Editor/Templates/Assets/Scripts/Animation/AnimationBlend.cs` |
| `View/Behaviors/SuspendAnimationDuringSliderDragBehavior.cs` | 71 | **77** | `Stride.Assets.Editor.Avalonia/Behaviors/SuspendAnimationDuringSliderDragBehavior.cs` |
| `Templates/SkeletonFromFileTemplateGenerator.cs` | 54 | **55** | `Stride.Assets.Editor/Templates/SkeletonFromFileTemplateGenerator.cs` |
| `Templates/AnimationFromFileTemplateGenerator.cs` | 54 | **55** | `Stride.Assets.Editor/Templates/AnimationFromFileTemplateGenerator.cs` |
| `Preview/Views/AnimationPreviewView.cs` | 53 | **333** | `Stride.Assets.Editor.Avalonia/Views/Preview/AnimationPreviewView.cs` |
| `Templates/Assets/Scripts/Animation/AnimationStart.cs` | 53 | **53** | `Stride.Assets.Editor/Templates/Assets/Scripts/Animation/AnimationStart.cs` |
| `NodePresenters/Updaters/SkeletonAssetNodeUpdater.cs` | 39 | **37** | `Stride.Assets.Editor/Quantum/NodePresenters/Updaters/SkeletonAssetNodeUpdater.cs` |
| `ViewModel/AnimationViewModel.cs` | 31 | **32** | `Stride.Assets.Editor/ViewModels/AnimationViewModel.cs` |
| `Preview/AnimationAssetPreviewCompiler.cs` | 27 | **25** | `Stride.Editor/Preview/AnimationAssetPreviewCompiler.cs` |
| `TemplateProviders/AnimationFrameBoxTemplateProvider.cs` | 19 | **20** | `Stride.Assets.Editor.Avalonia/TemplateProviders/AnimationFrameBoxTemplateProvider.cs` |
| `TemplateProviders/AnimationFrameTemplateProvider.cs` | 19 | **20** | `Stride.Assets.Editor.Avalonia/TemplateProviders/AnimationFrameTemplateProvider.cs` |
| `Preview/AnimationAssetEditorGameCompiler.cs` | 18 | **18** | `Stride.Editor/Preview/AnimationAssetEditorGameCompiler.cs` |


---

### 🎯 P18: Sprite Studio, Sheet Packing & Slicing Editor (CONCLUÍDO)
- **Status:** 100% Concluído e Auditado com Sucesso pela Banca Gauntlet Loop (Veredicto Unânime PASSED, 275/275 testes xUnit GREEN).
- **Escopo:** ViewModels especializados de SpriteSheet e SpriteFont, geração de fontes estáticas e SDF via `GeneratePrecompiledFontCommand`, gerador de template `SpriteSheetFromFileTemplateGenerator`, política `AddSpriteStudioModelAssetPolicy`, comandos batch LDR `ThumbnailFromSpriteBatchCommand`, cache thread-safe com detecção de regiões `SpriteEditorImageCache`, e visualizador com slicing, 9-patch e pan/zoom em Avalonia 12 (`SpriteSheetEditorView.axaml`).
- **Localização:** `sources/editor/Stride.Assets.Editor/` e `Stride.Assets.Editor.Avalonia/Views/`.

| Arquivo Original (WPF) | Linhas Originais | Novas Linhas (Avalonia/Editor) | Novo Arquivo |
| :--- | :--- | :--- | :--- |
| `AssetEditors/SpriteEditor/Services/SpriteEditorImageCache.cs` | 132 | **179** | `Stride.Assets.Editor/AssetEditors/SpriteEditor/ViewModels/SpriteEditorImageCache.cs` |
| `ViewModel/SpriteFontViewModel.cs` | 98 | **99** | `Stride.Assets.Editor/ViewModels/SpriteFontViewModel.cs` |
| `Templates/SpriteSheetFromFileTemplateGenerator.cs` | 97 | **97** | `Stride.Assets.Editor/Templates/SpriteSheetFromFileTemplateGenerator.cs` |
| `Thumbnails/ThumbnailFromSpriteBatchCommand.cs` | 97 | **93** | `Stride.Editor/Thumbnails/ThumbnailFromSpriteBatchCommand.cs` |
| `AssetEditors/SpriteEditor/Views/SpriteEditorView.xaml.cs` | 83 | **93** | `Stride.Assets.Editor.Avalonia/Views/SpriteSheetEditorView.axaml.cs` |
| `Thumbnails/SpriteStudioSheetThumbnailCompiler.cs` | 65 | **65** | `Stride.Assets.Editor/Thumbnails/SpriteStudioSheetThumbnailCompiler.cs` |
| `ViewModel/Preview/SpriteSheetPreviewViewModel.cs` | 46 | **67** | `Stride.Assets.Editor/ViewModels/Preview/SpriteSheetPreviewViewModel.cs` |
| `NodePresenters/Updaters/SpriteSheetAssetNodeUpdater.cs` | 41 | **39** | `Stride.Assets.Editor/Quantum/NodePresenters/Updaters/SpriteSheetAssetNodeUpdater.cs` |
| `ViewModel/Preview/SpriteStudioSheetPreviewViewModel.cs` | 35 | **34** | `Stride.Assets.Editor/ViewModels/Preview/SpriteStudioSheetPreviewViewModel.cs` |
| `ViewModel/Preview/SpriteFontPreviewViewModel.cs` | 33 | **32** | `Stride.Assets.Editor/ViewModels/Preview/SpriteFontPreviewViewModel.cs` |
| `ViewModel/SpriteStudioModelViewModel.cs` | 31 | **31** | `Stride.Assets.Editor/ViewModels/SpriteStudioModelViewModel.cs` |
| `AssetEditors/EntityHierarchyEditor/ViewModels/AddAssetPolicies/AddSpriteStudioModelAssetPolicy.cs` | 25 | **26** | `Stride.Assets.Editor/AssetEditors/EntityHierarchyEditor/ViewModels/AddAssetPolicies/AddSpriteStudioModelAssetPolicy.cs` |
| `AssetEditors/SpriteEditor/ViewModels/SpriteInfoPartViewModel.cs` | 24 | **24** | `Stride.Assets.Editor/AssetEditors/SpriteEditor/ViewModels/SpriteInfoPartViewModel.cs` |
| `Thumbnails/SpriteStudioAnimationThumbnailCompiler.cs` | 20 | **20** | `Stride.Assets.Editor/Thumbnails/SpriteStudioAnimationThumbnailCompiler.cs` |
| `Preview/Views/SpriteFontPreviewView.cs` | 18 | **14** | `Stride.Assets.Editor.Avalonia/Views/Preview/SpriteFontPreviewView.cs` |
| `ViewModel/PrecompiledSpriteFontViewModel.cs` | 18 | **18** | `Stride.Assets.Editor/ViewModels/PrecompiledSpriteFontViewModel.cs` |
| `ViewModel/SpriteSheetViewModel.cs` | 18 | **18** | `Stride.Assets.Editor/ViewModels/SpriteSheetViewModel.cs` |
| `Preview/Views/SpriteSheetPreviewView.cs` | 17 | **13** | `Stride.Assets.Editor.Avalonia/Views/Preview/SpriteSheetPreviewView.cs` |


---

### 🎯 P3: Gizmos 3D & Raycasting / Picking (CONCLUÍDO)
- **Status:** 100% Concluído e Auditado com Sucesso pela Banca Gauntlet Loop (Harsh Critics 1, 2 e 3: 0 Critical, 0 Important, 0 Minor).
- **Escopo:** Manipuladores 3D de Translação, Rotação e Escala, Gizmos de Câmera, Luzes, Modelos, Física, Partículas, Navegação, RenderDoc e Picking de Entidades na Viewport Vulkan.
- **Localização:** `sources/editor/Stride.Assets.Editor/AssetEditors/Gizmos/` e `SceneEditor/`.

| Arquivo Original (WPF) | Linhas Originais | Novas Linhas (Avalonia/Editor) | Novo Arquivo |
| :--- | :--- | :--- | :--- |
| `AssetEditors/Gizmos/PhysicsConstraintGizmo.cs` | 569 | **569** | `Stride.Assets.Editor/AssetEditors/Gizmos/PhysicsConstraintGizmo.cs` |
| `AssetEditors/Gizmos/TransformationGizmo.cs` | 480 | **497** | `Stride.Assets.Editor/AssetEditors/Gizmos/TransformationGizmo.cs` |
| `AssetEditors/Gizmos/ViewportGridGizmo.cs` | 416 | **435** | `Stride.Assets.Editor/AssetEditors/Gizmos/ViewportGridGizmo.cs` |
| `AssetEditors/EntityHierarchyEditor/Game/EditorGameLightProbeGizmoService.cs` | 398 | **401** | `Stride.Assets.Editor/AssetEditors/EntityHierarchyEditor/Game/EditorGameLightProbeGizmoService.cs` |
| `AssetEditors/EntityHierarchyEditor/Game/EditorGameComponentGizmoService.cs` | 397 | **397** | `Stride.Assets.Editor/AssetEditors/EntityHierarchyEditor/Game/EditorGameComponentGizmoService.cs` |
| `AssetEditors/Gizmos/CameraOrientationGizmo.cs` | 364 | **384** | `Stride.Assets.Editor/AssetEditors/Gizmos/CameraOrientationGizmo.cs` |
| `AssetEditors/Gizmos/LightSpotGizmo.cs` | 342 | **342** | `Stride.Assets.Editor/AssetEditors/Gizmos/LightSpotGizmo.cs` |
| `AssetEditors/Gizmos/ScaleGizmo.cs` | 325 | **344** | `Stride.Assets.Editor/AssetEditors/Gizmos/ScaleGizmo.cs` |
| `AssetEditors/Gizmos/TranslationGizmo.cs` | 310 | **321** | `Stride.Assets.Editor/AssetEditors/Gizmos/TranslationGizmo.cs` |
| `AssetEditors/Gizmos/PhysicsGizmo.cs` | 283 | **283** | `Stride.Assets.Editor/AssetEditors/Gizmos/PhysicsGizmo.cs` |
| `AssetEditors/Gizmos/RotationGizmo.cs` | 254 | **288** | `Stride.Assets.Editor/AssetEditors/Gizmos/RotationGizmo.cs` |
| `AssetEditors/Gizmos/SpaceMarker.cs` | 215 | **215** | `Stride.Assets.Editor/AssetEditors/Gizmos/SpaceMarker.cs` |
| `AssetEditors/Gizmos/GizmoResources.Designer.cs` | 203 | **209** | `Stride.Assets.Editor/AssetEditors/Gizmos/GizmoResources.Designer.cs` |
| `AssetEditors/Gizmos/CameraGizmo.cs` | 198 | **198** | `Stride.Assets.Editor/AssetEditors/Gizmos/CameraGizmo.cs` |
| `AssetEditors/Gizmos/GizmoBase.cs` | 177 | **177** | `Stride.Assets.Editor/AssetEditors/Gizmos/GizmoBase.cs` |
| `AssetEditors/Gizmos/LightPointGizmo.cs` | 159 | **159** | `Stride.Assets.Editor/AssetEditors/Gizmos/LightPointGizmo.cs` |
| `AssetEditors/Gizmos/VoxelVolumeGizmo.cs` | 151 | **151** | `Stride.Assets.Editor/AssetEditors/Gizmos/VoxelVolumeGizmo.cs` |
| `AssetEditors/Gizmos/EntityGizmo.cs` | 134 | **134** | `Stride.Assets.Editor/AssetEditors/Gizmos/EntityGizmo.cs` |
| `AssetEditors/Gizmos/AxialGizmo.cs` | 134 | **134** | `Stride.Assets.Editor/AssetEditors/Gizmos/AxialGizmo.cs` |
| `AssetEditors/Gizmos/NavigationBoundingBoxGizmo.cs` | 130 | **130** | `Stride.Assets.Editor/AssetEditors/Gizmos/NavigationBoundingBoxGizmo.cs` |
| `AssetEditors/Gizmos/ModelGizmo.cs` | 122 | **122** | `Stride.Assets.Editor/AssetEditors/Gizmos/ModelGizmo.cs` |
| `AssetEditors/Gizmos/DispatcherGizmo.cs` | 122 | **122** | `Stride.Assets.Editor/AssetEditors/Gizmos/DispatcherGizmo.cs` |
| `AssetEditors/Gizmos/LightProbeGizmo.cs` | 96 | **96** | `Stride.Assets.Editor/AssetEditors/Gizmos/LightProbeGizmo.cs` |
| `AssetEditors/Gizmos/ParticleSystemGizmo.cs` | 87 | **87** | `Stride.Assets.Editor/AssetEditors/Gizmos/ParticleSystemGizmo.cs` |
| `AssetEditors/Gizmos/GizmoViewportRenderer.cs` | 83 | **83** | `Stride.Assets.Editor/AssetEditors/Gizmos/GizmoViewportRenderer.cs` |
| `AssetEditors/EntityHierarchyEditor/ViewModels/EntityGizmosViewModel.cs` | 67 | **67** | `Stride.Assets.Editor/AssetEditors/EntityHierarchyEditor/ViewModels/EntityGizmosViewModel.cs` |
| `AssetEditors/Gizmos/LightDirectionalGizmo.cs` | 65 | **65** | `Stride.Assets.Editor/AssetEditors/Gizmos/LightDirectionalGizmo.cs` |
| `AssetEditors/Gizmos/DispatcherLightGizmo.cs` | 63 | **63** | `Stride.Assets.Editor/AssetEditors/Gizmos/DispatcherLightGizmo.cs` |
| `AssetEditors/Gizmos/BillboardingGizmo.cs` | 56 | **63** | `Stride.Assets.Editor/AssetEditors/Gizmos/BillboardingGizmo.cs` |
| `AssetEditors/Gizmos/GridGizmoBase.cs` | 54 | **56** | `Stride.Assets.Editor/AssetEditors/Gizmos/GridGizmoBase.cs` |
| `AssetEditors/Gizmos/GizmoTransformationAxes.cs` | 54 | **54** | `Stride.Assets.Editor/AssetEditors/Gizmos/GizmoTransformationAxes.cs` |
| `AssetEditors/Gizmos/LightShaftsVolumeGizmo.cs` | 50 | **50** | `Stride.Assets.Editor/AssetEditors/Gizmos/LightShaftsVolumeGizmo.cs` |
| `AssetEditors/Gizmos/GizmoEmissiveColorMaterial.cs` | 47 | **47** | `Stride.Assets.Editor/AssetEditors/Gizmos/GizmoEmissiveColorMaterial.cs` |
| `AssetEditors/Gizmos/GizmoUniformColorMaterial.cs` | 43 | **43** | `Stride.Assets.Editor/AssetEditors/Gizmos/GizmoUniformColorMaterial.cs` |

---

### 🎛️ P7: Property Grid & Template Providers (CONCLUÍDO)
- **Status:** 100% Concluído e Auditado no Gauntlet Loop (Harsh Critics 1, 2 e 3: 0 Critical, 0 Important, 0 Minor).
- **Escopo:**
  1. **Specialized Asset Property Template Providers (Avalonia 12):** `AnimationPropertyTemplates.axaml`, `SkeletonPropertyTemplates.axaml`, `MaterialPropertyTemplates.axaml`, `SpriteFontPropertyTemplates.axaml`, `UIPropertyTemplates.axaml`.
  2. **Add Entity Component UI Control (`AddEntityComponentUserControl.axaml/.cs`):** 119 linhas AXAML, 341 linhas C# com busca em tempo real e árvore de componentes.
  3. **Controles Numéricos, Vetoriais, Cores & Validação:** `Vector4Editor.cs`, `NumericTextBox.cs`, `ColorEditor.cs`, `TextBoxKeyUpCommandBehavior.cs`, `DefaultPropertyTemplateProviders.axaml` (1.610 linhas).

---

### 📦 P8: Asset Editors Complementares & Diálogos Modais (CONCLUÍDO)
- **Status:** 100% Concluído e Auditado por Unanimidade no Gauntlet Loop com Ciclo TDD Rigoroso (Protocolo Obrigatório de Bugs: RED -> GREEN com 95/95 testes verdes).
- **Escopo:**
  1. **Diálogos Modais Centrais & Navegação de Templates (`Stride.GameStudio.Avalonia/Views/`):** `NewProjectWindow.axaml` (72 linhas) & `.cs` (108 linhas), `TemplateBrowserUserControl.axaml` (102 linhas) & `.cs` (99 linhas), `TemplateImageConverter.cs` (70 linhas).
  2. **Diálogos de Manutenção de Assets & Notificações Flutuantes:** `FixAssetReferencesWindow.axaml` (206 linhas) & `.cs` (54 linhas), `FixAssetReferencesViewModel.cs` (221 linhas), `NotificationWindow.axaml` (83 linhas) & `.cs` (193 linhas).
  3. **Editores Especializados de Assets:** `ModelEditorView.axaml` (296 linhas), `MaterialEditorView.axaml` (243 linhas), `SoundEditorView.axaml` (393 linhas), `VideoEditorView.axaml` (401 linhas), com ViewModels fortemente tipados.

---

### ⚙️ P9: Sistema de Configurações, Preferências & Diálogos Auxiliares (CONCLUÍDO)
- **Status:** 100% Concluído e Auditado no Gauntlet Loop com Ciclo TDD Rigoroso (Protocolo Obrigatório de Bugs: RED -> GREEN).
- **Escopo:**
  1. **Configurações & Preferências Centrais:** `SettingsWindow.axaml` / `.cs`, `SettingsViewModel`, `SettingsCategoryViewModel`, persistência de preferências de editor, suporte a temas e internacionalização (`SupportedLanguage`).
  2. **Diálogos de Inicialização e Diagnóstico:** Diálogos auxiliares, suporte a crash reporter e integração com o sistema de settings.

---

### 🖼️ P10: Asset View Avançado, Grade de Thumbnails & Drag-and-Drop (CONCLUÍDO)
- **Status:** 100% Concluído e Auditado no Gauntlet Loop com Ciclo TDD Rigoroso (Protocolo Obrigatório de Bugs: RED -> GREEN).
- **Escopo:**
  1. **Grade de Thumbnails & Compilação de Miniaturas:** Compiladores de miniaturas (`ModelThumbnailCompiler`, `MaterialThumbnailCompiler`, `TextureThumbnailCompiler`, `PrefabThumbnailCompiler`, etc.), cache assíncrono e renderização de previews.
  2. **Asset View & Navegação:** `AssetViewUserControl.axaml`, árvore de pastas, filtros por tipo e reatividade de seleção.
  3. **Comportamentos de Drag-and-Drop:** `PropertyViewDragDropBehavior`, `TreeViewDragDropBehavior`, `PropertyViewItemDragDropBehavior`.

---

### 🎛️ P11: Property Grid Avançado & Quantum Node Custom Editors (CONCLUÍDO)
- **Status:** 100% Concluído e Auditado no Gauntlet Loop com Ciclo TDD Rigoroso (Protocolo Obrigatório de Bugs: RED -> GREEN com 152/152 testes verdes).
- **Escopo:**
  1. **Quantum Node Presenter Commands, Updaters & Keys:**
     - Comandos: `AddNewScriptComponentCommand`, `EditCurveCommand`, `PickupEntityCommand`, `PickupEntityComponentCommand`, `PickupSceneObjectCommandBase`, `SetSymbolReferenceCommand`, `SkeletonNodePreserveAllCommand`.
     - Updaters: `NavigationNodeUpdater`, `ModelNodeLinkNodeUpdater`, `EntityHierarchyEditorNodeUpdater`, `VisualScriptNodeUpdater`, `FXAAEffectNodeUpdater`, `SpriteFontAssetNodeUpdater`.
     - Keys quânticas: `ModelNodeLinkData`, `VisualScriptData`, `SpriteFontData`.
     - Modelos de serviços: `IPickedEntity`, `IEntityPickerDialog`, `IStrideDialogService`, `SymbolReferenceViewModel`.
  2. **Property Grid Template Providers & Data Type Selectors:**
     - `DataTypeTemplateSelector` (Avalonia `IDataTemplate`, `TemplateDefinitionCollection`, `TemplateDefinition`).
     - Provedores: `GenericDictionaryNodeTemplateProvider`, `NullableStructTemplateProvider`, `ObjectTemplateProvider`, `ModelComponentMaterialTemplateProvider`, `ScriptVariableReferenceTemplateProvider`, `ModelNodeLinkNameTemplateProvider`, `ScriptTextEditorTemplateProvider`.
     - Desacoplamento e herança limpa de `ContentReferenceTemplateProvider`.
  3. **Behaviors de Validação de Propriedades Vetoriais, Numéricas e Árvore Reativa:**
     - `TextBoxPropertyValueValidationBehavior`: Validação e realce visual (IndianRed) para TextBox.
     - `TextBoxVectorPropertyValueValidationBehavior`: Validação de componentes individuais e vetoriais em `VectorEditorBase`.
     - `ListBoxHighlightedItemBehavior`: Gerenciamento de itens destacados e seleção reativa.
     - `TreeViewAutoExpandBehavior`: Rastreamento e restauração do caminho de nós expandidos/recolhidos via `IChildViewModel`.
- **Validação de Build:** Clean Build (0 Erros) em toda a solução.
- **Testes Automatizados (Fase RED -> GREEN):** 152 / 152 Aprovados (100% Verdes, 0 Falhas) em `Stride.Core.Assets.Editor.Tests`.

---

## 📅 Próximo Marco: P12 - Scene Editor, Hierarchy & Transform Gizmo
- **Objetivo:** Refatoração e portabilidade do editor central de cenas 3D (`SceneEditorView`), árvore hierárquica de entidades (`EntityHierarchyEditor`), sincronização bidirecional do transform gizmo na viewport Vulkan RADV e manipulação de entidades no grafo Quantum.
- **Execução:** Orquestrada via equipes 3x3 (Builders + Harsh Critics) no padrão Spec-Driven / Gauntlet Loop ininterrupto.


### 🎯 P17: Asset Pickers, Reference Selectors & Type Modals (CONCLUÍDO)
- **Status:** 100% Concluído e Homologado pela Banca Gauntlet Loop (Reviewers 1, 2 e 3: PASSED Unânime).
- **Escopo:** Seletores de Assets e Pacotes (, , ), contratos desacoplados (, ), comandos Quantum (, , ), ViewModels de referência e Template Providers do Property Grid (, , ).
- **Validação TDD:** 25 novos testes integrados xUnit adicionados; total de **253/253 testes GREEN** (0 falhas).
