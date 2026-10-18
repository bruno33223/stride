# HANDOFF: Transição Marco P16 -> Marco P17

## 1. Conquistas do Marco P16
- Portabilidade completa e homologação da suíte de Audio Preview, Sound Templates e Spatial Emitters para Linux nativo (Avalonia 12 + Vulkan RADV + OpenAL).
- 18 testes automatizados integrados adicionados à suíte global (totalizando 228 testes 100% GREEN).
- Paridade de linhas de 98% a 100% em todos os componentes de áudio.

## 2. Ponto de Entrada para o Marco P17: Asset Pickers, Reference Selectors & Type Modals
- **Arquitetura Alvo**: Diálogos modais e seletores de assets e tipos para o Property Grid e inspetores do Stride Game Studio:
  - `AssetPickerView` / `AssetPickerViewModel`: busca, filtros por tipo/tag, paginação, árvore de pastas e seleção de referências de assets.
  - `TypeModalView` / `TypeModalViewModel`: seletor de tipos de componentes e subclasses com reflexão Quantum.
  - `ReferenceSelectorView`: controle embutido no Property Grid para atribuição rápida de referências (drag-and-drop, botão de busca e limpeza).
- **Fatias Verticais**:
  - **Fatia 1 (slice-1)**: Asset Picker & Reference ViewModels (`AssetPickerViewModel`, `ReferenceSelectorViewModel`).
  - **Fatia 2 (slice-2)**: Type Modal & Subclass Hierarchy Services (`TypeModalViewModel`, `TypeSelectorService`).
  - **Fatia 3 (slice-3)**: Avalonia Modal Views & Picker Controls (`AssetPickerView.axaml`, `TypeModalView.axaml`, `ReferenceSelectorControl.axaml`).

## 3. Diretiva de Orquestração
- Operação estrita como Tech Leader / Staff Engineer: despacho imediato dos 3 Builders para execução autônoma das 3 fatias verticais com TDD Iron Law.
