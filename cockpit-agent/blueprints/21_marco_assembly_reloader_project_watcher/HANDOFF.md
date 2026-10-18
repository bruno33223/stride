# 🤝 HANDOFF: Marco P21 - Live Assembly Reloading, Project Watcher & Shader Compiler Server

**Epic:** Portabilidade contínua do Stride Studio para Linux 100% Nativo (Avalonia 12 + Vulkan RADV + .NET 10)  
**Status do Marco P21:** **100% CONCLUÍDO & HOMOLOGADO (328/328 testes xUnit GREEN)**  
**Próximo Marco:** **Marco P22**

---

## 📦 Entregas Homologadas no Marco P21

1. **Fatia 1 (slice-1): Live Assembly Reloading Engine**
   - `ReloadAssembliesOperation.cs` (142 linhas, operação transacional com registro em `AssemblyRegistry`)
   - `GameStudioAssemblyReloader.cs` (491 linhas, serialização Yaml quântica com `UnloadingVisitor`, fixup context e hot-reload)
   - `FixupAssetContext.cs` (23 linhas) e sincronização com `SessionViewModel`, `ISessionViewModel` e `AssetViewModel`
   - Suíte de testes: `TestLiveAssemblyReloadingIntegration.cs` (6 testes GREEN)

2. **Fatia 2 (slice-2): Project Watcher & Filesystem Synchronization**
   - `ProjectWatcher.cs` (501 linhas, substituição completa do stub, pipeline TPL Dataflow, debounce 500ms e cancelamento limpo)
   - Ajustes em `PackageLoadedAssembly.cs`, `PackageViewModel.cs` e `ProjectViewModel.cs`
   - Suíte de testes: `TestProjectWatcherIntegration.cs` (5 testes GREEN)

3. **Fatia 3 (slice-3): Effect Compiler Server Session & Shader Compilation IPC**
   - `EffectCompilerServerSession.cs` (329 linhas, IPC desacoplado de WPF, `RemoteEffectCompilerEffectRequested`, persistência em `EffectLogStore`)
   - `RouterHelper.cs` (132 linhas, execução nativa de `Stride.ConnectionRouter` no Linux)
   - Sincronização em `SessionViewModel.cs`, `EffectLogViewModel.cs` e `AssetViewModel.cs`
   - Suíte de testes: `TestEffectCompilerServerSessionIntegration.cs` (6 testes GREEN)

---

## 📊 Métricas Globais Atualizadas

- **Tarefas Mapeadas Concluídas:** **536 de 1.022 (52,45%)**
- **LOC Legado Original Portado (Antes):** **68.220 de 110.247 linhas (61,88%)**
- **LOC Nativo Linux Gerado (Depois):** **65.980 linhas**
- **Taxa de Paridade Média:** **96,72%**
- **Suíte Global de Testes xUnit (.NET 10):** **328 de 328 testes APROVADOS (100% GREEN, 0 falhas, 0 regressões)**
