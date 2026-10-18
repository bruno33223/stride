# MASTER BLUEPRINT: 11_marco_property_grid_quantum

## 1. Visão Geral e Objetivo
Portar e consolidar integralmente o subsistema de **Property Grid Avançado, Quantum Node Presenters, Commands, Updaters, Template Providers e Validação de Propriedades Reativas** do Stride Game Studio para Linux Nativo (Avalonia 12 + Vulkan RADV), garantindo:
- Paridade estrita de comportamento e linhas de código com as implementações originais WPF (PROIBIDO resumir ou podar regras de negócio, fallbacks, cálculos numéricos e tratamentos de tipos Quantum).
- Eliminação total de dependências de Win32, WPF (`System.Windows`, `System.Windows.Media`) e Windows Forms.
- Ciclo TDD Iron Law: cada fatia deve possuir teste automatizado integrado demonstrando RED antes da implementação e GREEN ao finalizar.
- Validação no Gauntlet Loop com 3 Harsh Critics independentes auditando paridade cega, clean build e ausência de regressões.

---

## 2. Fatias Verticais de Implementação (Lote 3x3)

### Fatia 1 (slice-1): Quantum Node Commands, Updaters & Keys
- **Objetivo:** Portar os comandos de manipulação quântica (Script Components, Curvas, Pickup de Entidades e Cenas, Símbolos e Esqueletos), Updaters de nós (Navigation, Model Link, Entity Hierarchy, FXAA, Visual Script e SpriteFont) e chaves de dados quânticos associados.
- **Arquivos sob Lock Exclusivo (Builder 1):**
  - `sources/editor/Stride.Assets.Editor/Quantum/NodePresenters/Commands/AddNewScriptComponentCommand.cs` (Original: 98 linhas)
  - `sources/editor/Stride.Assets.Editor/Quantum/NodePresenters/Commands/EditCurveCommand.cs` (Original: 66 linhas)
  - `sources/editor/Stride.Assets.Editor/Quantum/NodePresenters/Commands/PickupEntityComponentCommand.cs` (Original: 60 linhas)
  - `sources/editor/Stride.Assets.Editor/Quantum/NodePresenters/Commands/PickupEntityCommand.cs` (Original: 56 linhas)
  - `sources/editor/Stride.Assets.Editor/Quantum/NodePresenters/Commands/PickupSceneObjectCommandBase.cs` (Original: 48 linhas)
  - `sources/editor/Stride.Assets.Editor/Quantum/NodePresenters/Commands/SetSymbolReferenceCommand.cs` (Original: 36 linhas)
  - `sources/editor/Stride.Assets.Editor/Quantum/NodePresenters/Commands/SkeletonNodePreserveAllCommand.cs` (Original: 35 linhas)
  - `sources/editor/Stride.Assets.Editor/Quantum/NodePresenters/Updaters/NavigationNodeUpdater.cs` (Original: 99 linhas)
  - `sources/editor/Stride.Assets.Editor/Quantum/NodePresenters/Updaters/ModelNodeLinkNodeUpdater.cs` (Original: 91 linhas)
  - `sources/editor/Stride.Assets.Editor/Quantum/NodePresenters/Updaters/EntityHierarchyEditorNodeUpdater.cs` (Original: 41 linhas)
  - `sources/editor/Stride.Assets.Editor/Quantum/NodePresenters/Updaters/VisualScriptNodeUpdater.cs` (Original: 38 linhas)
  - `sources/editor/Stride.Assets.Editor/Quantum/NodePresenters/Updaters/FXAAEffectNodeUpdater.cs` (Original: 36 linhas)
  - `sources/editor/Stride.Assets.Editor/Quantum/NodePresenters/Updaters/SpriteFontAssetNodeUpdater.cs` (Original: 31 linhas)
  - `sources/editor/Stride.Assets.Editor/Quantum/NodePresenters/Keys/ModelNodeLinkData.cs` (Original: 15 linhas)
  - `sources/editor/Stride.Assets.Editor/Quantum/NodePresenters/Keys/VisualScriptData.cs` (Original: 14 linhas)
  - `sources/editor/Stride.Assets.Editor/Quantum/NodePresenters/Keys/SpriteFontData.cs` (Original: 13 linhas)
  - `sources/editor/Stride.Core.Assets.Editor.Tests/TestQuantumNodeCommandsAndUpdaters.cs` (Teste TDD automatizado integrado)
- **Critérios de Aceite:**
  - Registro de comandos quânticos no node presenter de propriedades.
  - Execução segura e reativa de updaters ao inspecionar entidades, modelos e scripts.
  - Testes passando 100% no `dotnet test`.

### Fatia 2 (slice-2): Property Grid Template Providers & Data Type Selectors
- **Objetivo:** Portar os provedores de template dinâmicos e seletores de tipos para PropertyGrid no Avalonia (seletores de tipo genérico, dicionários, coleções, materiais, referências de variáveis e scripts).
- **Arquivos sob Lock Exclusivo (Builder 2):**
  - `sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/DataTypeTemplateSelector.cs` (Original: 58 linhas)
  - `sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/GenericDictionaryNodeTemplateProvider.cs` (Original: 33 linhas)
  - `sources/editor/Stride.Assets.Editor.Avalonia/Views/ModelComponentMaterialTemplateProvider.cs` (Original: 27 linhas)
  - `sources/editor/Stride.Assets.Editor.Avalonia/Views/ScriptVariableReferenceTemplateProvider.cs` (Original: 21 linhas)
  - `sources/editor/Stride.Assets.Editor.Avalonia/Views/ModelNodeLinkNameTemplateProvider.cs` (Original: 20 linhas)
  - `sources/editor/Stride.Assets.Editor.Avalonia/Views/ScriptTextEditorTemplateProvider.cs` (Original: 19 linhas)
  - `sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/NullableStructTemplateProvider.cs` (Original: 18 linhas)
  - `sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/ObjectTemplateProvider.cs` (Original: 17 linhas)
  - `sources/editor/Stride.Core.Assets.Editor.Tests/TestPropertyTemplateProviders.cs` (Teste TDD automatizado integrado)
- **Critérios de Aceite:**
  - Provedores selecionam templates corretos para cada nó quântico correspondente.
  - Resolução adequada de tipos primitivos, nulos e dicionários tipados.
  - Testes passando 100% no `dotnet test`.

### Fatia 3 (slice-3): Behaviors de Validação de Propriedades Vetoriais, Numéricas e Árvore Reativa
- **Objetivo:** Portar para Avalonia os comportamentos de validação em tempo real de caixas de texto com valores escalares e vetoriais, auto-expansão em árvores e realce de itens de lista.
- **Arquivos sob Lock Exclusivo (Builder 3):**
  - `sources/editor/Stride.Core.Assets.Editor.Avalonia/View/Behaviors/TextBoxVectorPropertyValueValidationBehavior.cs` (Original: 199 linhas)
  - `sources/editor/Stride.Core.Assets.Editor.Avalonia/View/Behaviors/TextBoxPropertyValueValidationBehavior.cs` (Original: 146 linhas)
  - `sources/editor/Stride.Core.Assets.Editor.Avalonia/View/Behaviors/ListBoxHighlightedItemBehavior.cs` (Original: 186 linhas)
  - `sources/editor/Stride.Core.Assets.Editor.Avalonia/View/Behaviors/TreeViewAutoExpandBehavior.cs` (Original: 150 linhas)
  - `sources/editor/Stride.Core.Assets.Editor.Tests/TestPropertyValidationBehaviors.cs` (Teste TDD automatizado integrado)
- **Critérios de Aceite:**
  - Validação estrita de expressões numéricas, vetores 2D/3D/4D e reversão graciosa em caso de input inválido.
  - Gerenciamento reativo de seleção e foco sem travar o pipeline da UI.
  - Testes passando 100% no `dotnet test`.

---

## 3. Matriz de Locks & Ownership de Arquivos

| Fatia | Builder Responsável | Arquivos sob Lock Exclusivo |
| :--- | :--- | :--- |
| **Fatia 1** | Builder 1 (Quantum Commands & Updaters) | `AddNewScriptComponentCommand.cs`, `EditCurveCommand.cs`, `PickupEntityComponentCommand.cs`, `PickupEntityCommand.cs`, `PickupSceneObjectCommandBase.cs`, `SetSymbolReferenceCommand.cs`, `SkeletonNodePreserveAllCommand.cs`, `NavigationNodeUpdater.cs`, `ModelNodeLinkNodeUpdater.cs`, `EntityHierarchyEditorNodeUpdater.cs`, `VisualScriptNodeUpdater.cs`, `FXAAEffectNodeUpdater.cs`, `SpriteFontAssetNodeUpdater.cs`, `ModelNodeLinkData.cs`, `VisualScriptData.cs`, `SpriteFontData.cs`, `TestQuantumNodeCommandsAndUpdaters.cs` |
| **Fatia 2** | Builder 2 (Template Providers & Selectors) | `DataTypeTemplateSelector.cs`, `GenericDictionaryNodeTemplateProvider.cs`, `ModelComponentMaterialTemplateProvider.cs`, `ScriptVariableReferenceTemplateProvider.cs`, `ModelNodeLinkNameTemplateProvider.cs`, `ScriptTextEditorTemplateProvider.cs`, `NullableStructTemplateProvider.cs`, `ObjectTemplateProvider.cs`, `TestPropertyTemplateProviders.cs` |
| **Fatia 3** | Builder 3 (Validation Behaviors & Tree Expand) | `TextBoxVectorPropertyValueValidationBehavior.cs`, `TextBoxPropertyValueValidationBehavior.cs`, `ListBoxHighlightedItemBehavior.cs`, `TreeViewAutoExpandBehavior.cs`, `TestPropertyValidationBehaviors.cs` |

---

## 4. Diretriz Inegociável de Paridade de Linhas (Zero-Summarization)
- Cada Builder DEVE inspecionar os arquivos originais nos diretórios WPF (`Stride.Core.Assets.Editor.Wpf`, `Stride.Assets.Presentation.Wpf`).
- A contagem de linhas e riqueza de lógica deve espelhar o original com precisão (mínimo de 85-100% de paridade de regras e métodos).
- Proibido omitir eventos, expressões matemáticas, verificações de segurança ou tratamentos de erro.
