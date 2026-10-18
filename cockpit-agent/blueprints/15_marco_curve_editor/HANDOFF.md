# HANDOFF: Transição Marco P15 -> Marco P16

## 1. Conquistas do Marco P15
- Portabilidade completa e homologação da suíte de Curve Editor, Animation Keys e Curves para Linux nativo (Avalonia 12 + Vulkan RADV).
- 21 testes automatizados integrados cobrindo ViewModels de canais multidimensionais, eixos e renderizadores, pontos de controle e behaviors interativos de Avalonia 12 (21/21 GREEN).
- Paridade de linhas de 87% a 100% em todos os 19 arquivos portados/alinhados.

## 2. Ponto de Entrada para o Marco P16: Audio Preview, Sound & Spatial Emitters
- **Arquitetura Alvo**: Edição de assets de som (`SoundAsset`), pré-visualização de formas de onda de áudio (`SoundPreviewView`), decodificação de áudio nativa no Linux (via backend OpenAL / SDL / Stride.Audio), emissores espaciais 3D e controles de volume/pitch/looping.
- **Fatias Verticais**:
  - **Fatia 1 (slice-1)**: Audio ViewModels & Waveform Decoding (`SoundAssetViewModel`, decodificação de PCM/WAV/OGG e visualização de amostras de amplitude).
  - **Fatia 2 (slice-2)**: Audio Runtime & Linux Native Playback (`SoundPreviewGameService`, engine de playback com OpenAL/Vulkan, controle de canais e espacialização 3D).
  - **Fatia 3 (slice-3)**: Avalonia Waveform Control & Audio View (`SoundPreviewView.axaml`, renderizador de waveform em canvas Avalonia 12, scrubbing de playhead e controles de transporte).

## 3. Diretiva de Orquestração
- Operação estrita como Tech Leader / Staff Engineer: orquestração contínua despachando 3 subagentes Builders para as 3 fatias verticais, com revisão e homologação no Gauntlet.
