# 🏛️ MASTER BLUEPRINT: Marco P26 - Thumbnail Compilation Pipeline, Caching & Preview Infrastructure

**Epic:** Migração integral e desacoplamento do pipeline de compilação assíncrona de miniaturas (thumbnails), motor de geração, cache em disco e memória, comandos gráficos de renderização de preview e arquitetura completa de Asset Preview ViewModels do ecossistema legado WPF para Linux 100% Nativo (Avalonia 12 + Vulkan RADV + .NET 10).

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

### 🔹 Fatia 1 (slice-1): Core Thumbnail Contracts, Service & Generators
- **Builder Role:** `Thumbnail Service & Generator Builder`
- **Escopo de Arquivos:**
  - `sources/editor/Stride.Core.Assets.Editor/Services/IThumbnailService.cs` (41 l)
  - `sources/editor/Stride.Core.Assets.Editor/Services/ThumbnailData.cs` (154 l)
  - `sources/editor/Stride.Core.Assets.Editor/Services/ThumbnailCompletedArgs.cs` (19 l)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/GameStudioThumbnailService.cs` (326 l)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/ThumbnailGenerator.cs` (366 l)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/ThumbnailBuiltEventArgs.cs` (68 l)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/ThumbnailBuildHelper.cs` (146 l)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/ThumbnailBuildStep.cs` (52 l)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/ThumbnailAssetBuildUnit.cs` (36 l)
  - `sources/editor/Stride.Core.Assets.Editor.Avalonia/Behaviors/TilePanelThumbnailPrioritizationBehavior.cs` (64 l)
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestThumbnailCoreServiceAndGeneratorIntegration.cs`
- **Objetivo:** Registro, despacho, priorização baseada em viewport e notificação de conclusão de miniaturas de assets de forma desacoplada da UI.

---

### 🔹 Fatia 2 (slice-2): Thumbnail Compilers, Commands & Static Resources
- **Builder Role:** `Thumbnail Compilers & Commands Builder`
- **Escopo de Arquivos:**
  - `sources/editor/Stride.Assets.Editor/Thumbnails/IThumbnailCompiler.cs` (13 l)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/IThumbnailCommand.cs` (17 l)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/ThumbnailCommand.cs` (60 l)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/ThumbnailCommandParameters.cs` (39 l)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/ThumbnailCompilerBase.cs` (205 l)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/ThumbnailCompilerContext.cs` (116 l)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/ThumbnailCompilationContext.cs` (10 l)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/ThumbnailListCompiler.cs` (80 l)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/CustomAssetThumbnailCompiler.cs` (55 l)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/StaticThumbnailCompiler.cs` (37 l)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/StaticThumbnailCommand.cs` (97 l)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/ThumbnailFromTextureCommand.cs` (104 l)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/ThumbnailFromEntityCommand.cs` (182 l)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/StrideThumbnailCommand.cs` (193 l)
  - `sources/editor/Stride.Assets.Presentation/Thumbnails/ProceduralModelThumbnailCompiler.cs` (49 l)
  - `sources/editor/Stride.Assets.Presentation/Thumbnails/PrecompiledFontThumbnailCompiler.cs` (45 l)
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestThumbnailCompilersAndCommandsIntegration.cs`
- **Objetivo:** Compiladores específicos por tipo de asset, geração estática a partir de bitmaps e geração dinâmica a partir de entidades e texturas renderizadas em Vulkan RADV.

---

### 🔹 Fatia 3 (slice-3): Asset Preview Engine, Annotations & ViewModels
- **Builder Role:** `Asset Preview Engine Builder`
- **Escopo de Arquivos:**
  - `sources/editor/Stride.Core.Assets.Editor/Services/IAssetPreviewService.cs` (21 l)
  - `sources/editor/Stride.Assets.Editor/Preview/IAssetPreview.cs` (62 l)
  - `sources/editor/Stride.Assets.Editor/Preview/IPreviewBuilder.cs` (50 l)
  - `sources/editor/Stride.Assets.Editor/Preview/AssetPreview.cs` (264 l)
  - `sources/editor/Stride.Assets.Editor/Preview/PreviewGame.cs` (216 l)
  - `sources/editor/Stride.Assets.Editor/Preview/PreviewEntity.cs` (25 l)
  - `sources/editor/Stride.Assets.Presentation/Preview/BuildAssetPreview.cs` (128 l)
  - `sources/editor/Stride.Assets.Editor/Annotations/AssetPreviewAttribute.cs` (35 l)
  - `sources/editor/Stride.Assets.Editor/Annotations/AssetPreviewViewModelAttribute.cs` (33 l)
  - `sources/editor/Stride.Assets.Editor/Annotations/AssetPreviewViewAttribute.cs` (32 l)
  - `sources/editor/Stride.Assets.Editor/Preview/ViewModel/IAssetPreviewViewModel.cs` (21 l)
  - `sources/editor/Stride.Assets.Presentation/ViewModel/IAnimatedPreviewViewModel.cs` (15 l)
  - `sources/editor/Stride.Assets.Presentation/ViewModel/Preview/AssetPreviewViewModel.cs` (27 l)
  - `sources/editor/Stride.Assets.Presentation/ViewModel/Preview/ModelPreviewViewModel.cs` (34 l)
  - `sources/editor/Stride.Assets.Presentation/ViewModel/Preview/MaterialPreviewViewModel.cs` (37 l)
  - `sources/editor/Stride.Assets.Presentation/ViewModel/Preview/TextureBasePreviewViewModel.cs` (96 l)
  - `sources/editor/Stride.Assets.Presentation/ViewModel/Preview/TexturePreviewViewModel.cs` (63 l)
  - `sources/editor/Stride.Assets.Presentation/ViewModel/Preview/SkyboxPreviewViewModel.cs` (61 l)
  - `sources/editor/Stride.Assets.Presentation/ViewModel/Preview/HeightmapPreviewViewModel.cs` (40 l)
  - `sources/editor/Stride.Assets.Presentation/ViewModel/Preview/EntityPreviewViewModel.cs` (23 l)
  - `sources/editor/Stride.Assets.Presentation/ViewModel/Preview/ProceduralModelPreviewViewModel.cs` (34 l)
- **TDD Iron Law:** `sources/editor/Stride.Core.Assets.Editor.Tests/TestAssetPreviewEngineAndViewModelsIntegration.cs`
- **Objetivo:** Motor de visualização em tempo real de assets (Modelos, Materiais, Texturas, Skyboxes, Heightmaps), anotações semânticas e sincronização de games de preview.

---

## 🚦 Critérios de Aceite Gauntlet Loop
- Paridade lógica de 85% a 100% com o legado WPF original.
- Compilação 100% limpa (0 erros, 0 warnings novos).
- Todos os testes de integração novos e existentes passando com 100% de sucesso (GREEN).
