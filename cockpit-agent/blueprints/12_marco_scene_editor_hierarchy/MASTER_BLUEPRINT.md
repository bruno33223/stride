# MASTER BLUEPRINT: Marco P12 - Scene Editor, Hierarchy & Transform Gizmo

## 1. Visão Geral e Arquitetura Alvo
O objetivo do **Marco P12** é a portabilidade e consolidação integral da suíte de edição de cenas e hierarquia de entidades do Stride Game Studio para Linux nativo (Avalonia 12 + Vulkan RADV), garantindo paridade funcional estrita (85-100% de paridade de linhas de código) com o legado WPF/Win32.

Este marco compreende:
1. **Fábricas de Entidades e Políticas de Adição de Assets**:
   - `IEntityFactory`, categorias e implementações (`Audio`, `Camera`, `Light`, `Model`, `Navigation`, `ParticleSystem`, `TwoDimension`).
   - Sistema de políticas de arrastar-e-soltar / inserção de assets (`IAddAssetPolicy`, `AddAssetPolicyBase`, `AddEntityComponentFileAssetPolicy`, `AddModelAssetPolicy`, `AddPrefabAssetPolicy`, `AddSpriteSheetAssetPolicy`, `AddSpriteStudioModelAssetPolicy`, `AddTextureAssetPolicy`, `AddUIPageAssetPolicy`, `AddVideoAssetPolicy`, `AddSceneAssetPolicy`).
2. **Controladores e ViewModels de Hierarquia e Cena**:
   - `EntityHierarchyElementChangePropagator`.
   - `EntityHierarchyEditorController` e `SceneEditorController`.
   - `SceneEditorViewModel` e `EntityHierarchyEditorViewModel` com paridade completa de features (multi-cenas `AllScenes`, `DisplayCameraPreview`, propagação de transformações, comandos de criação, busca/filtro na hierarquia, deleção em lote com undo/redo, etc.).
3. **Controles Visuais Avalonia & Entity Picker**:
   - Diálogo `EntityPickerWindow.axaml` / `EntityPickerWindow.axaml.cs` implementando o contrato `IEntityPickerDialog`.
   - Integração da árvore hierárquica e controle de viewport com Gizmos de Transformação (Translação, Rotação e Escala) em Avalonia.

---

## 2. Fatias Verticais (Vertical Slices)

### Fatia 1 (slice-1): Entity Factories, Asset Addition Policies & Hierarchy Synchronization
- **Arquivos a portar/alinhar**:
  - `Stride.Assets.Editor/AssetEditors/EntityHierarchyEditor/EntityFactories/`:
    - `IEntityFactory.cs`
    - `EntityFactoryCategory.cs`
    - `EntityFactory.cs`
    - `AudioEntityFactory.cs`
    - `CameraEntityFactory.cs`
    - `LightEntityFactory.cs`
    - `ModelEntityFactory.cs`
    - `NavigationEntityFactory.cs`
    - `ParticleSystemEntityFactory.cs`
    - `TwoDimensionEntityFactory.cs`
  - `Stride.Assets.Editor/AssetEditors/EntityHierarchyEditor/ViewModels/AddAssetPolicies/`:
    - `IAddAssetPolicy.cs`
    - `AddAssetPolicyBase.cs`
    - `AddEntityComponentFileAssetPolicy.cs`
    - `AddModelAssetPolicy.cs`
    - `AddPrefabAssetPolicy.cs`
    - `AddSpriteSheetAssetPolicy.cs`
    - `AddSpriteStudioModelAssetPolicy.cs`
    - `AddTextureAssetPolicy.cs`
    - `AddUIPageAssetPolicy.cs`
    - `AddVideoAssetPolicy.cs`
  - `Stride.Assets.Editor/AssetEditors/SceneEditor/ViewModels/AddAssetPolicies/`:
    - `AddSceneAssetPolicy.cs`
  - `Stride.Assets.Editor/AssetEditors/EntityHierarchyEditor/ViewModels/EntityHierarchyElementChangePropagator.cs`
- **Validação TDD**: Teste integrado `TestEntityFactoriesAndAddAssetPolicies.cs`.

### Fatia 2 (slice-2): Controllers & ViewModels de Cena e Hierarquia Completos
- **Arquivos a portar/alinhar**:
  - `Stride.Assets.Editor/AssetEditors/EntityHierarchyEditor/Services/EntityHierarchyEditorController.cs`
  - `Stride.Assets.Editor/AssetEditors/SceneEditor/Services/SceneEditorController.cs`
  - `Stride.Assets.Editor/ViewModels/SceneEditorViewModel.cs` (expansão de 27 linhas para ~236 linhas com paridade 100%)
  - `Stride.Assets.Editor/ViewModels/EntityHierarchyEditorViewModel.cs` (completude de comandos e sincronização com game controller)
- **Validação TDD**: Teste integrado `TestSceneAndHierarchyControllers.cs`.

### Fatia 3 (slice-3): Controles Visuais Avalonia, Entity Picker & Transform Gizmos
- **Arquivos a portar/alinhar**:
  - `Stride.Assets.Editor.Avalonia/Views/EntityPickerWindow.axaml`
  - `Stride.Assets.Editor.Avalonia/Views/EntityPickerWindow.axaml.cs`
  - Registrador de serviço de diálogo para `IEntityPickerDialog`
  - `Stride.Assets.Editor.Avalonia/Views/EntityHierarchyEditorView.axaml` e `.axaml.cs`
  - Integração com Gizmos de Transformação (`TransformationGizmo`, `TranslationGizmo`, `RotationGizmo`, `ScaleGizmo`)
- **Validação TDD**: Teste integrado `TestEntityPickerAndGizmoIntegration.cs`.

---

## 3. Critérios de Homologação (Gauntlet Loop)
1. 0 erros de compilação em toda a solução sob .NET 10.
2. 100% dos testes unitários e de integração passando na suíte xUnit.
3. Testes RED criados antes de cada implementação conforme o Protocolo Obrigatório de Bugs / TDD Iron Law.
4. Paridade estrita de linhas de código preservada sem resumos artificiais.
5. Atualização de `MAPPING_TASKLIST.tsv` e `MAPPING_README.md`.
