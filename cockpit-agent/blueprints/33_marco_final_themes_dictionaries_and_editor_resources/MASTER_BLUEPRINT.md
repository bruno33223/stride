# 🏛️ MASTER BLUEPRINT: Marco P33 - Temas, Dicionários de Imagens & Recursos Globais (A GRANDE FINAL - 100%)

**Marco:** P33 (O Marco Final)  
**Objetivo:** Migrar e converter para Linux (.NET 10 / Avalonia 12 / Vulkan RADV) os últimos 17 recursos visuais legados do Stride Game Studio, completando **1.022 / 1.022 tarefas (100,00%)** e coroando a migração definitiva para Linux Nativo!  
**Orquestrador (Tech Leader):** Antigravity Staff Engineer  
**Padrão de Execução:** Gauntlet Loop (3 Builders vs 3 Reviewers Independentes)  

---

## 🎯 Fatias Verticais

### 🔹 Fatia 1 (slice-1): Vector Image Dictionaries & Core Common Resources
- **Builder:** `Vector Image Dictionaries & Common Resources Builder`
- **Reviewer:** `Vector Image Dictionaries & Common Resources Reviewer`
- **Escopo Técnico (3 arquivos / 3.167 linhas originais legadas):**
  1. `sources/editor/Stride.Core.Assets.Editor.Wpf/View/ImageDictionary.xaml` (1.647 l) -> `sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/ImageDictionary.axaml`
  2. `sources/editor/Stride.Assets.Presentation.Wpf/View/ImageDictionary.xaml` (1.192 l) -> `sources/editor/Stride.Assets.Editor.Avalonia/Views/ImageDictionary.axaml`
  3. `sources/editor/Stride.Core.Assets.Editor.Wpf/View/CommonResources.xaml` (328 l) -> `sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/CommonResources.axaml`
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestVectorImageDictionariesAndCommonResourcesIntegration.cs` (RED -> GREEN).

---

### 🔹 Fatia 2 (slice-2): ExpressionDark Global Themes, TableView & TableflowView Styles
- **Builder:** `ExpressionDark Themes & Table Styles Builder`
- **Reviewer:** `ExpressionDark Themes & Table Styles Reviewer`
- **Escopo Técnico (7 arquivos / 870 linhas originais legadas):**
  1. `sources/editor/Stride.Core.Assets.Editor.Wpf/Themes/ExpressionDark/Resources/TableView.ExpressionDark.normalcolor.Graphics.xaml` (267 l) -> `sources/editor/Stride.Core.Assets.Editor.Avalonia/Themes/ExpressionDark/Resources/TableView.ExpressionDark.normalcolor.Graphics.axaml`
  2. `sources/editor/Stride.Core.Assets.Editor.Wpf/Themes/ExpressionDark/Resources/Common.Resources.xaml` (245 l) -> `sources/editor/Stride.Core.Assets.Editor.Avalonia/Themes/ExpressionDark/Resources/Common.Resources.axaml`
  3. `sources/editor/Stride.Core.Assets.Editor.Wpf/Themes/generic.xaml` (109 l) -> `sources/editor/Stride.Core.Assets.Editor.Avalonia/Themes/Generic.axaml`
  4. `sources/editor/Stride.Core.Assets.Editor.Wpf/Themes/ExpressionDark/TableflowView.ExpressionDark.normalcolor.xaml` (105 l) -> `sources/editor/Stride.Core.Assets.Editor.Avalonia/Themes/ExpressionDark/TableflowView.ExpressionDark.normalcolor.axaml`
  5. `sources/editor/Stride.Core.Assets.Editor.Wpf/Themes/ExpressionDark/TableflowView.GridElementTemplates.xaml` (91 l) -> `sources/editor/Stride.Core.Assets.Editor.Avalonia/Themes/ExpressionDark/TableflowView.GridElementTemplates.axaml`
  6. `sources/editor/Stride.Core.Assets.Editor.Wpf/Themes/ExpressionDark/Resources/ExpressionDark.normalcolor.Resources.xaml` (41 l) -> `sources/editor/Stride.Core.Assets.Editor.Avalonia/Themes/ExpressionDark/Resources/ExpressionDark.normalcolor.Resources.axaml`
  7. `sources/editor/Stride.Core.Assets.Editor.Wpf/Themes/ThemeSelector.xaml` (12 l) -> `sources/editor/Stride.Core.Assets.Editor.Avalonia/Themes/ThemeSelector.axaml`
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestExpressionDarkThemesAndTableStylesIntegration.cs` (RED -> GREEN).

---

### 🔹 Fatia 3 (slice-3): Presentation Themes, Editor Templates & Visual Scripting/Compositor Resources
- **Builder:** `Presentation Themes & Editor Templates Builder`
- **Reviewer:** `Presentation Themes & Editor Templates Reviewer`
- **Escopo Técnico (7 arquivos / 830 linhas originais legadas):**
  1. `sources/editor/Stride.Assets.Presentation.Wpf/Themes/Generic.xaml` (457 l) -> `sources/editor/Stride.Assets.Editor.Avalonia/Themes/Generic.axaml`
  2. `sources/editor/Stride.Assets.Presentation.Wpf/AssetEditors/ScriptEditor/Resources/ThemeScriptEditor.xaml` (267 l) -> `sources/editor/Stride.Assets.Editor.Avalonia/AssetEditors/ScriptEditor/Resources/ThemeScriptEditor.axaml`
  3. `sources/editor/Stride.Assets.Presentation.Wpf/View/VisualScriptingTemplates.xaml` (30 l) -> `sources/editor/Stride.Assets.Editor.Avalonia/Views/VisualScriptingTemplates.axaml`
  4. `sources/editor/Stride.Assets.Presentation.Wpf/AssetEditors/ScriptEditor/Resources/Icons.xaml` (28 l) -> `sources/editor/Stride.Assets.Editor.Avalonia/AssetEditors/ScriptEditor/Resources/Icons.axaml`
  5. `sources/editor/Stride.Assets.Presentation.Wpf/AssetEditors/VisualScriptEditor/Views/GraphTemplates.xaml` (22 l) -> `sources/editor/Stride.Assets.Editor.Avalonia/AssetEditors/VisualScriptEditor/Views/GraphTemplates.axaml`
  6. `sources/editor/Stride.Editor.Wpf/Themes/Generic.xaml` (15 l) -> `sources/editor/Stride.Editor.Avalonia/Themes/Generic.axaml`
  7. `sources/editor/Stride.Assets.Presentation.Wpf/View/GraphicsCompositorTemplates.xaml` (11 l) -> `sources/editor/Stride.Assets.Editor.Avalonia/Views/GraphicsCompositorTemplates.axaml`
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestPresentationThemesAndEditorTemplatesIntegration.cs` (RED -> GREEN).

---

## 🛡️ Regras e Protocolos Rígidos
1. Respostas em **PT-BR**.
2. **Sem Gambiarras:** 85-100% de paridade estrita com o legado Stride, sem stubs falsos ou recursos faltando.
3. **Protocolo Obrigatório de Bugs / TDD Iron Law:** Criação obrigatória de teste integrado que comprove a falha inicial (RED) antes de qualquer código de produção, concluindo apenas com 100% GREEN.
4. **Despacho Ordenado:** Despachar simultaneamente os 3 Builders via `invoke_subagent`. Os Reviewers só entram após a entrega dos 3 testes verdes.
