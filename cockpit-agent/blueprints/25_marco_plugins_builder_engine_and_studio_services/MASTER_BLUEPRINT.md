# 🏛️ MASTER BLUEPRINT: Marco P25 - Plugin Architecture, Asset Builder Engine & Studio Services

**Epic:** Migração integral e desacoplamento da arquitetura de plugins do editor, motor assíncrono de build de assets (`AssetBuilderService`, `GameStudioBuilderService`, `GameStudioDatabase`, unidades de build) e serviços centrais de seleção e documentação do editor Stride do legado Windows/WPF para Linux 100% Nativo (Avalonia 12 + Vulkan RADV + .NET 10).

---

## 🎯 Metas & Regras Inegociáveis
1. **Respostas em PT-BR**.
2. **Sem Gambiarras**: 85% a 100% de paridade estrita com o legado original Stride WPF, desacoplando totalmente de Win32, COM ou dependências do Windows.
3. **Protocolo Obrigatório de Bugs / TDD Iron Law**:
   - Todo Builder DEVE OBRIGATORIAMENTE criar um teste integrado xUnit que FALHE (RED) antes de qualquer implementação de código.
   - O trabalho só é concluído quando o teste passar com 100% de sucesso (GREEN).
4. **Papel Exclusivo de Tech Leader**: O orquestrador despacha simultaneamente exatamente 3 Builders via `invoke_subagent`. Os Reviewers NUNCA são despachados concorrentemente com os Builders.
5. **Timer de 3 Horas**: Mantido ativo ininterruptamente.

---

## 🧩 Fatias Verticais (Vertical Slices)

### 🔹 Fatia 1 (slice-1): Core Plugin Architecture & Registration Services
- **Builder Role:** `Plugin Architecture Builder`
- **Escopo de Arquivos:**
  - `sources/editor/Stride.Core.Assets.Editor/Services/IAssetsPluginService.cs` (35 l)
  - `sources/editor/Stride.Core.Assets.Editor/Services/AssetsPlugin.cs` (66 l)
  - `sources/editor/Stride.Core.Assets.Editor/Services/AssetsEditorPlugin.cs` (64 l)
  - `sources/editor/Stride.Core.Assets.Editor/CoreAssetsEditorPlugin.cs` (82 l)
  - `sources/editor/Stride.Assets.Editor/StrideAssetsPlugin.cs` (122 l)
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestPluginArchitectureAndLifecycleIntegration.cs`
- **Objetivo:** Registro, descoberta de assemblies de plugins, ciclo de vida e extensão limpa de tipos de assets e sessões sem amarras de interface de usuário WPF.

---

### 🔹 Fatia 2 (slice-2): Asset Builder Engine, Build Units & Studio Database Services
- **Builder Role:** `Asset Builder Engine Builder`
- **Escopo de Arquivos:**
  - `sources/editor/Stride.Core.Assets.Editor/Services/IBuildService.cs` (17 l)
  - `sources/editor/Stride.Core.Assets.Editor/Services/AssetBuilderService.cs` (129 l)
  - `sources/editor/Stride.Core.Assets.Editor/Services/AssetBuildUnit.cs` (97 l)
  - `sources/editor/Stride.Core.Assets.Editor/Services/AssetBuiltEventArgs.cs` (35 l)
  - `sources/editor/Stride.Core.Assets.Editor/Services/QueuePosition.cs` (19 l)
  - `sources/editor/Stride.Assets.Editor/Build/GameStudioBuilderService.cs` (143 l)
  - `sources/editor/Stride.Assets.Editor/Build/GameStudioDatabase.cs` (143 l)
  - `sources/editor/Stride.Assets.Editor/Build/DefaultAssetBuilderPriorities.cs` (13 l)
  - `sources/editor/Stride.Assets.Editor/Build/PrecompiledAssetBuildUnit.cs` (37 l)
  - `sources/editor/Stride.Assets.Editor/Build/EditorGameBuildUnit.cs` (35 l)
  - `sources/editor/Stride.Assets.Editor/Build/AnonymousAssetBuildUnit.cs` (26 l)
  - `sources/editor/Stride.Assets.Editor/Build/IGameSettingsAccessor.cs` (20 l)
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestAssetBuilderEngineIntegration.cs`
- **Objetivo:** Pipeline de compilação em segundo plano de assets e shaders, banco de dados de compilação do Game Studio e orquestração de prioridades assíncronas.

---

### 🔹 Fatia 3 (slice-3): Studio Selection & Documentation Services
- **Builder Role:** `Studio Selection & Services Builder`
- **Escopo de Arquivos:**
  - `sources/editor/Stride.Core.Assets.Editor/Services/SelectionService.cs` (210 l)
  - `sources/editor/Stride.Core.Assets.Editor/Services/SelectionState.cs` (144 l)
  - `sources/editor/Stride.Core.Assets.Editor/Services/SelectionOperation.cs` (66 l)
  - `sources/editor/Stride.Core.Assets.Editor/Services/SelectionScope.cs` (41 l)
  - `sources/editor/Stride.Core.Assets.Editor/Services/UserDocumentationService.cs` (270 l)
  - `sources/editor/Stride.Core.Assets.Editor/Services/CodeEditorOpenerService.cs` (85 l)
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestStudioSelectionAndServicesIntegration.cs`
- **Objetivo:** Rastreamento transacional e multiescopo de seleção de objetos no editor (`SelectionService`), resolução de URLs de documentação de componentes e abertura agnóstica de editores de código (VS Code, JetBrains Rider) em Linux.

---

## 🚦 Critérios de Aceite Gauntlet Loop
- Paridade lógica de 85% a 100% com o legado WPF original.
- Compilação 100% limpa (0 erros, 0 warnings novos).
- Todos os testes de integração novos e existentes passando com 100% de sucesso (GREEN).
- Execução sequencial dos testes ou respeito ao isolamento quântico.
