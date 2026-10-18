# 🏛️ MASTER BLUEPRINT: Marco P20 - Visual Scripting & Script Editor

**Epic Goal:** Concluir a migração nativa do ecossistema de Scripts C# e Visual Scripting do Stride para Linux 100% Nativo (.NET 10 + Avalonia 12 + Vulkan RADV), cobrindo viewmodels de arquivos de código-fonte, gerador de templates com diálogo nativo de seleção/nomeação (`ScriptNameWindow`), scripts utilitários modelos de gameplay/câmera/física, viewmodels de método e diagnóstico do editor de scripts com Roslyn, e a infraestrutura gráfica de Visual Scripting (vértices de bloco, arestas de ligação, slots e conversores de exibição).

---

## 🎯 Divisão de Fatias Verticais (3x3 Arquitetura)

### 🔹 Fatia 1 (slice-1): Script Templates, Generators & Dialogs
* **Builder:** `Script Templates & Dialogs Builder`
* **Escopo:**
  1. `sources/editor/Stride.Assets.Editor/Templates/ScriptTemplateGenerator.cs` (~158 linhas, suporte a `ScriptSourceFileAsset`, substituição de macros `##Namespace##` e `##Scriptname##`, criação do asset com `ObjectFactoryRegistry.NewInstance`, salvamento condicional e diálogo `ScriptNameWindow`).
  2. `sources/editor/Stride.Assets.Editor/Templates/TemplateDotNetNewDescription.cs` (~29 linhas, extensão de `TemplateAssetDescription` para suporte a templates dotnet new).
  3. Diálogo Avalonia 12 nativo `sources/editor/Stride.Assets.Editor.Avalonia/Views/ScriptNameWindow.axaml` e `.axaml.cs` (~146 linhas combinadas, diálogo modal para informar nome de classe e namespace, validação de identificador C# e seleção de template).
  4. Preservação e cópia dos scripts modelos de template de gameplay em `sources/editor/Stride.Assets.Editor/Templates/Assets/Scripts/`:
     - Câmeras: `FreeCamera.cs` (305 l), `BasicCameraController.cs` (280 l), `FpsCamera.cs` (229 l), `OrbitCamera.cs` (202 l), `SideScrollingCamera.cs` (138 l).
     - Física & Movimentação: `PlayerController.cs` (168 l), `DebugShapesRender.cs` (59 l).
     - Eventos & Utilitários: `SceneStreaming.cs` (159 l), `GameProfiler.cs` (134 l), `EventBroadcaster.cs` (60 l), `EventListener.cs` (39 l).
     - Scripts base: `DefaultSyncScript.cs` (25 l), `DefaultAsyncScript.cs` (24 l), `DefaultStartupScript.cs` (20 l).
* **Protocolo Obrigatório de Bugs / TDD Iron Law:**
  - Criar `sources/editor/Stride.Core.Assets.Editor.Tests/TestScriptTemplatesAndGeneratorsIntegration.cs` que deve **FALHAR (RED)** antes da implementação dos tipos/diálogos e passar com 100% de sucesso (**GREEN**).

---

### 🔹 Fatia 2 (slice-2): Script Editor ViewModels, Workspace & Diagnostics
* **Builder:** `Script Editor ViewModels & Workspace Builder`
* **Escopo:**
  1. `sources/editor/Stride.Assets.Editor/ViewModels/ScriptSourceFileAssetViewModel.cs` (~501 linhas adaptadas, herança de `SourceCodeAssetViewModel` / `SourceCodeViewModel<ScriptSourceFileAsset>`, gerenciamento de texto, sincronização com disco e controle de alterações externas).
  2. `sources/editor/Stride.Assets.Editor/AssetEditors/ScriptEditor/ScriptEditorViewModel.cs` (~114 linhas, ciclo de vida do editor de script, suporte a comandos de compilação/execução, Undo/Redo e status de diagnóstico).
  3. `sources/editor/Stride.Assets.Editor/AssetEditors/ScriptEditor/RoslynWorkspace.cs` e `RoslynHost.cs` (~569 linhas combinadas, abstração da workspace Roslyn para análise de código, resolução de metadados e diagnóstico sintático/semântico).
  4. `sources/editor/Stride.Assets.Editor/AssetEditors/ScriptEditor/MetadataUtil.cs` (~51 linhas, utilitários de metadados e referências).
  5. Conversores de editor de script:
     - `CodeActionToGlyphConverter.cs` (~39 linhas)
     - `CodeActionsConverter.cs` (~35 linhas)
     - `ClassificationHighlightColorsDark.cs` (~33 linhas)
     - `ScriptToEntityAndScript.cs` (~18 linhas)
  6. Validação de `Thumbnails/ScriptSourceFileThumbnailCompiler.cs` (~18 linhas).
* **Protocolo Obrigatório de Bugs / TDD Iron Law:**
  - Criar `sources/editor/Stride.Core.Assets.Editor.Tests/TestScriptEditorViewModelsAndWorkspaceIntegration.cs` que deve **FALHAR (RED)** antes da implementação/integração e passar com 100% de sucesso (**GREEN**).

---

### 🔹 Fatia 3 (slice-3): Visual Scripting Graph ViewModels, Converters & Templates
* **Builder:** `Visual Scripting Graph Builder`
* **Escopo:**
  1. `sources/editor/Stride.Assets.Editor/ViewModels/VisualScriptViewModel.cs` (~103 linhas, herança de `AssetViewModel<VisualScriptAsset>`, anotação `[AssetViewModel<VisualScriptAsset>]`, gerenciamento de métodos e slots).
  2. `sources/editor/Stride.Assets.Editor/ViewModels/VisualScriptMethodViewModel.cs` (~318 linhas, modelagem de métodos de visual scripting, parâmetros, blocos e conexões).
  3. `sources/editor/Stride.Assets.Editor/AssetEditors/VisualScriptEditor/VisualScriptEditorViewModel.Diagnostics.cs` (~226 linhas, diagnóstico e validação de nós de script visual).
  4. Elementos de grafo:
     - `BlockNodeVertex.cs` (~189 linhas, vértice de bloco lógico no grafo visual)
     - `LinkNodeEdge.cs` (~24 linhas, aresta de conexão de fluxo de dados/execução)
     - `VisualScriptSlotViewModel.cs` (~46 linhas, portas de entrada e saída)
     - `VisualScriptFunctionParameterViewModel.cs` (~11 linhas)
  5. Conversores e Templates de Visual Scripting:
     - `MethodToDisplayName.cs` (~87 linhas)
     - `AvailableVariableReferenceValueConverter.cs` (~43 linhas)
     - `VisualScriptSlotBrush.cs` (~40 linhas)
     - Templates AXAML: `VisualScriptingTemplates.axaml` e `GraphTemplates.axaml` em `Stride.Assets.Editor.Avalonia/Views/`.
* **Protocolo Obrigatório de Bugs / TDD Iron Law:**
  - Criar `sources/editor/Stride.Core.Assets.Editor.Tests/TestVisualScriptingGraphViewModelsAndConvertersIntegration.cs` que deve **FALHAR (RED)** antes da implementação e passar com 100% de sucesso (**GREEN**).

---

## 🚦 Critérios de Aceite e Homologação
1. **0 Erros de Compilação (Clean Build)** em todos os projetos de editor e Avalonia (.NET 10).
2. **Suíte Global de Testes:** 295 + novos testes passando (**100% GREEN, 0 falhas**).
3. **Paridade Arquitetural Estrita:** 85% a 100% de linhas de código preservadas em relação ao original Stride.
4. **Sem Gambiarras:** Código limpo, desacoplado de dependências Win32/WPF, com suporte nativo a Linux.
5. **Aprovação Unânime do Gauntlet Loop:** Reviewers só convocados após a entrega dos 3 Builders.
