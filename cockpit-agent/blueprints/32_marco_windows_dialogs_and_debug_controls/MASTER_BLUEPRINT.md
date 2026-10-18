# 🏛️ MASTER BLUEPRINT: Marco P32 - Editor Windows, Dialogs, Debug UserControls & Test Assets

**Marco:** P32  
**Objetivo:** Migrar e desacoplar para Linux (.NET 10 / Avalonia 12 / Vulkan RADV) as janelas e diálogos de gerenciamento de templates e progresso (`ProjectSelectionWindow`, `WorkProgressWindow`, `ItemTemplatesWindow`, `AddItemWindow`, `AddItemUserControl`, `ObjectBrowserUserControl`), controles visuais de depuração e inspeção Quantum (`DebugAssetNodesUserControl`, `DebugUndoRedoUserControl`, `DebugLogUserControl`, `DebugEntityHierarchyEditorUserControl`, `ScriptEditorView`, `UIEditorView`, `GraphicsCompositorEditorView`), e classes de teste de assets e designers de atalhos e recursos (`TestAsset`, `UnitTestAsset`, `KeyGestures`, `Images`, `Resources`, `Settings`).  
**Orquestrador (Tech Leader):** Antigravity Staff Engineer  
**Padrão de Execução:** Gauntlet Loop (3 Builders vs 3 Reviewers Independentes)  

---

## 🎯 Fatias Verticais

### 🔹 Fatia 1 (slice-1): Template & Progress Windows & Dialogs
- **Builder:** `Template & Progress Windows Builder`
- **Reviewer:** `Template & Progress Windows Reviewer`
- **Escopo Técnico (10 arquivos / ~700 l):**
  1. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/ProjectSelectionWindow.axaml` + `.cs` (107 l + 98 l)
  2. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/WorkProgressWindow.axaml` + `.cs` (40 l + 88 l)
  3. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/ItemTemplatesWindow.axaml` + `.cs` (43 l + 65 l)
  4. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/AddItemWindow.axaml` + `.cs` (28 l + 41 l)
  5. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/AddItemUserControl.axaml` + `.cs` (139 l + 46 l)
  6. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/ObjectBrowserUserControl.axaml` + `.cs` (50 l + 69 l)
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestTemplateAndProgressWindowsIntegration.cs` (RED -> GREEN).

---

### 🔹 Fatia 2 (slice-2): Debug Tools UserControls & Editor Views
- **Builder:** `Debug Tools & Editor Views Builder`
- **Reviewer:** `Debug Tools & Editor Views Reviewer`
- **Escopo Técnico (12 arquivos / ~800 l):**
  1. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/DebugTools/DebugAssetNodesUserControl.axaml` + `.cs` (87 l + 263 l)
  2. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/DebugTools/DebugUndoRedoUserControl.axaml` + `.cs` (74 l + 27 l)
  3. `sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/DebugTools/DebugLogUserControl.axaml` + `.cs` (13 l + 27 l)
  4. `sources/editor/Stride.Assets.Editor.Avalonia/Views/DebugEntityHierarchyEditorUserControl.axaml` + `.cs` (35 l + 93 l)
  5. `sources/editor/Stride.Assets.Editor.Avalonia/AssetEditors/ScriptEditor/ScriptEditorView.axaml` + `.cs` (18 l + 92 l)
  6. `sources/editor/Stride.Assets.Editor.Avalonia/AssetEditors/UIEditor/Views/UIEditorView.axaml.cs` (74 l)
  7. `sources/editor/Stride.Assets.Editor.Avalonia/AssetEditors/GraphicsCompositorEditor/Views/GraphicsCompositorEditorView.axaml.cs` (60 l)
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestDebugToolsAndEditorViewsIntegration.cs` (RED -> GREEN).

---

### 🔹 Fatia 3 (slice-3): Test Assets, KeyGestures & Resource Designers
- **Builder:** `Test Assets & Resource Designers Builder`
- **Reviewer:** `Test Assets & Resource Designers Reviewer`
- **Escopo Técnico (7 arquivos / ~900 l):**
  1. `sources/editor/Stride.Assets.Presentation/Test/TestAsset.cs` (394 l)
  2. `sources/editor/Stride.Assets.Presentation/Test/UnitTestAsset.cs` (41 l)
  3. `sources/editor/Stride.Assets.Presentation/Resources/Strings/KeyGestures.Designer.cs` (144 l)
  4. `sources/editor/Stride.Core.Assets.Editor/Resources/Strings/KeyGestures.Designer.cs` (144 l)
  5. `sources/editor/Stride.Core.Assets.Editor/Resources/Images.Designer.cs` (73 l)
  6. `sources/editor/Stride.Assets.Presentation/Properties/Resources.Designer.cs` (63 l)
  7. `sources/editor/Stride.Assets.Presentation/Properties/Settings.Designer.cs` (26 l)
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestAssetsAndResourceDesignersIntegration.cs` (RED -> GREEN).

---

## 🛡️ Regras e Protocolos Rígidos
1. Respostas em **PT-BR**.
2. **Sem Gambiarras:** 85-100% de paridade estrita com o legado Stride, sem stubs falsos ou NotImplementedException.
3. **Protocolo Obrigatório de Bugs / TDD Iron Law:** Criação obrigatória de teste integrado que comprove a falha inicial (RED) antes de qualquer código de produção, concluindo apenas com 100% GREEN.
4. **Despacho Ordenado:** Despachar simultaneamente os 3 Builders via `invoke_subagent`. Os Reviewers só entram após a entrega dos 3 testes verdes.
