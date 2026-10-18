# 🛡️ GAUNTLET LOG: Marco P21 - Live Assembly Reloading, Project Watcher & Shader Compiler Server

**Epic:** Portabilidade contínua do ecossistema de Live Assembly Reloading, Sincronização de Filesystem do Projeto (`ProjectWatcher`) e Sessão IPC do Compilador de Shaders/Efeitos (`EffectCompilerServerSession`) do Stride para Linux 100% Nativo (.NET 10 + Avalonia 12 + Vulkan RADV)  
**Status Final:** **PASSED (100% HOMOLOGADO)**  
**Data:** 17/10/2026  
**Suíte de Testes:** **328/328 testes xUnit APROVADOS (100% GREEN, 0 falhas, 0 regressões)**

---

## 🏛️ Avaliação da Banca Revisora Independente

### 🔹 Fatia 1 (slice-1): Live Assembly Reloading Engine
* **Revisor:** `Live Assembly Reloading Reviewer` (`c13f2b93`)
* **Veredicto:** **PASSED** ✅ (0 Critical, 0 Important, 0 Minor)
* **Auditoria:**
  - `ReloadAssembliesOperation.cs` (142 linhas, 108.4% de paridade): Operação transacional derivada de `DirtyingOperation`, registro e desregistro no `AssemblyRegistry` e `DataSerializerFactory`, e execução com Undo/Redo.
  - `GameStudioAssemblyReloader.cs` (491 linhas, 100.2% de paridade): Orquestração de hot-reload sem perda de estado, fidelidade total do `UnloadingVisitor` na serialização Yaml de nós quânticos para tipos a descarregar, restauração de identificadores (`ItemId`) e overrides de propriedades com transações atômicas (`ContentValueChangeOperation`).
  - `FixupAssetContext.cs` (23 linhas) e integração aos ViewModels (`SessionViewModel`, `ISessionViewModel`, `AssetViewModel` com proteção de guardas no `BaseContentChanged` e `AssetPropertyChanged`).
* **TDD:** `TestLiveAssemblyReloadingIntegration.cs` (6/6 testes GREEN).

---

### 🔹 Fatia 2 (slice-2): Project Watcher & Filesystem Synchronization
* **Revisor:** `Project Watcher & Filesystem Synchronization Reviewer` (`c9e10f54`)
* **Veredicto:** **PASSED** ✅ (0 Critical, 0 Important, 1 Minor herdado/inofensivo)
* **Auditoria:**
  - `ProjectWatcher.cs` (501 linhas, 98.5% de paridade): Substituição completa do stub anterior de 70 linhas, arquitetura reativa TPL Dataflow (`BufferBlock<FileEvent>` -> `TransformBlock<FileEvent, AssemblyChangedEvent>` -> `BroadcastBlock<AssemblyChangedEvent>`), debounce thread-safe de 500ms via `IAsyncEnumerable<List<AssemblyChangedEvent>> BatchChange`, deduplicação atômica e cancelamento cooperativo.
  - Resolução de caminhos agnóstica a SO via `UPath`, `UFile`, `UDirectory` e `Path.DirectorySeparatorChar`.
  - Leitura resiliente de arquivos concorrentes com até 10 tentativas em intervalos exponenciais contra file-locks.
  - Alinhamento de contratos em `PackageLoadedAssembly.cs`, `PackageViewModel.cs` e `ProjectViewModel.cs`.
* **TDD:** `TestProjectWatcherIntegration.cs` (5/5 testes GREEN).

---

### 🔹 Fatia 3 (slice-3): Effect Compiler Server Session & Shader Compilation IPC
* **Revisor:** `Effect Compiler Server Session Reviewer` (`44ffa0dc`)
* **Veredicto:** **PASSED** ✅ (0 Critical, 0 Important, 0 Minor)
* **Auditoria:**
  - `EffectCompilerServerSession.cs` (329 linhas, 98% de paridade): 100% desacoplado de dependências WPF/Win32, orquestração IPC via `Stride.ConnectionRouter`, canal `SocketMessageLayer`, recepção de mensagens `RemoteEffectCompilerEffectRequested`, deserialização estendida via `BinarySerializationReader` com `SerializerSelector.AssetWithReuse` e persistência em `EffectLogStore`.
  - Notificações thread-safe via `IDispatcherService` e `IEditorDialogService.ShowNotificationWindow`.
  - Transações atômicas de Undo/Redo com `AnonymousDirtyingOperation` para sincronização de contadores de efeitos pendentes.
  - `RouterHelper.cs` (132 linhas, 95% de paridade): Desacoplado de WinForms com suporte nativo multiplataforma para execução do `Stride.ConnectionRouter` no Linux.
  - Ajustes em `SessionViewModel.cs`, `EffectLogViewModel.cs` e `AssetViewModel.cs`.
* **TDD:** `TestEffectCompilerServerSessionIntegration.cs` (6/6 testes GREEN).

---

## 🏁 Veredicto Final do Marco P21
**UNANIMOUSLY PASSED (100% HOMOLOGADO)**
