# 🏛️ MASTER BLUEPRINT: Marco P23 - Project & Asset Template Generators, DotNetNew Bridge & Platform Update Dialogs (Avalonia 12)

**Epic Goal:** Concluir a migração nativa do ecossistema de Geradores de Templates de Projeto e Assets, Ponte de Templates DotNetNew (`dotnet new`), Diálogo de Parâmetros de Template (`DotNetNewTemplateParametersWindow`), Gerador e Diálogo de Atualização de Plataformas da Solução (`UpdatePlatformsTemplateGenerator` / `UpdatePlatformsWindow`), e Geradores de Modelo 3D e Biblioteca de Projeto (`ModelFromFileTemplateGenerator`, `ProjectLibraryTemplateGenerator`) para Linux 100% Nativo (.NET 10 + Avalonia 12 + Vulkan RADV).

---

## 🎯 Divisão de Fatias Verticais (3x3 Arquitetura)

### 🔹 Fatia 1 (slice-1): DotNetNew Template Bridge, Generator Engine & Dialog
* **Builder:** `DotNetNew Template Engine Builder`
* **Escopo:**
  1. `sources/editor/Stride.Assets.Editor/Templates/DotNetNewTemplateBridge.cs` (~281 linhas, ponte de comunicação com CLI `dotnet new` do .NET 10).
  2. `sources/editor/Stride.Assets.Editor/Templates/DotNetNewTemplateGenerator.cs` (~252 linhas, orquestrador de execução de templates dotnet new).
  3. `sources/editor/Stride.Assets.Editor/Templates/DotNetNewTemplateRegistry.cs` (~140 linhas, registro e descoberta de templates disponíveis).
  4. Diálogo Avalonia 12 nativo `sources/editor/Stride.Assets.Editor.Avalonia/Views/DotNetNewTemplateParametersWindow.axaml` e `.axaml.cs` (~31 + 285 linhas, diálogo modal para coletar e validar parâmetros de templates).
* **Protocolo Obrigatório de Bugs / TDD Iron Law:**
  - Criar `sources/editor/Stride.Core.Assets.Editor.Tests/TestDotNetNewTemplatesAndDialogsIntegration.cs` que deve **FALHAR (RED)** antes da implementação dos tipos e passar com 100% de sucesso (**GREEN**).

---

### 🔹 Fatia 2 (slice-2): Platform Update Generator & Solution Platform ViewModels
* **Builder:** `Platform Update Generator & ViewModels Builder`
* **Escopo:**
  1. `sources/editor/Stride.Assets.Editor/Templates/UpdatePlatformsTemplateGenerator.cs` (~217 linhas, gerador de atualização/adição de plataformas-alvo da solução).
  2. `sources/editor/Stride.Assets.Editor/Templates/SolutionPlatformViewModel.cs` (~93 linhas, modelagem reativa de plataformas suportadas: Windows, Linux, Android, iOS, UWP).
  3. Diálogo Avalonia 12 nativo `sources/editor/Stride.Assets.Editor.Avalonia/Views/UpdatePlatformsWindow.axaml` e `.axaml.cs` (~58 + 92 linhas, modal de seleção de plataformas).
* **Protocolo Obrigatório de Bugs / TDD Iron Law:**
  - Criar `sources/editor/Stride.Core.Assets.Editor.Tests/TestUpdatePlatformsAndSolutionViewModelsIntegration.cs` que deve **FALHAR (RED)** antes da implementação e passar com 100% de sucesso (**GREEN**).

---

### 🔹 Fatia 3 (slice-3): 3D Model & Project Library Template Generators & Dialogs
* **Builder:** `Model & Project Library Templates Builder`
* **Escopo:**
  1. `sources/editor/Stride.Assets.Editor/Templates/ModelFromFileTemplateGenerator.cs` (~164 linhas, gerador de templates a partir de arquivos FBX/OBJ de modelos 3D).
  2. Diálogo Avalonia 12 nativo `sources/editor/Stride.Assets.Editor.Avalonia/Views/ModelAssetTemplateWindow.axaml` e `.axaml.cs` (~58 + 105 linhas, diálogo modal com opções de importação de modelo, esqueleto e materiais).
  3. `sources/editor/Stride.Assets.Editor/Templates/ProjectLibraryTemplateGenerator.cs` (~102 linhas, gerador de bibliotecas de código de projeto).
  4. Diálogo Avalonia 12 nativo `sources/editor/Stride.Assets.Editor.Avalonia/Views/ProjectLibraryWindow.axaml` e `.axaml.cs` (~31 + 97 linhas, diálogo modal de criação de biblioteca).
* **Protocolo Obrigatório de Bugs / TDD Iron Law:**
  - Criar `sources/editor/Stride.Core.Assets.Editor.Tests/TestModelAndProjectLibraryTemplatesIntegration.cs` que deve **FALHAR (RED)** antes da implementação e passar com 100% de sucesso (**GREEN**).

---

## 🚦 Critérios de Aceite e Homologação
1. **0 Erros de Compilação (Clean Build)** em todos os projetos de editor e Avalonia (.NET 10).
2. **Suíte Global de Testes:** 347 + novos testes passando (**100% GREEN, 0 falhas**).
3. **Paridade Arquitetural Estrita:** 85% a 100% de linhas de código preservadas em relação ao original Stride.
4. **Sem Gambiarras:** Código limpo, desacoplado de Win32/WPF, com suporte nativo a Linux.
5. **Aprovação Unânime do Gauntlet Loop:** Reviewers só convocados após a entrega dos 3 Builders.
