# 🏛️ MASTER BLUEPRINT: Marco P21 - Live Assembly Reloading, Project Watcher & Shader Compiler Server

**Epic Goal:** Concluir a migração nativa do ecossistema de Live Assembly Reloading, Sincronização de Filesystem do Projeto (`ProjectWatcher`) e Sessão IPC do Compilador de Shaders/Efeitos (`EffectCompilerServerSession`) do Stride para Linux 100% Nativo (.NET 10 + Avalonia 12 + Vulkan RADV), garantindo que o Game Studio reaja a alterações de código C#, recompile e recarregue tipos dinâmicos em tempo de execução via Quantum/Yaml sem reiniciar o editor, monitore o filesystem em background com debounce thread-safe e gerencie compilações assíncronas de efeitos.

---

## 🎯 Divisão de Fatias Verticais (3x3 Arquitetura)

### 🔹 Fatia 1 (slice-1): Live Assembly Reloading Engine
* **Builder:** `Live Assembly Reloading Builder`
* **Escopo:**
  1. `sources/editor/Stride.Assets.Editor/AssemblyReloading/ReloadAssembliesOperation.cs` (~131 linhas, herança de `DirtyingOperation`, manuseio de `AssemblyContainer`, `PackageLoadedAssembly` e chamadas `UnloadAssemblies` / `LoadAssemblies`).
  2. `sources/editor/Stride.Assets.Editor/AssemblyReloading/GameStudioAssemblyReloader.cs` (~490 linhas, recarregamento a quente de assemblies do projeto, serialização Yaml de tipos descarregados com `UnloadingVisitor`, gerenciamento de fixup context em `SessionViewModel`, Undo/Redo e re-resolução de tipos em tempo de execução).
* **Protocolo Obrigatório de Bugs / TDD Iron Law:**
  - Criar `sources/editor/Stride.Core.Assets.Editor.Tests/TestLiveAssemblyReloadingIntegration.cs` que deve **FALHAR (RED)** antes da implementação dos tipos e passar com 100% de sucesso (**GREEN**).

---

### 🔹 Fatia 2 (slice-2): Project Watcher & Filesystem Synchronization
* **Builder:** `Project Watcher & Filesystem Synchronization Builder`
* **Escopo:**
  1. `sources/editor/Stride.Assets.Editor/AssetEditors/ProjectWatcher.cs` (substituição integral do stub de 70 linhas pela versão completa e robusta de ~454 linhas).
  2. Monitoramento assíncrono via `BufferBlock<FileEvent>`, `DirectoryWatcher` e `SessionViewModel`.
  3. Suporte a `AssemblyChangedEvent`, `AssemblyChangeType` (Binary, Project, Source), debounce e bufferização de notificações de I/O em lote via `IAsyncEnumerable<List<AssemblyChangedEvent>> BatchChange`.
  4. Rastreamento e sincronização com `Microsoft.CodeAnalysis.Project` e `PackageLoadedAssembly`.
* **Protocolo Obrigatório de Bugs / TDD Iron Law:**
  - Criar `sources/editor/Stride.Core.Assets.Editor.Tests/TestProjectWatcherIntegration.cs` que deve **FALHAR (RED)** antes da implementação e passar com 100% de sucesso (**GREEN**).

---

### 🔹 Fatia 3 (slice-3): Effect Compiler Server Session & Shader Compilation IPC
* **Builder:** `Effect Compiler Server Session Builder`
* **Escopo:**
  1. `sources/editor/Stride.Assets.Editor/EffectCompilerServerSession.cs` (~314 linhas, desacoplado de dependências WPF/Win32).
  2. Gerenciamento de conexão com o `EffectCompilerServer` via `Stride.ConnectionRouter` e `Stride.Engine.Network`.
  3. Rastreamento de pacotes (`TrackedPackage`), escuta assíncrona de compilações de shaders (`EffectCompileRequest`) e atualização sincronizada do `EffectLogViewModel` no `SessionViewModel`.
  4. Utilização do `IDispatcherService` do Stride Core Presentation para notificações seguras de UI.
* **Protocolo Obrigatório de Bugs / TDD Iron Law:**
  - Criar `sources/editor/Stride.Core.Assets.Editor.Tests/TestEffectCompilerServerSessionIntegration.cs` que deve **FALHAR (RED)** antes da implementação e passar com 100% de sucesso (**GREEN**).

---

## 🚦 Critérios de Aceite e Homologação
1. **0 Erros de Compilação (Clean Build)** em todos os projetos de editor e Avalonia (.NET 10).
2. **Suíte Global de Testes:** 311 + novos testes passando (**100% GREEN, 0 falhas**).
3. **Paridade Arquitetural Estrita:** 85% a 100% de linhas de código preservadas em relação ao original Stride.
4. **Sem Gambiarras:** Código limpo, desacoplado de Win32/WPF, com suporte nativo a Linux.
5. **Aprovação Unânime do Gauntlet Loop:** Reviewers só convocados após a entrega dos 3 Builders.
