# 📋 MASTER BLUEPRINT: Marco P24 - Advanced Copy/Paste Engine & Hierarchy Processors

**Epic:** Portabilidade integral do motor de área de transferência especializada (`CopyPasteService`), contratos de processamento (`ICopyProcessor`, `IPasteProcessor`, `IAssetPostPasteProcessor`) e de todos os processadores concretos de entidades, componentes, UI e cenas do Stride Game Studio para Linux 100% Nativo (Avalonia 12 + Vulkan RADV + .NET 10).  
**Status:** PLANEJADO & PRONTO PARA EXECUÇÃO  
**Data:** 17/10/2026  
**Taxa de Paridade Exigida:** 85% a 100% com o código original legado.  
**Regra TDD Iron Law:** Criação obrigatória de testes automatizados que falhem (RED) reproduzindo a ausência/falha antes de qualquer implementação de código, atingindo 100% GREEN sem regressões.  
**Tech Leader Rule:** NUNCA disparar reviewers simultaneamente com builders. Os 3 builders executam primeiro até completarem testes GREEN.

---

## 🎯 Fatias Verticais (3x3 Spec-Driven)

### 🔹 Fatia 1 (slice-1): Core Copy/Paste Engine, Contracts & Container Services
- **Responsável:** Builder 1 (Core CopyPaste Engine)
- **Escopo de Código:**
  - `sources/editor/Stride.Core.Assets.Editor/Services/CopyPaste/ICopyPasteService.cs` (105 l)
  - `sources/editor/Stride.Core.Assets.Editor/Services/CopyPaste/CopyPasteService.cs` (409 l): Motor central gerenciando serialização JSON/YAML, clipboard do sistema, encadeamento de processadores e transações atômicas.
  - `sources/editor/Stride.Core.Assets.Editor/Services/CopyPaste/CopyPasteData.cs` (67 l)
  - `sources/editor/Stride.Core.Assets.Editor/Services/CopyPaste/IPasteProcessor.cs` (60 l)
  - `sources/editor/Stride.Core.Assets.Editor/Services/CopyPaste/ICopyProcessor.cs` (30 l)
  - `sources/editor/Stride.Core.Assets.Editor/Services/CopyPaste/IAssetPostPasteProcessor.cs` (26 l)
  - `sources/editor/Stride.Core.Assets.Editor/Services/CopyPaste/IPasteResult.cs` (38 l)
  - `sources/editor/Stride.Core.Assets.Editor/Services/CopyPaste/PasteResult.cs` (37 l)
  - `sources/editor/Stride.Core.Assets.Editor/Components/CopyPasteProcessors/PasteProcessorBase.cs` (29 l)
  - `sources/editor/Stride.Core.Assets.Editor/Components/CopyPasteProcessors/AssetPostPasteProcessorBase.cs` (29 l)
- **TDD Test File:**
  - `sources/editor/Stride.Core.Assets.Editor.Tests/TestCopyPasteServiceCoreIntegration.cs`

---

### 🔹 Fatia 2 (slice-2): Entity Component & Entity Hierarchy Processors (DEF-01 Resolution)
- **Responsável:** Builder 2 (Entity Processors)
- **Escopo de Código:**
  - `sources/editor/Stride.Assets.Presentation/ViewModel/CopyPasteProcessors/EntityComponentCopyProcessor.cs` (50 l)
  - `sources/editor/Stride.Assets.Presentation/ViewModel/CopyPasteProcessors/EntityComponentPasteProcessor.cs` (95 l)
  - `sources/editor/Stride.Assets.Presentation/ViewModel/CopyPasteProcessors/EntityHierarchyPasteProcessor.cs` (54 l)
  - Atualização limpa em `StrideDefaultAssetsPlugin.cs` para registrar as instâncias reais dos processadores (resolvendo o apontamento DEF-01 do Marco P22).
- **TDD Test File:**
  - `sources/editor/Stride.Core.Assets.Editor.Tests/TestEntityCopyPasteProcessorsIntegration.cs`

---

### 🔹 Fatia 3 (slice-3): Asset Item, UI Hierarchy & Scene Post-Paste Processors
- **Responsável:** Builder 3 (Asset & Scene Processors)
- **Escopo de Código:**
  - `sources/editor/Stride.Core.Assets.Editor/Components/CopyPasteProcessors/AssetItemPasteProcessor.cs` (91 l)
  - `sources/editor/Stride.Assets.Presentation/ViewModel/CopyPasteProcessors/UIHierarchyPasteProcessor.cs` (43 l)
  - `sources/editor/Stride.Assets.Presentation/ViewModel/CopyPasteProcessors/ScenePostPasteProcessor.cs` (22 l)
- **TDD Test File:**
  - `sources/editor/Stride.Core.Assets.Editor.Tests/TestAssetItemAndHierarchyProcessorsIntegration.cs`

---

## 🛡️ Critérios de Aceite Gauntlet Loop
1. Paridade funcional estrita de 85% a 100% com o código legado original WPF.
2. Desacoplamento integral de `System.Windows.Clipboard` ou Win32 OLE — uso de abstrações multiplataforma limpas compatíveis com Avalonia 12 / Linux headless.
3. Tratamento defensivo de GUIDs em clonagens e pastes para prevenir colisões de IDs de entidades e nós quânticos.
4. Suíte xUnit executando 100% GREEN sem regressões.
