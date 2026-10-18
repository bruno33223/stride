# MASTER BLUEPRINT: 09_marco_settings_preferences

## 1. Visão Geral e Objetivo
Portar integralmente o subsistema de **Configurações, Preferências e Diálogos Auxiliares** da Game Studio do Stride para Linux Nativo (Avalonia 12 + Vulkan RADV), garantindo:
- Paridade estrita de comportamento e linhas de código com o código original WPF (PROIBIDO resumir ou podar regras de negócio, fallbacks e tratamentos de borda).
- Eliminação total de dependências de Win32, WPF e Windows Forms.
- Ciclo TDD Iron Law: cada fatia deve possuir teste automatizado integrado demonstrando RED antes da implementação e GREEN ao finalizar.
- Validação no Gauntlet Loop com 3 Harsh Critics independentes auditando paridade e clean build.

---

## 2. Fatias Verticais de Implementação (Lote 3x3)

### Fatia 1 (slice-1): Domínio, Settings Core & Modelos de Configuração
- **Objetivo:** Portar os modelos de configuração fundamentais, classes estáticas de settings, wrappers de propriedades de pacotes e comandos de settings.
- **Arquivos sob Lock Exclusivo (Builder 1):**
  -  (Original WPF: 148 linhas)
  -  (Original WPF: 101 linhas)
  -  (Original WPF: 54 linhas)
  -  (Original WPF: 29 linhas)
  -  (Original WPF: 54 linhas)
  -  (Original WPF: 101 linhas)
  -  (Original WPF: 18 linhas)
  -  (Teste TDD automatizado integrado)
- **Critérios de Aceite:**
  - Persistência e carregamento de perfis de usuário sem dependência de registro Win32.
  - Carregamento de chaves e valores default idênticos ao WPF.
  - Testes passando 100% no .

### Fatia 2 (slice-2): ViewModels, Providers de Configurações & Updaters
- **Objetivo:** Portar os ViewModels que controlam a hierarquia de categorias de configurações, configurações do jogo (GameSettings) e serviços de provedor de build.
- **Arquivos sob Lock Exclusivo (Builder 2):**
  -  (Original WPF: 141 linhas)
  -  (Original WPF: 110 linhas)
  -  (Original WPF: 15 linhas)
  -  (Original WPF: 157 linhas)
  -  (Original WPF: 158 linhas)
  -  (Original WPF: 29 linhas)
  -  (Original WPF: 29 linhas)
  -  (Original WPF: 91 linhas)
  -  (Original WPF: 63 linhas)
  -  (Teste TDD automatizado integrado)
- **Critérios de Aceite:**
  - Reatividade MVVM completa para categorias, busca e nós de propriedade de configuração.
  - Associação correta do GameSettingsAsset com a sessão e compilação de assets.
  - Testes passando 100% no .

### Fatia 3 (slice-3): Views Avalonia, Converters, SettingsWindow & Diálogos Auxiliares
- **Objetivo:** Construir as janelas modais de configurações e diagnóstico em Avalonia XAML moderno, integrando-as ao menu e comandos da janela principal.
- **Arquivos sob Lock Exclusivo (Builder 3):**
  -  (Original WPF: 99 linhas)
  -  (Original WPF: 57 linhas)
  -  (Original WPF: 26 linhas)
  -  (Original WPF: 27 linhas)
  -  (Original WPF: 18 linhas)
  -  (Teste TDD automatizado integrado)
- **Arquivo Compartilhado (Append-Only):**
  -  (Adição dos comandos de exibição da SettingsWindow e DebugWindow)
- **Critérios de Aceite:**
  - Layout escuro moderno (FluentTheme Dark), pesquisa em tempo real, árvore de categorias fluida e PropertyGrid reativo.
  - Compilação limpa (0 erros) em  e .

---

## 3. Matriz de Locks & Ownership de Arquivos

| Fatia | Builder Responsável | Arquivos sob Lock Exclusivo |
| :--- | :--- | :--- |
| **Fatia 1** | Builder 1 (Core & Domínio) | , , , , , , ,  |
| **Fatia 2** | Builder 2 (ViewModels & Providers) | , , , , , , , , ,  |
| **Fatia 3** | Builder 3 (Avalonia UI & Janelas) | , , , , ,  |

*Arquivo Compartilhado (Append-Only):* 

---

## 4. Diretriz Inegociável de Paridade de Linhas (Zero-Summarization)
- Cada Builder DEVE inspecionar o arquivo original no caminho WPF correspondente (,  ou ).
- A contagem de linhas e riqueza de lógica deve espelhar o original com precisão de pelo menos 85-100% das linhas úteis (não criar stubs ou versões abreviadas).
- O Harsh Critic rejeitará categoricamente qualquer fatia com discrepância artificial de linhas de código ou métodos omitidos.
