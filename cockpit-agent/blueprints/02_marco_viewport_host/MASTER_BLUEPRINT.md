# MASTER BLUEPRINT: 02_marco_viewport_host

## 1. Visão Geral e Objetivo
Implementar a infraestrutura do controle de viewport 3D nativa no Avalonia (`StrideViewportControl`) herdando de `NativeControlHost`, desacoplando o antigo `HwndHost` do Windows e fornecendo o host nativo para a renderização da cena em Linux via Vulkan/X11.

## 2. Fatias Verticais de Implementação
### Fatia 1 (slice-1): Controle Nativo StrideViewportControl
- **Alvo:** `sources/editor/Stride.GameStudio.Avalonia/Controls/StrideViewportControl.cs`
- **Ações:**
  - Implementar classe herdando de `Avalonia.Controls.NativeControlHost`.
  - Prover métodos de ciclo de vida (`CreateNativeControlCore`, `DestroyNativeControlCore`).
  - Em Linux (Unix/X11), criar o container de janela nativo e expor o handle para inicialização da swapchain Vulkan do Stride.

### Fatia 2 (slice-2): Integração na Viewport do Editor
- **Alvo:** `sources/editor/Stride.GameStudio.Avalonia/Views/EditorCollectionView.axaml` (ou container central em `MainView.axaml`)
- **Ações:**
  - Integrar o `StrideViewportControl` no centro da área de edição, permitindo exibir o placeholder interativo da cena 3D com sobreposição gráfica e status da engine (FPS / Modo Vulkan).

### Fatia 3 (slice-3): Validação e Compilação
- **Alvo:** Compilar e validar a solução Avalonia Desktop sem erros de tipos ou dependências.

## 3. Matriz de Locks e Ownership de Arquivos
| Fatia | Builder | Arquivos sob Lock Exclusivo |
| :--- | :--- | :--- |
| Fatia 1 | Builder 1 | `sources/editor/Stride.GameStudio.Avalonia/Controls/StrideViewportControl.cs` |
| Fatia 2 | Builder 2 | `sources/editor/Stride.GameStudio.Avalonia/Views/EditorCollectionView.axaml` |
| Fatia 3 | Builder 3 | Verificação e build da solução |
