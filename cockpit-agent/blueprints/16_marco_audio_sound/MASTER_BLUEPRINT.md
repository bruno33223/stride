# MASTER BLUEPRINT: Marco P16 - Audio Preview, Sound & Spatial Emitters

## 1. Visão Geral e Arquitetura Alvo
O objetivo do **Marco P16** é a portabilidade, consolidação e homologação completa da suíte de áudio, reprodução e pré-visualização de sons do Stride Game Studio para Linux nativo (Avalonia 12 + Vulkan RADV + Stride.Audio OpenAL/SDL), garantindo paridade funcional estrita (85-100% de paridade de linhas de código) com o legado WPF/Win32.

O subsistema de Áudio compreende:
1. **Audio ViewModels & Templates**:
   - `SoundPreviewViewModel` com controle de transporte (Play, Pause, Stop), volume mestre, scrubbing de playhead e tempo decorrido/duração.
   - `SoundFromFileTemplateGenerator` para criação assistida de assets de áudio a partir de arquivos brutos (.wav, .ogg, .mp3) com extração de faixas e metadados de stream.
   - Templates de som: `DefaultSound.sdtpl`, `MusicSound.sdtpl`, `SpatializedSound.sdtpl`.
2. **Audio Runtime & Playback Services**:
   - `SoundPreview` (serviço game-side de pré-visualização e playback em viewport interativo com despachante assíncrono).
   - `SoundAssetEditorGameCompiler` (compilador de preview para o pipeline de montagem de som).
   - `SoundThumbnailCompiler` (geração de miniatura de asset com forma de onda de áudio).
3. **Avalonia Controls & Visual Audio View**:
   - `SoundPreviewView` em Avalonia 12 nativo, com barra de transporte, sliders de volume e playhead, cronômetro de precisão milissegundos e estado de carregamento assíncrono.
   - Gizmos 3D de áudio no viewport: `AudioEmitterGizmo` e `AudioListenerGizmo`.

---

## 2. Fatias Verticais (Vertical Slices)

### Fatia 1 (slice-1): Audio ViewModels & Asset Templates
- **Arquivos a portar/alinhar**:
  - `Stride.Assets.Editor/ViewModels/Preview/SoundPreviewViewModel.cs` (82 linhas)
  - `Stride.Assets.Editor/Templates/SoundFromFileTemplateGenerator.cs` (71 linhas)
  - `Stride.Assets.Editor/Templates/Assets/Media/DefaultSound.sdtpl`
  - `Stride.Assets.Editor/Templates/Assets/Media/MusicSound.sdtpl`
  - `Stride.Assets.Editor/Templates/Assets/Media/SpatializedSound.sdtpl`
- **Validação TDD**: Teste integrado `TestAudioViewModelsAndTemplatesIntegration.cs` cobrindo geração de assets de som, parâmetros de template e comandos de transporte do ViewModel.

### Fatia 2 (slice-2): Audio Runtime, Preview Service & Compilers
- **Arquivos a portar/alinhar**:
  - `Stride.Assets.Editor/Preview/SoundPreview.cs` (152 linhas)
  - `Stride.Editor/Preview/SoundAssetEditorGameCompiler.cs` (19 linhas)
  - `Stride.Assets.Editor/Thumbnails/SoundThumbnailCompiler.cs` (20 linhas)
- **Validação TDD**: Teste integrado `TestSoundPreviewGameServiceIntegration.cs` cobrindo ciclo de vida de reprodução de áudio, atualização de tempo e compilação de assets de som.

### Fatia 3 (slice-3): Avalonia Audio View & Interactive Controls
- **Arquivos a portar/alinhar**:
  - `Stride.Assets.Editor.Avalonia/Views/Preview/SoundPreviewView.cs` (e template visual em Avalonia 12 nativo com botões de transporte, scrubbing de tempo e barra de volume)
  - Gizmos espaciais e fábrica de entidades de áudio (`AudioEntityFactory.cs`, `AudioEmitterGizmo.cs`, `AudioListenerGizmo.cs`).
- **Validação TDD**: Teste integrado `TestAudioPreviewViewAndControlsIntegration.cs` cobrindo inicialização de visual, bindings de propriedades de áudio e responsividade de transporte.

---

## 3. Critérios de Homologação (Gauntlet Loop)
1. 0 erros de compilação em toda a solução sob .NET 10.
2. 100% dos testes unitários e de integração passando na suíte xUnit.
3. Testes RED criados antes de cada implementação conforme o Protocolo Obrigatório de Bugs / TDD Iron Law.
4. Paridade estrita de linhas de código preservada sem resumos artificiais (85-100%).
5. Atualização de `MAPPING_TASKLIST.tsv` e `MAPPING_README.md`.
