# 🏛️ MASTER BLUEPRINT: Marco P30 - Presentation Behaviors, Template Generators & Value Converters Infrastructure

**Marco:** P30  
**Objetivo:** Migrar e desacoplar para Linux (.NET 10 / Avalonia 12 / Vulkan RADV) toda a suíte de behaviors de interação e apresentação do Editor (`BindScrollOffsetBehavior`, `ThumbBehavior`, `ViewportZoomBehavior`, `ValidateTextBoxAfterSlidingBehavior`, etc.), o ecossistema de geradores de templates de assets e operações de sessão (`VideoFromFileTemplateGenerator`, `TemplateGeneratorHelper`, `AssetsUpgradeOperation`, etc.), e a infraestrutura completa de Value Converters e Quantum Template Providers.  
**Orquestrador (Tech Leader):** Antigravity Staff Engineer  
**Padrão de Execução:** Gauntlet Loop (3 Builders vs 3 Reviewers Independentes)

---

## 🎯 Fatias Verticais

### 🔹 Fatia 1 (slice-1): Viewport Interaction, Sliding & Selection Behaviors (Avalonia)
- **Builder:** `Presentation Behaviors & Controls Builder`
- **Reviewer:** `Presentation Behaviors & Controls Reviewer`
- **Escopo Técnico (15 arquivos - ~680 l):**
  1. `sources/editor/Stride.Assets.Editor.Avalonia/Behaviors/BindScrollOffsetBehavior.cs` (64 l)
  2. `sources/editor/Stride.Assets.Editor.Avalonia/Behaviors/ThumbBehavior.cs` (61 l)
  3. `sources/editor/Stride.Assets.Editor.Avalonia/Behaviors/ViewportZoomBehavior.cs` (58 l)
  4. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Behaviors/ValidateTextBoxAfterSlidingBehavior.cs` (61 l)
  5. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Behaviors/DragContainer.cs` (65 l)
  6. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Behaviors/ActivateOnLogBehavior.cs` (64 l)
  7. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Behaviors/TreeViewBindableSelectedItemsBehavior.cs` (44 l)
  8. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Behaviors/BringSelectionToViewBehavior.cs` (37 l)
  9. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Behaviors/PropertyViewFilteringBehavior.cs` (33 l)
  10. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Behaviors/OnComboBoxClosedWithSelectionBehavior.cs` (32 l)
  11. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Behaviors/OnSelectionChangedWithSelectionBehavior.cs` (31 l)
  12. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Behaviors/ActivateOnLocationChangedBehavior.cs` (31 l)
  13. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Extensions/ControlExtensions.cs` (66 l)
  14. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Controls/TreeViewTemplateSelector.cs` (64 l)
  15. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Commands/SetContentTemplateCommand.cs` (30 l)
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestPresentationBehaviorsAndControlsIntegration.cs` (RED -> GREEN).

---

### 🔹 Fatia 2 (slice-2): Asset Template Generators, Upgrade Operations & Dialog ViewModels
- **Builder:** `Template Generators & Asset Operations Builder`
- **Reviewer:** `Template Generators & Asset Operations Reviewer`
- **Escopo Técnico (18 arquivos - ~690 l):**
  1. `sources/editor/Stride.Assets.Presentation/Templates/VideoFromFileTemplateGenerator.cs` (75 l)
  2. `sources/editor/Stride.Core.Assets.Editor/Components/TemplateDescriptions/TemplateGeneratorHelper.cs` (68 l)
  3. `sources/editor/Stride.Core.Assets.Editor/ViewModel/FixAssetReferenceActionItem.cs` (62 l)
  4. `sources/editor/Stride.Core.Assets.Editor/ViewModel/FuncClipboardMonitor.cs` (58 l)
  5. `sources/editor/Stride.Core.Assets.Editor/ViewModel/AssetsUpgradeOperation.cs` (43 l)
  6. `sources/editor/Stride.Core.Assets.Editor/Components/AddAssets/AssetTemplatesViewModel.cs` (43 l)
  7. `sources/editor/Stride.Core.Assets.Editor/Components/FixAssetReferences/AssetReferenceReplacementViewModel.cs` (41 l)
  8. `sources/editor/Stride.Assets.Presentation/Templates/HullAssetFactoryTemplateGenerator.cs` (47 l)
  9. `sources/editor/Stride.Assets.Presentation/Templates/HeightmapFactoryTemplateGenerator.cs` (45 l)
  10. `sources/editor/Stride.Assets.Presentation/Templates/GraphicsCompositorTemplateGenerator.cs` (42 l)
  11. `sources/editor/Stride.Core.Assets.Editor/Components/TemplateDescriptions/ViewModels/AddItemTemplateCollectionViewModel.cs` (36 l)
  12. `sources/editor/Stride.Core.Assets.Editor/ViewModel/BackgroundTaskDescription.cs` (28 l)
  13. `sources/editor/Stride.Core.Assets.Editor/ViewModel/ColorPaletteViewModel.cs` (24 l)
  14. `sources/editor/Stride.Core.Assets.Editor/ViewModel/IDragSourceViewModel.cs` (14 l)
  15. `sources/editor/Stride.Core.Assets.Editor/ViewModel/Progress/KeepOpen.cs` (12 l)
  16. `sources/editor/Stride.Assets.Presentation/ViewModel/Commands/MoveDirection.cs` (10 l)
  17. `sources/editor/Stride.Assets.Presentation/ViewModel/IResizingTarget.cs` (45 l)
  18. `sources/editor/Stride.Assets.Presentation/SceneEditor/EntityPickingResult.cs` (62 l)
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestTemplateGeneratorsAndAssetOperationsIntegration.cs` (RED -> GREEN).

---

### 🔹 Fatia 3 (slice-3): Quantum Value Converters & Template Providers
- **Builder:** `Value Converters & Template Providers Builder`
- **Reviewer:** `Value Converters & Template Providers Reviewer`
- **Escopo Técnico (43 arquivos - ~1.200 l):**
  1. Conversores de Apresentação (25 arquivos em `Stride.Assets.Presentation/ValueConverters/` e `Stride.Core.Assets.Editor/View/ValueConverters/`):
     `MethodToDisplayName`, `FlagEnumToObservableList`, `TimeToFrames`, `AssetToExpandedAtInitialization`, `AvailableVariableReferenceValueConverter`, `AbstractNodeEntryMatchesNodeValue`, `VisualScriptSlotBrush`, `AssetFilterViewModelToFullDisplayName`, `NameBreakingLine`, `NodeToCameraSlotIndex`, `TypeToResource`, `EnumToTooltip`, `EntityComponentToTransformLinkInfo`, `AssetViewModelToUrl`, `UFileToBitmapImage`, `EntityComponentToResource`, `DifferentValuesToString`, `DifferentValuesToNull`, `DifferentValueToParam`, `CollectionContainsItem`, `EnumToResource`, `IsFirstChild`, `AbstractNodeEntryToType`, `AbstractNodeEntryToDisplayName`, `TypeToDisplayName`.
  2. Quantum Template Providers (18 arquivos em `Stride.Core.Assets.Editor/View/TemplateProviders/` e `Stride.Assets.Presentation/TemplateProviders/`):
     `NodeViewModelTemplateProvider`, `ComputeCurveTemplateProviders`, `DictionaryStringKeyTemplateProvider`, `DictionaryEnumKeyTemplateProvider`, `ListTemplateProvider`, `NullableTemplateProvider`, `ArrayTemplateProvider`, `GameSettingAddConfigurationTemplateProvider`, `RangedValueTemplateProvider`, `CategoryNodeTemplateProvider`, `DictionaryTemplateProvider`, `UnloadableObjectTemplateProvider`, `EntityComponentCollectionTemplateProvider`, `AbstractTypeTemplateProvider`, `GameSettingsFiltersTemplateProvider`, `SetTemplateProvider`, `ListItemTemplateProvider`, `ArrayItemTemplateProvider`.
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestValueConvertersAndTemplateProvidersIntegration.cs` (RED -> GREEN).

---

## 🛡️ Regras e Protocolos Rígidos
1. Respostas em **PT-BR**.
2. **Sem Gambiarras:** Código limpo, 85-100% de paridade com o legado Stride e desacoplamento 100% de WPF/Win32.
3. **Protocolo Obrigatório de Bugs / TDD Iron Law:** Cada Builder deve obrigatoriamente criar o teste integrado e comprovar a fase RED antes de qualquer implementação de código. O código só é aceito com 100% de testes GREEN.
4. **Despacho Ordenado:** Os 3 Builders são despachados simultaneamente via `invoke_subagent`. Os 3 Reviewers só serão convocados após as 3 fatias estarem entregues com testes verdes.
