# 🏛️ MASTER BLUEPRINT: Marco P22 - Entity Hierarchy, Code Solution Management & Stride Default Assets Plugin

**Epic Goal:** Concluir a migração nativa do ecossistema de Hierarquia de Entidades e Componentes (`EntityViewModel`), Gerenciamento de Código e Solução C# (`CodeViewModel`) e o Plugin Central de Registro de Assets Padrão (`StrideDefaultAssetsPlugin`) do Stride para Linux 100% Nativo (.NET 10 + Avalonia 12 + Vulkan RADV), garantindo a paridade integral com o editor original do Stride, sincronização quântica de componentes de cena e registro canônico de todos os tipos de assets no Game Studio.

---

## 🎯 Divisão de Fatias Verticais (3x3 Arquitetura)

### 🔹 Fatia 1 (slice-1): Entity Hierarchy ViewModel & Component Container
* **Builder:** `Entity Hierarchy & Components Builder`
* **Escopo:**
  1. `sources/editor/Stride.Assets.Presentation/ViewModels/EntityViewModel.cs` (expansão fiel de 107 linhas para ~448 linhas a partir de `sources/editor/Stride.Assets.Presentation.Wpf/AssetEditors/EntityHierarchyEditor/ViewModels/EntityViewModel.cs`).
  2. Implementação integral de contratos: `IEditorDesignPartViewModel<EntityDesign, Entity>`, `IIsEditableViewModel`, `IAddChildrenPropertiesProviderViewModel`.
  3. Bindings de grafo quântico para `Name`, `Components` e nós de transformação.
  4. Suporte a propagação de mudanças de hierarquia via `EntityHierarchyElementChangePropagator`, gerenciamento de prefabs (`sourcePrefab`) e comandos de componentes.
* **Protocolo Obrigatório de Bugs / TDD Iron Law:**
  - Criar `sources/editor/Stride.Core.Assets.Editor.Tests/TestEntityHierarchyAndComponentsIntegration.cs` que deve **FALHAR (RED)** antes da implementação dos tipos e passar com 100% de sucesso (**GREEN**).

---

### 🔹 Fatia 2 (slice-2): Code Solution ViewModel & Workspace Synchronization
* **Builder:** `Code Solution & Workspace Synchronization Builder`
* **Escopo:**
  1. `sources/editor/Stride.Assets.Editor/ViewModels/CodeViewModel.cs` (expansão fiel de 42 linhas para ~400 linhas a partir de `sources/editor/Stride.Assets.Presentation.Wpf/ViewModel/CodeViewModel.cs`).
  2. Integração assíncrona desacoplada de WPF com `ProjectWatcher` e `RoslynWorkspace`.
  3. Rastreamento e processamento do fluxo de alterações em lote via `IAsyncEnumerable<List<AssemblyChangedEvent>> BatchChange`.
  4. Gerenciamento de arquivos `.cs`, sincronização com `SessionViewModel`, controle de tamanho de fonte (`ScriptEditorSettings.FontSize`) e formatação de sintaxe de tooltips desacoplada de `System.Windows.Media`.
* **Protocolo Obrigatório de Bugs / TDD Iron Law:**
  - Criar `sources/editor/Stride.Core.Assets.Editor.Tests/TestCodeSolutionAndWorkspaceSynchronizationIntegration.cs` que deve **FALHAR (RED)** antes da implementação e passar com 100% de sucesso (**GREEN**).

---

### 🔹 Fatia 3 (slice-3): Stride Default Assets Plugin & Factory Registration
* **Builder:** `Stride Default Assets Plugin & Registration Builder`
* **Escopo:**
  1. `sources/editor/Stride.Assets.Presentation/StrideDefaultAssetsPlugin.cs` (expansão fiel de 31 linhas para ~450 linhas a partir de `sources/editor/Stride.Assets.Presentation.Wpf/StrideDefaultAssetsPlugin.cs`).
  2. Registro central de plugins de assets do Stride: `ComponentTypeComparer`, factories de componentes de entidade, templates de inicialização, registradores de preview (`IAssetPreviewViewModel`), comandos quânticos de nós, registradores de compiladores e inicializadores de editor.
  3. Desacoplamento limpo de referências exclusivas de WPF (`System.Windows`, etc.), mantendo conformidade 100% com Linux/.NET 10.
* **Protocolo Obrigatório de Bugs / TDD Iron Law:**
  - Criar `sources/editor/Stride.Core.Assets.Editor.Tests/TestStrideDefaultAssetsPluginIntegration.cs` que deve **FALHAR (RED)** antes da implementação e passar com 100% de sucesso (**GREEN**).

---

## 🚦 Critérios de Aceite e Homologação
1. **0 Erros de Compilação (Clean Build)** em todos os projetos de editor e Avalonia (.NET 10).
2. **Suíte Global de Testes:** 328 + novos testes passando (**100% GREEN, 0 falhas**).
3. **Paridade Arquitetural Estrita:** 85% a 100% de linhas de código preservadas em relação ao original Stride.
4. **Sem Gambiarras:** Código limpo, desacoplado de Win32/WPF, com suporte nativo a Linux.
5. **Aprovação Unânime do Gauntlet Loop:** Reviewers só convocados após a entrega dos 3 Builders.
