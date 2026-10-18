# 🤝 HANDOFF: Marco P28 - Editor Presentation Controls, View Behaviors & Scene Picking Infrastructure

**Marco:** P28  
**Data:** 2026-10-18  
**Autor:** Antigravity Staff Engineer  
**Status:** **HOMOLOGADO COM SUCESSO**  
**Próximo Marco:** Marco P29

---

## 📌 Resumo Executivo da Entrega

O Marco P28 concluiu com excelência a migração e o desacoplamento de toda a infraestrutura central de controles de apresentação do Editor Stride, behaviors de viewport e manipulação de cena, engine de raycasting / picking geométrico 3D (`PickingSceneRenderer`), destacadores de assets com resolução de dependências e operações estruturais na árvore hierárquica de entidades da cena.

### Métricas Consolidadas:
- **Tarefas Mapeadas Concluídas:** 14 tarefas legadas migradas.
- **Linhas Originais Migradas:** 2.376 linhas legadas WPF portadas.
- **Linhas Novas Implementadas:** 3.761 linhas de código de produção e testes em .NET 10 / Avalonia.
- **Testes Integrados Criados e Aprovados (TDD Iron Law):** 21 testes (100% GREEN, zero falhas).
- **Taxa de Paridade Real:** 100% com os equivalentes WPF originais.
- **Gambiarras / Stubs:** ZERO.

---

## 📦 Artefatos Produzidos

### 1. Código de Produção (Cross-Platform & Avalonia 12)
- `sources/editor/Stride.Core.Assets.Editor/ArgumentCheck.cs` (392 l)
- `sources/editor/Stride.Core.Assets.Editor.Avalonia/Controls/GridLogViewer.cs` (363 l)
- `sources/editor/Stride.Core.Assets.Editor.Avalonia/Controls/EditableContentListBox.cs` (306 l)
- `sources/editor/Stride.Core.Assets.Editor.Avalonia/Controls/DataGridEx.cs` (155 l)
- `sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/ToolTipHelper.cs` (161 l)
- `sources/editor/Stride.Assets.Presentation/SceneEditor/PickingSceneRenderer.cs` (245 l)
- `sources/editor/Stride.Assets.Editor.Avalonia/Behaviors/ThumbLikeBehavior.cs` (231 l)
- `sources/editor/Stride.Assets.Editor.Avalonia/Behaviors/ViewportPaneBehavior.cs` (166 l)
- `sources/editor/Stride.Assets.Presentation/AssetEditors/AssetHighlighters/TextureAssetHighlighter.cs` (134 l)
- `sources/editor/Stride.Assets.Presentation/AssetEditors/AssetHighlighters/MaterialAssetHighlighter.cs` (126 l)
- `sources/editor/Stride.Assets.Presentation/AssetEditors/EntityHierarchyEditor/ViewModels/EntityFolderOperation.cs` (166 l)
- `sources/editor/Stride.Assets.Presentation/AssetEditors/EntityComponentSourceCodeResolver.cs` (211 l)
- `sources/editor/Stride.Assets.Presentation/AssetEditors/IEntityComponentSourceCodeResolver.cs` (26 l)
- `sources/editor/Stride.Assets.Presentation/NodePresenters/Updaters/UIAssetNodeUpdater.cs` (188 l)

### 2. Testes Integrados Automatizados (.NET 10 / xUnit)
- `sources/editor/Stride.Core.Assets.Editor.Tests/TestEditorControlsAndDiagnosticsIntegration.cs` (325 l - 7 testes GREEN)
- `sources/editor/Stride.Core.Assets.Editor.Tests/TestViewportBehaviorsAndPickingIntegration.cs` (356 l - 10 testes GREEN)
- `sources/editor/Stride.Core.Assets.Editor.Tests/TestEntityOperationsAndCodeResolversIntegration.cs` (210 l - 4 testes GREEN)

---

## 🔍 Instruções para o Próximo Marco (Marco P29)

1. Mapear o escopo do Marco P29 a partir do `cockpit-agent/MAPPING_TASKLIST.tsv`.
2. Criar o diretório `cockpit-agent/blueprints/29_marco_.../` com o `MASTER_BLUEPRINT.md`.
3. Dividir em exatamente 3 fatias verticais ortogonais.
4. Orquestrar os 3 Builders simultaneamente via `invoke_subagent` sob o TDD Iron Law (RED -> GREEN).
5. Após entrega e testes 100% verdes dos Builders, acionar a Banca Revisora (3 Reviewers) para auditoria e homologação.
