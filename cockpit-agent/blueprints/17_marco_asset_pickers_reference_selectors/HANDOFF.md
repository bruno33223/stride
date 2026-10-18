# HANDOFF: Marco P17 - Asset Pickers, Reference Selectors & Type Modals

## 1. Visão Geral da Entrega
O **Marco P17** consolidou a portabilidade e homologação dos serviços e diálogos de seleção de assets, pacotes, entidades e componentes no Stride Game Studio para Linux nativo (Avalonia 12 + Vulkan RADV + .NET 10).

- **Veredicto Gauntlet**: **PASSED (100% de Aprovação Unânime)**
- **Suíte de Testes Global**: **253 testes passando (100% GREEN, 0 falhas, 0 regressões)**
- **Tarefas Concluídas no TSV**: 23 novas tarefas marcadas como `CONCLUIDO` em `cockpit-agent/MAPPING_TASKLIST.tsv`
- **Protocolo de Bugs / TDD Iron Law**: Aplicado estritamente na correção de binding em `DefaultPropertyTemplateProviders.axaml` (teste RED antes -> GREEN após).

---

## 2. Artefatos e Fontes Entregues

### Fatia 1 (slice-1): Contratos e Comandos Quantum
- `Stride.Core.Assets.Editor/Services/IPackagePickerDialog.cs`
- `Stride.Core.Assets.Editor/Services/IEditorDialogService.cs`
- `Stride.Core.Assets.Editor/Services/ContentReferenceHelper.cs`
- `Stride.Core.Assets.Editor/Quantum/NodePresenters/Keys/ReferenceData.cs`
- `Stride.Core.Assets.Editor/ViewModels/IAddReferenceViewModel.cs`
- `Stride.Core.Assets.Editor/ViewModels/AddReferenceViewModel.cs`
- `Stride.Core.Assets.Editor/ViewModels/ContentReferenceViewModel.cs`
- `Stride.Assets.Presentation/ViewModels/EntityReferenceViewModel.cs`
- `Stride.Assets.Presentation/ViewModels/ComponentReferenceViewModel.cs`
- `Stride.Assets.Editor/Quantum/NodePresenters/Commands/SetEntityReferenceCommand.cs`
- `Stride.Assets.Editor/Quantum/NodePresenters/Commands/SetComponentReferenceCommand.cs`
- Testes: `TestAssetPickerContractsAndCommandsIntegration.cs` (10/10 GREEN)

### Fatia 2 (slice-2): Janelas Modais Avalonia 12 & Dialog Service
- `Stride.Core.Assets.Editor.Avalonia/Views/AssetPickerWindow.axaml` e `.axaml.cs`
- `Stride.Core.Assets.Editor.Avalonia/Views/PackagePickerWindow.axaml` e `.axaml.cs` (com `Dispatcher.UIThread.CheckAccess()`)
- `Stride.Assets.Editor/Services/IDebuggerPickerDialog.cs` e `IPickedDebugger.cs`
- `Stride.Assets.Editor.Avalonia/Views/DebuggerPickerWindow.axaml` e `.axaml.cs`
- `Stride.GameStudio.Avalonia/Services/EditorDialogService.cs`
- Testes: `TestAssetPickerWindowsAndDialogServiceIntegration.cs` (6/6 GREEN)

### Fatia 3 (slice-3): Template Providers & Value Converters
- `Stride.Assets.Editor.Avalonia/Views/RenderStageReferenceTemplateProvider.cs`
- `Stride.Assets.Editor.Avalonia/Views/EntityReferenceTemplateProvider.cs`
- `Stride.Assets.Editor.Avalonia/Views/EntityComponentReferenceTemplateProvider.cs`
- `Stride.Assets.Editor.Avalonia/Views/ShaderClassNodeMixinReferenceTemplateProvider.cs`
- `Stride.Assets.Editor.Avalonia/Views/ScriptVariableReferenceTemplateProvider.cs`
- `Stride.Core.Assets.Editor.Avalonia/Views/ContentReferenceTemplateProvider.cs`
- `Stride.Core.Assets.Editor.Avalonia/Converters/ContentReferenceToUrl.cs`
- `Stride.Core.Assets.Editor.Avalonia/Converters/ContentReferenceToAsset.cs`
- `DefaultPropertyTemplateProviders.axaml` corrigido com binding canônico
- Testes: `TestReferenceSelectorsAndTemplateProvidersIntegration.cs` (9/9 GREEN)

---

## 3. Próximo Marco: Marco P18 - Sprite Studio, Sheet Packing & Slicing Editor
O **Marco P18** compreende a portabilidade e consolidação do editor e pipeline de sprites e folhas de sprites:
1. **Fatia 1 (slice-1)**: `SpriteSheetEditorViewModel`, `SpriteSheetAsset`, geradores de template de sprite (`SpriteSheetFromFileTemplateGenerator`).
2. **Fatia 2 (slice-2)**: Algoritmo de empacotamento de atlas (`SpriteSheetPacker`), importadores de dados e compiladores offline de sprite studio.
3. **Fatia 3 (slice-3)**: Visual Avalonia 12 do editor de sprites (`SpriteSheetEditorView.axaml` / `.cs`), ferramenta visual de corte (slicing grid, bordas 9-patch e pivôs de animação).
