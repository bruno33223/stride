# MASTER BLUEPRINT: 01_marco_dark_theme

## 1. Visão Geral e Objetivo
Eliminar a aparência "Windows 97 / wireframe de debug" do Stride Game Studio nativo (Avalonia) no Linux, aplicando o tema escuro profissional (FluentTheme Dark) e limpando as bordas hardcoded nos containers de visualização.

## 2. Fatias Verticais de Implementação
### Fatia 1 (slice-1): Limpeza de Bordas e Estilização de Layout
- **Alvo:** `sources/editor/Stride.GameStudio.Avalonia/Views/MainView.axaml`
- **Ações:**
  - Remover bordas de debug (`BorderBrush="Red"`, `BorderBrush="Orange"`, `BorderBrush="Blue"`, `BorderBrush="Cyan"`).
  - Substituir por bordas sutis e harmônicas com tema escuro (`#2B2B2B` ou divisores nativos).
  - Manter integridade de bindings de todos os controles (`DataContext="{Binding ...}"`).

### Fatia 2 (slice-2): Tema e Recursos de Cores Globais
- **Alvo:** `sources/editor/Stride.GameStudio.Avalonia/App.axaml`
- **Ações:**
  - Garantir `RequestedThemeVariant="Dark"` como padrão.
  - Calibrar paletas Fluent e propriedades de background para um visual escuro moderno uniforme.

### Fatia 3 (slice-3): Validação de Compilação
- **Alvo:** Validação do build do projeto `Stride.GameStudio.Avalonia.Desktop`

## 3. Matriz de Locks e Ownership de Arquivos
| Fatia | Builder | Arquivos sob Lock Exclusivo |
| :--- | :--- | :--- |
| Fatia 1 | Builder 1 | `sources/editor/Stride.GameStudio.Avalonia/Views/MainView.axaml` |
| Fatia 2 | Builder 2 | `sources/editor/Stride.GameStudio.Avalonia/App.axaml` |
| Fatia 3 | Builder 3 | Verificação e build da solução |
