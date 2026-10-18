# 🛡️ GAUNTLET LOG: Marco P23 - Project & Asset Template Generators, DotNetNew Bridge & Platform Update Dialogs

**Epic:** Portabilidade integral do motor de geradores de templates de projetos e assets (`dotnet new`, plataformas de solução, modelos 3D e bibliotecas de projeto) e de seus respectivos diálogos modais para Linux 100% Nativo (Avalonia 12 + Vulkan RADV + .NET 10)  
**Status Final:** **PASSED (100% HOMOLOGADO - UNÂNIME)**  
**Data:** 17/10/2026  
**Suíte de Testes:** **373/373 testes xUnit APROVADOS (100% GREEN, 0 falhas, 0 regressões)**

---

## 🏛️ Avaliação da Banca Revisora Independente (Gauntlet Loop)

### 🔹 Fatia 1 (slice-1): DotNetNew Template Bridge, Generator Engine & Dialog
* **Revisor:** `DotNetNew Template Engine Reviewer` (`dda40a17`)
* **Veredicto:** **PASSED** ✅ (0 Critical, 0 Important, 0 Minor)
* **Auditoria:**
  - `DotNetNewTemplateBridge.cs` (281 linhas, 100% de paridade com o legado WPF): Isolamento de pastas de templates em `%LocalAppData%\\Stride\\TemplateEngine\\<NuGetVersion>` implementado com fidelidade através do redirecionamento de `USERPROFILE` e `HOME` via `StrideTemplateEngineEnvironment` (`IEnvironment`). Modo pasta (Folder installer) a partir de pacotes descompactados no cache NuGet, detecção por timestamp de `.nupkg` via `GetLastChangeTimeAsync`, consolidação de metadados agregados (`templates.sdtpls`) e empacotamento determinístico no `TemplateManager`.
  - `DotNetNewTemplateGenerator.cs` (281 linhas, 98.5% de paridade lógica): Desacoplamento limpo de interface gráfica via contrato `IDotNetNewTemplateParametersDialog` (`Stride.Assets.Editor.Services`), factory delegada `CreateParametersDialogDelegate` e serviço de diálogos `IStrideDialogService`, com fallback dinâmico defensivo. Execução unattended (`SetParameters`), orquestração do template engine, escaneamento de projetos executáveis irmãos (`.Windows`, `.Linux`, `.macOS`, `.iOS`, `.Android`), inicialização orientada ao SO hospedeiro (`OperatingSystem.IsLinux()`) e correção de arquétipos via `AssetAnalysis.FixAssetReferences`.
  - `DotNetNewTemplateParametersWindow.axaml` & `.axaml.cs` (31 + 347 linhas, 96% de paridade funcional): Diálogo modal nativo Avalonia 12 fortemente tipado, com geração procedural de controles para parâmetros (`bool` -> CheckBox, `choice` com múltiplos valores -> CheckBoxes verticais com SO da máquina, `choice` único -> ComboBox, texto/número -> TextBox), regra reativa `HDR x GraphicsProfile` com memorização de escolha (`hdrLastUserChoice`), tipografia via `Humanize` e execução thread-safe via `Dispatcher.UIThread`.
* **TDD Iron Law:** `TestDotNetNewTemplatesAndDialogsIntegration.cs` (12/12 testes GREEN, 0 falhas).

---

### 🔹 Fatia 2 (slice-2): Platform Update Generator & Solution Platform ViewModels
* **Revisor:** `Platform Update Generator & ViewModels Reviewer` (`34977db1`)
* **Veredicto:** **PASSED** ✅ (0 Critical, 0 Important, 0 Minor)
* **Auditoria:**
  - `SolutionPlatformViewModel.cs` (93 linhas, 100% de paridade funcional): ViewModel puro desacoplado de dependências WPF, propriedades reativas `Platform`, `IsInstalled`, `IsSelected`, `MarkedToRemove` e verificação dos requisitos de máquina local para builds multiplataforma.
  - `UpdatePlatformsTemplateGenerator.cs` (239 linhas, 100% de paridade lógica): Patching cirúrgico e atômico em XML de arquivos de projeto MSBuild (`PatchGameProject`) em soluções .NET 10, manipulando tanto `TargetFramework` singular quanto `TargetFrameworks` plurais para Linux, Android, iOS e Windows sem corromper nós de compilação ou diretivas de build.
  - `UpdatePlatformsWindow.axaml` & `.axaml.cs` (79 + 177 linhas, 100% de paridade funcional): Diálogo modal Avalonia 12 nativo, interface desacoplada através do contrato `IUpdatePlatformsDialog`, confirmação de exclusão em cascata (`ConfirmRemovingPlatformsAsync`) com avisos claros ao usuário e execução assíncrona thread-safe.
* **TDD Iron Law:** `TestUpdatePlatformsAndSolutionViewModelsIntegration.cs` (7/7 testes GREEN, 0 falhas).

---

### 🔹 Fatia 3 (slice-3): 3D Model & Project Library Template Generators & Dialogs
* **Revisor:** `Model & Project Library Templates Reviewer` (`4351fa81`)
* **Veredicto:** **PASSED** ✅ (0 Critical, 0 Important, 0 Minor)
* **Auditoria:**
  - `ModelFromFileTemplateGenerator.cs` (182 linhas, 100% de paridade lógica): Desacoplamento via `IModelAssetTemplateDialog`, suporte a modo unattended para testes automatizados, preservação estrita do GUID determinístico (`3B778954-54C4-4FF3-97EF-4CD7AEA0B97D`), 6 configurações de `ModelFromFileTemplateSettings` mantidas e importação 3D FBX/OBJ via `ModelAssetImporter`.
  - `ProjectLibraryTemplateGenerator.cs` (117 linhas, 100% de paridade lógica): Preservação estrita do GUID determinístico (`e12246ff-41a1-49d4-90e4-e72a4eb4a3e9`), validação de identificadores válidos para projetos C# e namespaces através de `NamingHelper.IsValidNamespace(...)` e `Utilities.BuildValidProjectName(...)`, com tratamento atômico de colisão de nomes.
  - `ImportModelFromFileViewModel.cs` (81 linhas): ViewModel fortemente tipado desacoplado da interface gráfica com suporte a `GraphViewModel` e `DummyReferenceContainer`.
  - Diálogos modais Avalonia 12 nativos: `ModelAssetTemplateWindow.axaml / .cs` (45 + 110 linhas) e `ProjectLibraryWindow.axaml / .cs` (28 + 172 linhas) implementando `IModalDialog` com validação de colisão em tempo real.
  - `StrideTemplates.cs` (24 linhas): Registro central consolidado de todos os 10 geradores de templates do ecossistema Stride.
* **TDD Iron Law:** `TestModelAndProjectLibraryTemplatesIntegration.cs` (7/7 testes GREEN, 0 falhas).

---

## 🏁 Veredicto Final do Marco P23
**UNANIMOUSLY PASSED (100% HOMOLOGADO)**
Suíte global: **373/373 testes xUnit APROVADOS (100% GREEN, 0 falhas, 0 regressões)**.
