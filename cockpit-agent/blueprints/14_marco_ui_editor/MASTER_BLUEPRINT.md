# MASTER BLUEPRINT: Marco P14 - UI Editor, UI Canvas & Visual Adorners (UIElement Hierarchy, Layout & Sizing)

## 1. Visão Geral e Arquitetura Alvo
O objetivo do **Marco P14** é a portabilidade, consolidação e testes rigorosos da suíte de edição visual de interfaces 2D e 3D do Stride Game Studio para Linux nativo (Avalonia 12 + Vulkan RADV), garantindo paridade funcional estrita (85-100% de paridade de linhas de código) com o legado WPF/Win32.

Este marco compreende:
1. **UI ViewModels & Element Hierarchy**:
   - `UIHierarchyItemViewModel`, `UIElementViewModel`, `UIRootViewModel`.
   - `UIElementFromLibrary`, `UIElementFromSystemLibrary`.
   - Conversor de tipos e fábrica de elementos (`UIElementFactoryTypeConverter`).
2. **UI Game Services, Layout & Camera**:
   - `UIEditorGameAdornerService` (incluindo `UIEditorGameAdornerService.Events.cs`).
   - `UIEditorGameCameraService`.
   - `UILayoutHelper` (manipulação de ancoragem, grid, canvas, stacks).
   - `UIEditorController` (ponte de despacho para viewport e runtime de UI).
3. **Visual Adorners & Avalonia Controls**:
   - Sistema de adorners visuais de manipulação interativa: `AdornerBase`, `IResizingAdorner`, `SizingAdorner`, `MoveAdorner`, `MarginAdorner`, `BorderAdorner`, `HighlightAdorner`.
   - Controles visuais Avalonia 12: `ThicknessEditor` e integração com `UIPageEditorView` / `UILibraryEditorView`.

---

## 2. Fatias Verticais (Vertical Slices)

### Fatia 1 (slice-1): UI ViewModels & Element Hierarchy
- **Arquivos a portar/alinhar**:
  - `Stride.Assets.Editor/AssetEditors/UIEditor/ViewModels/UIHierarchyItemViewModel.cs`
  - `Stride.Assets.Editor/AssetEditors/UIEditor/ViewModels/UIElementViewModel.cs`
  - `Stride.Assets.Editor/AssetEditors/UIEditor/ViewModels/UIRootViewModel.cs`
  - `Stride.Assets.Editor/AssetEditors/UIEditor/ViewModels/UIElementFromLibrary.cs`
  - `Stride.Assets.Editor/AssetEditors/UIEditor/ViewModels/UIElementFromSystemLibrary.cs`
  - `Stride.Assets.Editor/AssetEditors/UIEditor/Views/UIElementFactoryTypeConverter.cs`
- **Validação TDD**: Teste integrado `TestUIViewModelsAndHierarchyIntegration.cs`.

### Fatia 2 (slice-2): UI Game Services, Layout & Camera
- **Arquivos a portar/alinhar**:
  - `Stride.Assets.Editor/AssetEditors/UIEditor/Game/UIEditorGameAdornerService.cs`
  - `Stride.Assets.Editor/AssetEditors/UIEditor/Game/UIEditorGameAdornerService.Events.cs`
  - `Stride.Assets.Editor/AssetEditors/UIEditor/Game/UIEditorGameCameraService.cs`
  - `Stride.Assets.Editor/AssetEditors/UIEditor/Game/UILayoutHelper.cs`
  - `Stride.Assets.Editor/AssetEditors/UIEditor/Services/UIEditorController.cs`
- **Validação TDD**: Teste integrado `TestUIGameServicesAndLayoutIntegration.cs`.

### Fatia 3 (slice-3): Visual Adorners & Avalonia Controls
- **Arquivos a portar/alinhar**:
  - `Stride.Assets.Editor/AssetEditors/UIEditor/Adorners/AdornerBase.cs`
  - `Stride.Assets.Editor/AssetEditors/UIEditor/Adorners/IResizingAdorner.cs`
  - `Stride.Assets.Editor/AssetEditors/UIEditor/Adorners/SizingAdorner.cs`
  - `Stride.Assets.Editor/AssetEditors/UIEditor/Adorners/MoveAdorner.cs`
  - `Stride.Assets.Editor/AssetEditors/UIEditor/Adorners/MarginAdorner.cs`
  - `Stride.Assets.Editor/AssetEditors/UIEditor/Adorners/BorderAdorner.cs`
  - `Stride.Assets.Editor/AssetEditors/UIEditor/Adorners/HighlightAdorner.cs`
  - `Stride.Assets.Editor.Avalonia/Views/ThicknessEditor.cs`
- **Validação TDD**: Teste integrado `TestUIAdornersAndControlsIntegration.cs`.

---

## 3. Critérios de Homologação (Gauntlet Loop)
1. 0 erros de compilação em toda a solução sob .NET 10.
2. 100% dos testes unitários e de integração passando na suíte xUnit.
3. Testes RED criados antes de cada implementação conforme o Protocolo Obrigatório de Bugs / TDD Iron Law.
4. Paridade estrita de linhas de código preservada sem resumos artificiais.
5. Atualização de `MAPPING_TASKLIST.tsv` e `MAPPING_README.md`.
