# MASTER BLUEPRINT: 10_marco_asset_view_thumbnails

## 1. Visão Geral e Objetivo
Portar e consolidar integralmente o subsistema de **Asset View Avançado, Grade de Thumbnails e Sistema de Drag-and-Drop Cross-Platform** do Stride Game Studio para Linux Nativo (Avalonia 12 + Vulkan RADV), garantindo:
- Paridade estrita de comportamento e linhas de código com as implementações originais WPF (PROIBIDO resumir ou podar regras de negócio, fallbacks, cálculos de matriz de thumbnail, adorners e tratamentos de drag-and-drop).
- Eliminação total de dependências de Win32, WPF (`System.Windows`, `System.Windows.Media`) e Windows Forms.
- Ciclo TDD Iron Law: cada fatia deve possuir teste automatizado integrado demonstrando RED antes da implementação e GREEN ao finalizar.
- Validação no Gauntlet Loop com 3 Harsh Critics independentes auditando paridade cega, clean build e ausência de regressões.

---

## 2. Fatias Verticais de Implementação (Lote 3x3)

### Fatia 1 (slice-1): Thumbnail Infrastructure & Compiladores de Assets
- **Objetivo:** Portar e unificar os geradores e compiladores de miniaturas (thumbnails) de assets para o pipeline Linux nativo, com cache, comandos de renderização estática e dinâmica de modelos, texturas, cenas e prefabs.
- **Arquivos sob Lock Exclusivo (Builder 1):**
  - `sources/editor/Stride.Assets.Presentation/Resources/Thumbnails/StaticThumbnails.cs` (Original: 164 linhas)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/ModelThumbnailCompiler.cs` (Original: 49 linhas)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/TextureThumbnailCompiler.cs` (Original: 34 linhas)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/SceneThumbnailCompiler.cs` (Original: 20 linhas)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/MaterialThumbnailCompiler.cs` (Original: 33 linhas)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/PrefabThumbnailCompiler.cs` (Original: 20 linhas)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/AnimationThumbnailCompiler.cs` (Original: 20 linhas)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/FontThumbnailCompiler.cs` (Original: 20 linhas)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/GameSettingsThumbnailCompiler.cs` (Original: 20 linhas)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/GraphicsCompositorThumbnailCompiler.cs` (Original: 20 linhas)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/HeightmapThumbnailCompiler.cs` (Original: 20 linhas)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/SkeletonThumbnailCompiler.cs` (Original: 20 linhas)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/SkyboxThumbnailCompiler.cs` (Original: 20 linhas)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/SoundThumbnailCompiler.cs` (Original: 20 linhas)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/SpriteSheetThumbnailCompiler.cs` (Original: 20 linhas)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/UILibraryThumbnailCompiler.cs` (Original: 20 linhas)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/UIPageThumbnailCompiler.cs` (Original: 20 linhas)
  - `sources/editor/Stride.Assets.Editor/Thumbnails/VideoThumbnailCompiler.cs` (Original: 20 linhas)
  - `sources/editor/Stride.Core.Assets.Editor.Tests/TestThumbnailInfrastructure.cs` (Teste TDD automatizado integrado)
- **Critérios de Aceite:**
  - Registro correto no `AssetCompilerRegistry` para os compiladores estáticos e dinâmicos em Linux.
  - Carregamento de bitmaps estáticos sem crash e geração de steps de build de thumbnail.
  - Testes passando 100% no `dotnet test`.

### Fatia 2 (slice-2): Cross-Platform Drag & Drop System
- **Objetivo:** Implementar o subsistema de Drag & Drop para Avalonia e Linux, permitindo arrastar assets da grade para a árvore de pastas, propriedades do inspector e viewport 3D da cena.
- **Arquivos sob Lock Exclusivo (Builder 2):**
  - `sources/editor/Stride.Core.Assets.Editor.Avalonia/View/Behaviors/DragDrop/IDragDropBehavior.cs` (Original: 24 linhas)
  - `sources/editor/Stride.Core.Assets.Editor.Avalonia/View/Behaviors/DragDrop/DragDropBehavior.cs` (Original: 571 linhas)
  - `sources/editor/Stride.Core.Assets.Editor.Avalonia/View/Behaviors/DragDrop/DragDropAdornerManager.cs` (Original: 285 linhas)
  - `sources/editor/Stride.Core.Assets.Editor.Avalonia/View/Behaviors/DragDrop/TreeViewDragDropBehavior.cs` (Original: 97 linhas)
  - `sources/editor/Stride.Core.Assets.Editor.Avalonia/View/Behaviors/DragDrop/PropertyViewDragDropBehavior.cs` (Original: 105 linhas)
  - `sources/editor/Stride.Core.Assets.Editor.Avalonia/View/Behaviors/DragDrop/PropertyViewItemDragDropBehavior.cs` (Original: 86 linhas)
  - `sources/editor/Stride.Core.Assets.Editor.Avalonia/View/Behaviors/DragDrop/DragWindow.axaml` (Original: 21 linhas)
  - `sources/editor/Stride.Core.Assets.Editor.Avalonia/View/Behaviors/DragDrop/DragWindow.axaml.cs` (Original: 26 linhas)
  - `sources/editor/Stride.Assets.Editor/AssetEditors/GameEditor/Services/EditorGameController.DragDrop.cs` (Original: 116 linhas)
  - `sources/editor/Stride.Core.Assets.Editor.Tests/TestDragDropBehaviors.cs` (Teste TDD automatizado integrado)
- **Critérios de Aceite:**
  - Manipulação de eventos `DragEnter`, `DragOver`, `DragLeave`, `Drop` via `Avalonia.Input.DragDrop`.
  - Empacotamento de payload compatível com `IAssetViewModel`, URLs e IDs de assets.
  - Renderização do adorner visual flutuante com feedback de operação permitida / proibida.
  - Testes passando 100% no `dotnet test`.

### Fatia 3 (slice-3): Asset View Avançado, Diretórios & Asset Picker
- **Objetivo:** Portar a hierarquia rica de pastas (`DirectoryBaseViewModel`, `DirectoryViewModel`, `MountPointViewModel`), o controle avançado de visualização de assets (`AssetViewUserControl`) com zoom, filtros e busca, e a janela modal `AssetPickerWindow`.
- **Arquivos sob Lock Exclusivo (Builder 3):**
  - `sources/editor/Stride.Core.Assets.Editor/ViewModels/DirectoryBaseViewModel.cs` (Original: 348 linhas)
  - `sources/editor/Stride.Core.Assets.Editor/ViewModels/DirectoryViewModel.cs` (Original: 208 linhas)
  - `sources/editor/Stride.Core.Assets.Editor/ViewModels/MountPointViewModel.cs` (Original: 52 linhas)
  - `sources/editor/Stride.Core.Assets.Editor/ViewModels/AssetMountPointViewModel.cs` (Original: 30 linhas)
  - `sources/editor/Stride.Core.Assets.Editor/Services/IAssetPickerDialog.cs` (Original: 53 linhas)
  - `sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/AssetViewUserControl.axaml` (Original: 429 linhas)
  - `sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/AssetViewUserControl.axaml.cs` (Original: 360 linhas)
  - `sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/AssetPickerWindow.axaml` (Original: 91 linhas)
  - `sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/AssetPickerWindow.axaml.cs` (Original: 139 linhas)
  - `sources/editor/Stride.Core.Assets.Editor.Tests/TestAssetViewReactivity.cs` (Teste TDD automatizado integrado)
- **Critérios de Aceite:**
  - Gerenciamento reativo de diretórios: criar pasta, renomear, mover, apagar, histórico de navegação.
  - AssetViewUserControl completo com slider de zoom, pesquisa por texto, filtro por tipos de assets e suporte a teclado/mouse.
  - Diálogo AssetPickerWindow para seleção modal de assets com preview e filtros configuráveis.
  - Testes passando 100% no `dotnet test`.

---

## 3. Matriz de Locks & Ownership de Arquivos

| Fatia | Builder Responsável | Arquivos sob Lock Exclusivo |
| :--- | :--- | :--- |
| **Fatia 1** | Builder 1 (Thumbnails & Compiladores) | `StaticThumbnails.cs`, Compiladores de Thumbnail (`ModelThumbnailCompiler.cs`, `TextureThumbnailCompiler.cs`, etc.), `TestThumbnailInfrastructure.cs` |
| **Fatia 2** | Builder 2 (Drag & Drop System) | `IDragDropBehavior.cs`, `DragDropBehavior.cs`, `DragDropAdornerManager.cs`, `TreeViewDragDropBehavior.cs`, `PropertyViewDragDropBehavior.cs`, `PropertyViewItemDragDropBehavior.cs`, `DragWindow.axaml / .cs`, `EditorGameController.DragDrop.cs`, `TestDragDropBehaviors.cs` |
| **Fatia 3** | Builder 3 (Asset View & Asset Picker) | `DirectoryBaseViewModel.cs`, `DirectoryViewModel.cs`, `MountPointViewModel.cs`, `AssetMountPointViewModel.cs`, `IAssetPickerDialog.cs`, `AssetViewUserControl.axaml / .cs`, `AssetPickerWindow.axaml / .cs`, `TestAssetViewReactivity.cs` |

---

## 4. Diretriz Inegociável de Paridade de Linhas (Zero-Summarization)
- Cada Builder DEVE inspecionar os arquivos originais nos diretórios WPF (`Stride.Core.Assets.Editor.Wpf`, `Stride.Assets.Presentation.Wpf`, `Stride.Editor.Wpf`).
- A contagem de linhas e riqueza de lógica deve espelhar o original com precisão (mínimo de 85-100% de paridade de regras e métodos).
- Proibido omitir eventos, métodos de navegação, verificações de segurança ou tratamentos de erro.
