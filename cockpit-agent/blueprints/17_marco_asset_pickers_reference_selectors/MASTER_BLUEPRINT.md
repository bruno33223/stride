# MASTER BLUEPRINT: Marco P17 - Asset Pickers, Reference Selectors & Type Modals

## 1. Visão Geral e Arquitetura Alvo
O objetivo do **Marco P17** é a portabilidade, consolidação e homologação completa dos serviços de seleção de assets, pacotes, entidades e componentes no Stride Game Studio para Linux nativo (Avalonia 12 + Vulkan RADV), garantindo paridade funcional estrita (85-100% de paridade de linhas de código) com o legado WPF/Win32.

O subsistema de Seletores de Assets e Referências compreende:
1. **Contratos, Serviços & Comandos Quantum**:
   - `IPackagePickerDialog` e `IAssetPickerDialog` no namespace central `Stride.Core.Assets.Editor.Services`.
   - `ChangeValueWithPickerCommandBase`, `SetContentReferenceCommand`, `SetEntityReferenceCommand`, `SetComponentReferenceCommand`.
   - `ContentReferenceHelper`, `ReferenceData`, `IAddReferenceViewModel`, `AddReferenceViewModel`, `ContentReferenceViewModel`, `EntityReferenceViewModel`, `ComponentReferenceViewModel`.
2. **Modais Avalonia & Dialog Service**:
   - `AssetPickerWindow.axaml` / `.axaml.cs` em `Stride.Core.Assets.Editor.Avalonia/Views/`.
   - `PackagePickerWindow.axaml` / `.axaml.cs` em `Stride.Core.Assets.Editor.Avalonia/Views/`.
   - `DebuggerPickerWindow.axaml` / `.axaml.cs` e `IDebuggerPickerDialog` em `Stride.Assets.Editor.Avalonia/Views/`.
   - Integração com `EditorDialogService` em `Stride.GameStudio.Avalonia/Services/EditorDialogService.cs`.
3. **Template Providers do Property Grid & Value Converters**:
   - `ContentReferenceTemplateProvider`, `EntityReferenceTemplateProvider`, `EntityComponentReferenceTemplateProvider`, `RenderStageReferenceTemplateProvider`, `ShaderClassNodeMixinReferenceTemplateProvider`.
   - Conversores de valor `ContentReferenceToUrl` e `ContentReferenceToAsset`.

---

## 2. Fatias Verticais (Vertical Slices)

### Fatia 1 (slice-1): Asset Picker & Reference Contracts, ViewModels & Commands
- **Arquivos a portar/alinhar**:
  - `sources/editor/Stride.Core.Assets.Editor/Services/IPackagePickerDialog.cs` (contrato desacoplado de UI)
  - `sources/editor/Stride.Core.Assets.Editor/Services/IEditorDialogService.cs` (adição de `CreatePackagePickerDialog`)
  - `sources/editor/Stride.Core.Assets.Editor/Services/ContentReferenceHelper.cs` (77 linhas fiéis com `CreateReference` e `GetReferenceTarget`)
  - `sources/editor/Stride.Core.Assets.Editor/Quantum/NodePresenters/Keys/ReferenceData.cs` (14 linhas)
  - `sources/editor/Stride.Core.Assets.Editor/ViewModels/IAddReferenceViewModel.cs` e `AddReferenceViewModel.cs` (34 linhas)
  - `sources/editor/Stride.Core.Assets.Editor/ViewModels/ContentReferenceViewModel.cs` (75 linhas fiéis)
  - `sources/editor/Stride.Assets.Presentation/ViewModels/EntityReferenceViewModel.cs` (52 linhas fiéis)
  - `sources/editor/Stride.Assets.Presentation/ViewModels/ComponentReferenceViewModel.cs` (64 linhas fiéis)
  - `sources/editor/Stride.Assets.Editor/Quantum/NodePresenters/Commands/SetEntityReferenceCommand.cs` (38 linhas fiéis)
  - `sources/editor/Stride.Assets.Editor/Quantum/NodePresenters/Commands/SetComponentReferenceCommand.cs` (43 linhas fiéis)
- **Validação TDD**: Teste integrado `TestAssetPickerContractsAndCommandsIntegration.cs` cobrindo criação de referências via helper, validação de nós no Quantum e execução de comandos de referência.

### Fatia 2 (slice-2): Avalonia Modal Windows & Dialog Service Integration
- **Arquivos a portar/alinhar**:
  - `sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/AssetPickerWindow.axaml` e `.axaml.cs` (139 linhas fiéis, compatível com Avalonia 12 nativo e IAssetPickerDialog)
  - `sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/PackagePickerWindow.axaml` e `.axaml.cs` (62 linhas fiéis, suporte a seleção única e múltipla com filtro)
  - `sources/editor/Stride.Assets.Editor/Services/IDebuggerPickerDialog.cs` e `Stride.Assets.Editor.Avalonia/Views/DebuggerPickerWindow.axaml` / `.axaml.cs`
  - `sources/editor/Stride.GameStudio.Avalonia/Services/EditorDialogService.cs` (implementação de `CreateAssetPickerDialog` e `CreatePackagePickerDialog`)
- **Validação TDD**: Teste integrado `TestAssetPickerWindowsAndDialogServiceIntegration.cs` cobrindo instanciação, filtros e contratos de diálogo em modo headless.

### Fatia 3 (slice-3): Reference Selectors, Property Grid Template Providers & Converters
- **Arquivos a portar/alinhar**:
  - `sources/editor/Stride.Assets.Editor.Avalonia/Views/RenderStageReferenceTemplateProvider.cs` (23 linhas fiéis)
  - Alinhamento de `EntityReferenceTemplateProvider.cs`, `EntityComponentReferenceTemplateProvider.cs`, `ShaderClassNodeMixinReferenceTemplateProvider.cs`
  - Registro dos template providers adicionais no container de Property Grid
  - `ContentReferenceToUrl.cs` e `ContentReferenceToAsset.cs` validação e registro de bindings
- **Validação TDD**: Teste integrado `TestReferenceSelectorsAndTemplateProvidersIntegration.cs` cobrindo correspondência de nós (`MatchNode`), resolução de referências e conversão bidirecional de URL/Asset.

---

## 3. Critérios de Homologação (Gauntlet Loop)
1. 0 erros de compilação em toda a solução sob .NET 10.
2. 100% dos testes unitários e de integração passando na suíte xUnit (228+ testes).
3. Testes RED criados antes de cada implementação conforme o Protocolo Obrigatório de Bugs / TDD Iron Law.
4. Paridade estrita de linhas de código preservada sem resumos artificiais (85-100%).
5. Atualização de `MAPPING_TASKLIST.tsv` e `MAPPING_README.md`.
