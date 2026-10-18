# MASTER BLUEPRINT: Marco P13 - Prefab Editor & Nested Archetypes / Asset Composites

## 1. Visão Geral e Arquitetura Alvo
O objetivo do **Marco P13** é a portabilidade, expansão fiel e consolidação integral do editor dedicado de prefabs (`PrefabEditorView`), arquitetura de arquétipos aninhados e compósitos de assets (`AssetCompositeHierarchy`, `ArchetypeNodeUpdater`, `AssetCompositeHierarchyPasteProcessor`), integração de controladores de jogo (`PrefabEditorController`, `PrefabEditorGame`, `PrefabEditorLightService`) e o script de instância de prefabs em runtime (`PrefabInstance.cs`) para Linux nativo (Avalonia 12 + Vulkan RADV), garantindo paridade funcional estrita (85-100% de paridade de linhas de código) com o legado WPF/Win32.

Este marco compreende:
1. **Prefab ViewModels, Archetype Engine & Base Part Linking**:
   - `IEditorDesignPartViewModel<TAssetPartDesign, TAssetPart>`.
   - `PrefabEditorViewModel` completo (com controllerFactory, inicialização de partes raiz, `LoadSettings`, `InitializeEditor` expandindo o root).
   - `PrefabRootViewModel` completo (com `OnLoadingRequested`, `OnLockingRequested`, propagação recursiva de carregamento/bloqueio para sub-entidades, notificação `NotifyGameSidePartAdded`).
   - Integração com `ArchetypeNodeUpdater`, `AssetCompositeHierarchyPasteProcessor`, `AssetCompositeHierarchyViewModel`.
2. **Prefab Editor Controllers, Game-Side Loading & Services**:
   - `PrefabEditorController` completo (145 linhas fiéis, com `AddPart`, `RemovePart`, `LoadEntities`, `UnloadEntities`, `GetAbsoluteId`, `FindPart`, inicialização de serviços de game).
   - `PrefabEditorGame` (81 linhas fiéis, com `FindSubEntity`, `LoadEntities`, `LoadEntity`, `UnloadEntities`, `UnloadEntity`).
   - `PrefabEditorLightService` (45 linhas fiéis, ambient light e directional lights dedicadas para preview sem gizmos visuais).
3. **Prefab Editor Avalonia View, TreeView Helper & Instance Script**:
   - `PrefabEditorView.cs` registrado via `[AssetEditorView<PrefabEditorViewModel>]` sobre Avalonia 12.
   - `AssetCompositeHierarchyTreeViewHelper.cs` (utilitário para expansão e colapso de nós em árvore Avalonia).
   - `PrefabInstance.cs` (template de script para spawn de instâncias com timeout, follow e triggers por teclado/intervalo).

---

## 2. Fatias Verticais (Vertical Slices)

### Fatia 1 (slice-1): Prefab ViewModels, Archetype Engine & Base Part Linking
- **Arquivos a portar/alinhar**:
  - `Stride.Assets.Editor/AssetEditors/AssetCompositeGameEditor/ViewModels/IEditorDesignPartViewModel.cs`
  - `Stride.Assets.Editor/ViewModels/PrefabEditorViewModel.cs`
  - `Stride.Assets.Presentation/ViewModels/PrefabRootViewModel.cs`
  - Verificação de nós virtuais em `ArchetypeNodeUpdater` e deserialização de hierarquia em `AssetCompositeHierarchyPasteProcessor`.
- **Validação TDD**: Teste integrado `TestPrefabViewModelsAndArchetypeIntegration.cs`.

### Fatia 2 (slice-2): Prefab Editor Controllers, Game-Side Loading & Services
- **Arquivos a portar/alinhar**:
  - `Stride.Assets.Editor/AssetEditors/PrefabEditor/Services/PrefabEditorController.cs`
  - `Stride.Assets.Editor/AssetEditors/PrefabEditor/Game/PrefabEditorGame.cs`
  - `Stride.Assets.Editor/AssetEditors/PrefabEditor/Game/PrefabEditorLightService.cs`
- **Validação TDD**: Teste integrado `TestPrefabEditorControllerAndGameIntegration.cs`.

### Fatia 3 (slice-3): Prefab Editor Avalonia View, TreeView Helper & Instance Script
- **Arquivos a portar/alinhar**:
  - `Stride.Assets.Editor.Avalonia/Views/PrefabEditorView.cs`
  - `Stride.Assets.Editor.Avalonia/Views/AssetCompositeHierarchyTreeViewHelper.cs`
  - `Stride.Assets.Editor/Templates/Assets/Scripts/Prefabs/PrefabInstance.cs`
- **Validação TDD**: Teste integrado `TestPrefabEditorViewAndInstanceScriptIntegration.cs`.

---

## 3. Critérios de Homologação (Gauntlet Loop)
1. 0 erros de compilação em toda a solução sob .NET 10.
2. 100% dos testes unitários e de integração passando na suíte xUnit.
3. Testes RED criados antes de cada implementação conforme o Protocolo Obrigatório de Bugs / TDD Iron Law.
4. Paridade estrita de linhas de código preservada sem resumos artificiais.
5. Atualização de `MAPPING_TASKLIST.tsv` e `MAPPING_README.md`.
