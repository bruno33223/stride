# 🏛️ MASTER BLUEPRINT: Marco P27 - Game Studio Session Architecture, Session Explorer & Solution Services

**Epic:** Migração integral e desacoplamento da arquitetura central de sessão do Stride Game Studio (`SessionObjectViewModel`, ciclo de vida de sessão, nós Quantum de sessão), componentes do Session Explorer (`SessionExplorerHelper`, propriedades de objetos de sessão, templates de abertura/criação de sessão e serviço de IDE/solução) e rastreadores de dependências, tags e correção de referências de assets para Linux 100% Nativo (Avalonia 12 + Vulkan RADV + .NET 10).

---

## 🎯 Metas & Regras Inegociáveis
1. **Respostas em PT-BR**.
2. **Sem Gambiarras**: 85% a 100% de paridade estrita com o legado original Stride WPF, desacoplando totalmente de Win32/WPF, Direct3D ou classes proprietárias do Windows.
3. **Protocolo Obrigatório de Bugs / TDD Iron Law**:
   - Todo Builder DEVE OBRIGATORIAMENTE criar um teste integrado xUnit que FALHE (RED) antes de qualquer implementação de código.
   - O trabalho só é concluído quando o teste passar com 100% de sucesso (GREEN).
4. **Papel Exclusivo de Tech Leader**: O orquestrador despacha simultaneamente exatamente 3 Builders via `invoke_subagent`. Os Reviewers NUNCA são despachados concorrentemente com os Builders.
5. **Timer de 3 Horas**: Mantido ativo ininterruptamente.

---

## 🧩 Fatias Verticais (Vertical Slices)

### 🔹 Fatia 1 (slice-1): Core Session Contracts, SessionObject & Quantum Node Updaters
- **Builder Role:** `Session Core Architecture Builder`
- **Escopo de Arquivos:**
  - `sources/editor/Stride.Core.Assets.Editor.Wpf/ViewModel/ISessionObjectViewModel.cs` (23 l)
  - `sources/editor/Stride.Core.Assets.Editor.Wpf/ViewModel/SessionObjectViewModel.cs` (122 l)
  - `sources/editor/Stride.Core.Assets.Editor.Wpf/ViewModel/SessionNodeContainer.cs` (28 l)
  - `sources/editor/Stride.Core.Assets.Editor.Wpf/ViewModel/NewSessionParameters.cs` (16 l)
  - `sources/editor/Stride.Core.Assets.Editor.Wpf/ViewModel/SessionStateChangedEventArgs.cs` (11 l)
  - `sources/editor/Stride.Core.Assets.Editor.Wpf/Quantum/NodePresenters/Keys/SessionData.cs` (15 l)
  - `sources/editor/Stride.Core.Assets.Editor.Wpf/Quantum/NodePresenters/Updaters/SessionNodeUpdater.cs` (42 l)
  - `sources/editor/Stride.Core.Assets.Editor.Wpf/Quantum/NodePresenters/Commands/SessionInitialDirectoryProvider.cs` (41 l)
- **Destino Migrado:**
  - `sources/editor/Stride.Core.Assets.Editor/ViewModels/ISessionObjectViewModel.cs`
  - `sources/editor/Stride.Core.Assets.Editor/ViewModels/SessionObjectViewModel.cs`
  - `sources/editor/Stride.Core.Assets.Editor/ViewModels/SessionNodeContainer.cs`
  - `sources/editor/Stride.Core.Assets.Editor/ViewModels/NewSessionParameters.cs`
  - `sources/editor/Stride.Core.Assets.Editor/ViewModels/SessionStateChangedEventArgs.cs`
  - `sources/editor/Stride.Core.Assets.Editor/Quantum/NodePresenters/Keys/SessionData.cs`
  - `sources/editor/Stride.Core.Assets.Editor/Quantum/NodePresenters/Updaters/SessionNodeUpdater.cs`
  - `sources/editor/Stride.Core.Assets.Editor/Quantum/NodePresenters/Commands/SessionInitialDirectoryProvider.cs`
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestSessionArchitectureAndQuantumIntegration.cs`
- **Objetivo:** Estabelecer a base fundamental para objetos gerenciados por sessão, sincronização com contêineres de nós Quantum e provedores de diretório inicial de sessão.

---

### 🔹 Fatia 2 (slice-2): Session Explorer Helper, Properties & Template Collection ViewModels
- **Builder Role:** `Session Explorer & Properties Builder`
- **Escopo de Arquivos:**
  - `sources/editor/Stride.Core.Assets.Editor.Wpf/View/SessionExplorerHelper.cs` (159 l)
  - `sources/editor/Stride.Core.Assets.Editor.Wpf/Components/Properties/SessionObjectPropertiesViewModel.cs` (153 l)
  - `sources/editor/Stride.Core.Assets.Editor.Wpf/Components/TemplateDescriptions/ViewModels/NewOrOpenSessionTemplateCollectionViewModel.cs` (113 l)
  - `sources/editor/Stride.Core.Assets.Editor.Wpf/Services/VisualStudioService.cs` (67 l) -> desacoplar para launcher agnóstico de solução/IDE no Linux
  - `sources/editor/Stride.Assets.Presentation.Wpf/ViewModel/NavigationViewModel.cs` (18 l)
- **Destino Migrado:**
  - `sources/editor/Stride.Core.Assets.Editor/View/SessionExplorerHelper.cs`
  - `sources/editor/Stride.Core.Assets.Editor/Components/Properties/SessionObjectPropertiesViewModel.cs`
  - `sources/editor/Stride.Core.Assets.Editor/Components/TemplateDescriptions/ViewModels/NewOrOpenSessionTemplateCollectionViewModel.cs`
  - `sources/editor/Stride.Core.Assets.Editor/Services/VisualStudioService.cs`
  - `sources/editor/Stride.Assets.Presentation/ViewModels/NavigationViewModel.cs`
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestSessionExplorerAndPropertiesIntegration.cs`
- **Objetivo:** Fornecer navegação de árvore de sessão, visualização e edição de propriedades de objetos da sessão, templates de novas soluções e integração com ambiente de desenvolvimento.

---

### 🔹 Fatia 3 (slice-3): Asset Dependency Tracking, Tags & Fix References ViewModels
- **Builder Role:** `Asset Dependencies & Fix References Builder`
- **Escopo de Arquivos:**
  - `sources/editor/Stride.Core.Assets.Editor.Wpf/ViewModel/AssetDependenciesViewModel.cs` (268 l)
  - `sources/editor/Stride.Core.Assets.Editor.Wpf/ViewModel/AssetSourceTrackerViewModel.cs` (198 l)
  - `sources/editor/Stride.Core.Assets.Editor.Wpf/ViewModel/TagsViewModel.cs` (234 l)
  - `sources/editor/Stride.Core.Assets.Editor.Wpf/Components/FixReferences/FixReferencesViewModel.cs` (299 l)
- **Destino Migrado:**
  - `sources/editor/Stride.Core.Assets.Editor/ViewModels/AssetDependenciesViewModel.cs`
  - `sources/editor/Stride.Core.Assets.Editor/ViewModels/AssetSourceTrackerViewModel.cs`
  - `sources/editor/Stride.Core.Assets.Editor/ViewModels/TagsViewModel.cs`
  - `sources/editor/Stride.Core.Assets.Editor/Components/FixReferences/FixReferencesViewModel.cs`
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestAssetDependenciesAndFixReferencesIntegration.cs`
- **Objetivo:** Implementar rastreamento bidirecional de dependências de assets em árvore e lista, rastreamento de arquivos fonte externos, gestão de tags e assistente de correção e religamento de referências órfãs.

---

## 🚦 Critérios de Aceite Gauntlet Loop
- Paridade lógica estrita (85% a 100%) com os originais WPF.
- Desacoplamento completo de Win32, WPF e Direct3D.
- Compilação 100% limpa no .NET 10 (0 erros).
- TDD Iron Law: 100% dos testes integrados passando (GREEN).
