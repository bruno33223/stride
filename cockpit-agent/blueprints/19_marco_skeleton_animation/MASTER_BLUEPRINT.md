# 🏛️ MASTER BLUEPRINT: Marco P19 - Skeleton & Animation Visualizer

**Epic Goal:** Concluir a migração nativa do ecossistema de Esqueletos 3D e Animações do Stride para Linux 100% Nativo (.NET 10 + Avalonia 12 + Vulkan RADV), cobrindo viewmodels especializados de importação, geradores de template de skeleton e animation, scripts de blending/início de animações, compiladores de preview do game engine, updaters quânticos de nós de duração/clipe e behaviors de timeline e reprodução no Avalonia 12.

---

## 🎯 Divisão de Fatias Verticais (3x3 Arquitetura)

### 🔹 Fatia 1 (slice-1): Animation ViewModels, Importers & Template Generators
* **Builder:** `Animation ViewModels & Generators Builder`
* **Escopo:**
  1. `sources/editor/Stride.Assets.Editor/ViewModels/AnimationViewModel.cs` (~32 linhas, herança de `ImportedAssetViewModel<AnimationAsset>`, resolução de `ModelAssetImporter` e sincronização de `AnimationTimeMaximum` e `AnimationTimeMinimum`).
  2. `sources/editor/Stride.Assets.Editor/Templates/SkeletonFromFileTemplateGenerator.cs` (~55 linhas, herança de `AssetFromFileTemplateGenerator`, GUID `7DA3597C-7AC4-450E-B60B-21DDB6296208`, importação de `SkeletonAsset` via `ModelAssetImporter`).
  3. `sources/editor/Stride.Assets.Editor/Templates/AnimationFromFileTemplateGenerator.cs` (~55 linhas, herança de `AssetFromFileTemplateGenerator`, GUID `24E65D4C-0207-4B62-B8F2-BAB33F44B4FA`, importação de `AnimationAsset` via `ModelAssetImporter`).
  4. Scripts de template de animação em `sources/editor/Stride.Assets.Editor/Templates/Assets/Scripts/Animation/`:
     - `AnimationBlend.cs` (~83 linhas, blending de duas animações com peso e avaliação dinâmica)
     - `AnimationStart.cs` (~53 linhas, autoplay de animação)
* **Protocolo Obrigatório de Bugs / TDD Iron Law:**
  - Criar `sources/editor/Stride.Core.Assets.Editor.Tests/TestAnimationViewModelsAndGeneratorsIntegration.cs` que deve **FALHAR (RED)** antes da implementação dos tipos e passar com 100% de sucesso (**GREEN**) após a conclusão.

---

### 🔹 Fatia 2 (slice-2): Animation Compilers, Updaters & Preview ViewModels
* **Builder:** `Animation Compilers & Updaters Builder`
* **Escopo:**
  1. Validar e integrar `sources/editor/Stride.Editor/Preview/AnimationAssetPreviewCompiler.cs` (25 linhas, compilador de preview de animações com `ThumbnailBuildStep` e `SceneRendererCollection`).
  2. Validar `sources/editor/Stride.Editor/Preview/AnimationAssetEditorGameCompiler.cs` (18 linhas, compilação de runtime do preview de animação).
  3. Validar `sources/editor/Stride.Assets.Editor/Quantum/NodePresenters/Updaters/AnimationAssetNodeUpdater.cs` (85 linhas, visibilidade e filtros de nós de duração e clipes).
  4. Validar `sources/editor/Stride.Assets.Editor/Quantum/NodePresenters/Updaters/SkeletonAssetNodeUpdater.cs` (37 linhas, ordenação e nós de ossos de esqueletos).
  5. Validar `sources/editor/Stride.Assets.Editor/ViewModels/Preview/AnimationPreviewViewModel.cs` (90 linhas, `IAnimatedPreviewViewModel`, reprodução contínua, slider de tempo e comandos play/pause).
* **Protocolo Obrigatório de Bugs / TDD Iron Law:**
  - Criar `sources/editor/Stride.Core.Assets.Editor.Tests/TestAnimationCompilersAndUpdatersIntegration.cs` que deve **FALHAR (RED)** antes da validação/testes integrados e passar com 100% de sucesso (**GREEN**).

---

### 🔹 Fatia 3 (slice-3): Avalonia Animation Controls, Behaviors & Template Providers
* **Builder:** `Avalonia Animation Controls Builder`
* **Escopo:**
  1. Portar `sources/editor/Stride.Assets.Editor.Avalonia/Behaviors/SuspendAnimationDuringSliderDragBehavior.cs` (~72 linhas, adaptado para Avalonia 12 com `PointerPressed` / `PointerReleased` e tracking de drag do Slider para suspender reprodução de animação durante arraste).
  2. Portar `sources/editor/Stride.Assets.Editor.Avalonia/TemplateProviders/AnimationFrameBoxTemplateProvider.cs` (20 linhas, correspondência de nós `StartAnimationTimeBox` e `EndAnimationTimeBox`).
  3. Portar `sources/editor/Stride.Assets.Editor.Avalonia/TemplateProviders/AnimationFrameTemplateProvider.cs` (20 linhas, correspondência de nós `StartAnimationTime` e `EndAnimationTime`).
  4. Validar e integrar `sources/editor/Stride.Assets.Editor.Avalonia/Views/Preview/AnimationPreviewView.cs` (anotação `[AssetPreviewView<AnimationPreview>]`).
* **Protocolo Obrigatório de Bugs / TDD Iron Law:**
  - Criar `sources/editor/Stride.Core.Assets.Editor.Tests/TestAnimationAvaloniaControlsAndBehaviorsIntegration.cs` que deve **FALHAR (RED)** antes da implementação dos controles e passar com 100% de sucesso (**GREEN**).

---

## 🚦 Critérios de Aceite e Homologação
1. **0 Erros de Compilação (Clean Build)** em todos os projetos de editor e Avalonia (.NET 10).
2. **Suíte Global de Testes:** 274 + novos testes passando (**100% GREEN, 0 falhas**).
3. **Paridade Arquitetural Estrita:** 85% a 100% de linhas de código preservadas em relação ao original Stride.
4. **Sem Gambiarras:** Código limpo, desacoplado de dependências Win32/WPF, com suporte nativo a Linux.
5. **Aprovação Unânime do Gauntlet Loop:** Reviewers só convocados após a entrega dos 3 Builders.
