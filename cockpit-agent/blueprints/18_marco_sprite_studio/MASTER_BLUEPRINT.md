# 🏛️ MASTER BLUEPRINT: Marco P18 - Sprite Studio, Sheet Packing & Slicing Editor

**Epic Goal:** Concluir a migração nativa do ecossistema de Sprites, Fontes Rasterizadas/SDF e SpriteStudio do Stride para Linux 100% Nativo (.NET 10 + Avalonia 12 + Vulkan RADV), cobrindo viewmodels especializados, geradores de template a partir de arquivos, comandos de renderização de thumbnails em batch, cache de manipulação de pixels com detecção automática de regiões (magic wand / color picker) e a view interativa com controle de slicing, bordas 9-patch e pivôs centrais.

---

## 🎯 Divisão de Fatias Verticais (3x3 Arquitetura)

### 🔹 Fatia 1 (slice-1): Sprite & Font ViewModels, Generation & Asset Policies
* **Builder:** `Sprite & Font ViewModels Builder`
* **Escopo:**
  1. `sources/editor/Stride.Assets.Editor/ViewModels/SpriteSheetViewModel.cs` (~18 linhas, herança de `AssetViewModel<SpriteSheetAsset>`).
  2. `sources/editor/Stride.Assets.Editor/ViewModels/SpriteFontViewModel.cs` (~99 linhas, comandos `GeneratePrecompiledFontCommand`, diálogo de seleção de pasta e espaço de cor Linear/Gamma, transação de UndoRedo e consistência de sessão).
  3. `sources/editor/Stride.Assets.Editor/ViewModels/PrecompiledSpriteFontViewModel.cs` (~19 linhas, herança de `AssetViewModel<PrecompiledSpriteFontAsset>`).
  4. `sources/editor/Stride.Assets.Editor/ViewModels/SpriteStudioModelViewModel.cs` (~32 linhas, herança de `ImportedAssetViewModel<SpriteStudioModelAsset>`, importação de nós e mesclagem de origem).
  5. `sources/editor/Stride.Assets.Editor/Templates/SpriteSheetFromFileTemplateGenerator.cs` (~98 linhas, herança de `AssetFromFileTemplateGenerator`, suporte aos GUIDs `SpriteSheetId` e `UISpriteSheetId`, extração de dimensões via `TextureTool` e criação de sprites com nomes de arquivo).
  6. `sources/editor/Stride.Assets.Editor/AssetEditors/EntityHierarchyEditor/ViewModels/AddAssetPolicies/AddSpriteStudioModelAssetPolicy.cs` (~26 linhas, herança de `CreateComponentPolicyBase<SpriteStudioModelAsset, SpriteStudioModelViewModel>`, criação de `SpriteStudioComponent` com referência de conteúdo).
* **Protocolo Obrigatório de Bugs / TDD Iron Law:**
  - Criar `sources/editor/Stride.Core.Assets.Editor.Tests/TestSpriteStudioViewModelsAndPoliciesIntegration.cs` que deve **FALHAR (RED)** antes da implementação dos tipos e passar com 100% de sucesso (**GREEN**) após a conclusão.

---

### 🔹 Fatia 2 (slice-2): Sprite Batch Commands, Thumbnails, Updaters & Image Cache
* **Builder:** `Sprite Batch & Image Cache Builder`
* **Escopo:**
  1. Integrar e validar `sources/editor/Stride.Editor/Thumbnails/ThumbnailFromSpriteBatchCommand.cs` (94 linhas, comando de renderização isolada de sprites em modo LDR via `GraphicsCompositor`).
  2. Validar `sources/editor/Stride.Assets.Editor/Thumbnails/SpriteStudioSheetThumbnailCompiler.cs` (66 linhas) e `sources/editor/Stride.Assets.Editor/Thumbnails/SpriteStudioAnimationThumbnailCompiler.cs` (20 linhas).
  3. Validar `sources/editor/Stride.Assets.Editor/AssetEditors/SpriteEditor/ViewModels/SpriteEditorImageCache.cs` (154 linhas, cache com `FindSpriteRegion`, `PickPixelColor`, `GetPixelSize`, integração segura com `TextureTool`).
  4. Validar `sources/editor/Stride.Assets.Editor/Quantum/NodePresenters/Updaters/SpriteSheetAssetNodeUpdater.cs` (40 linhas, controle de visibilidade de nós de `Borders`, `Center` e `CenterFromMiddle` de acordo com `SpriteSheetType`).
  5. Validar `SpriteSheetPreviewViewModel.cs`, `SpriteStudioSheetPreviewViewModel.cs` e `SpriteFontPreviewViewModel.cs`.
* **Protocolo Obrigatório de Bugs / TDD Iron Law:**
  - Criar `sources/editor/Stride.Core.Assets.Editor.Tests/TestSpriteStudioThumbnailsAndCacheIntegration.cs` que deve **FALHAR (RED)** antes da validação/testes integrados e passar com 100% de sucesso (**GREEN**).

---

### 🔹 Fatia 3 (slice-3): Avalonia Sprite Sheet Editor View, Slicing & Visual Interaction
* **Builder:** `Sprite Sheet Avalonia View & Slicing Builder`
* **Escopo:**
  1. Expandir `sources/editor/Stride.Assets.Editor.Avalonia/Views/SpriteSheetEditorView.axaml.cs` para paridade total com o original WPF (~85 linhas):
     - Anotação `[AssetEditorView<SpriteSheetEditorViewModel>]`.
     - Implementação de `IAssetEditorView` com método `InitializeEditor(IAssetEditorViewModel editor)`.
     - Comandos estáticos `FocusOnRegion` e `ActivateMagicWand`.
     - Manipulação de foco para atalhos de teclado.
  2. Validar e harmonizar `sources/editor/Stride.Assets.Editor.Avalonia/Views/SpriteSheetEditorView.axaml` (393 linhas):
     - Viewport de zoom e pan.
     - Barra de ferramentas com botões de seleção de ferramenta (Magic Wand, Color Picker, Seleção Retangular).
     - Lista de sprites com filtros e botões de ação (+ / Duplicar / Excluir).
     - Painel de propriedades de corte (TextureRegion, Borders 9-patch, Center).
  3. Validar `sources/editor/Stride.Assets.Editor.Avalonia/Views/Preview/SpriteSheetPreviewView.cs` e `SpriteFontPreviewView.cs`.
* **Protocolo Obrigatório de Bugs / TDD Iron Law:**
  - Criar `sources/editor/Stride.Core.Assets.Editor.Tests/TestSpriteSheetEditorViewAndSlicingIntegration.cs` que deve **FALHAR (RED)** antes da expansão da view e passar com 100% de sucesso (**GREEN**).

---

## 🚦 Critérios de Aceite e Homologação
1. **0 Erros de Compilação (Clean Build)** em todos os projetos de editor e Avalonia (.NET 10).
2. **Suíte Global de Testes:** 253 + novos testes passando (**100% GREEN, 0 falhas**).
3. **Paridade Arquitetural Estrita:** 85% a 100% de linhas de código preservadas em relação ao original Stride.
4. **Sem Gambiarras:** Código limpo, desacoplado de dependências Win32/WPF, com suporte nativo a Linux.
5. **Aprovação Unânime do Gauntlet Loop:** Reviewers só convocados após a entrega dos 3 Builders.
