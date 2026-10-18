# MASTER BLUEPRINT: Marco P15 - Curve Editor, Animation Keys & Curves (Evaluation, Tangents & Avalonia Canvas)

## 1. Visão Geral e Arquitetura Alvo
O objetivo do **Marco P15** é a portabilidade, consolidação e homologação completa da suíte de edição visual de curvas de animação e chaves do Stride Game Studio para Linux nativo (Avalonia 12 + Vulkan RADV), garantindo paridade funcional estrita (85-100% de paridade de linhas de código) com o legado WPF/Win32.

O subsistema Curve Editor provê edição visual precisa para:
1. **Curvas de Animação e Interpolação**: Chaves de animação escalar (`float`), vetorial (`Vector2`, `Vector3`, `Vector4`), rotação (`Quaternion`/Euler) e cores (`Color3`, `Color4`).
2. **Sistema de Eixos e Grade Dinâmica**: Eixos cartesianos independentes (X/tempo em frames/segundos, Y/valores e amplitudes) com suporte linear e logarítmico, auto-fit, pan e zoom infinito.
3. **Controle de Tangentes e Pontos de Controle**: Manipulação de tangentes de entrada e saída (Linear, Hermite, Bezier, Step), quebra de tangentes e edição coordenada de múltiplos canais.
4. **Renderização Vetorial e Interação Avalonia 12**: Canvas de renderização de curvas de alta performance, manipulação interativa via ponteiros/gestos e templates de computação de curva.

---

## 2. Fatias Verticais (Vertical Slices)

### Fatia 1 (slice-1): Curve Editor Core ViewModels & Multi-Dimensional Curves
- **Objetivo**: Completar os modelos de desenho de curvas, fábrica de curvas Quantum e suporte a canais vetoriais, de rotação e cores.
- **Arquivos a portar/alinhar**:
  - `Stride.Assets.Editor/AssetEditors/CurveEditor/ViewModels/CurveEditorViewModel.DrawingModel.cs` (507 linhas fiéis)
  - `Stride.Assets.Editor/AssetEditors/CurveEditor/ViewModels/CurveEditorViewModel.CurveFactory.cs` (116 linhas fiéis)
  - `Stride.Assets.Editor/AssetEditors/CurveEditor/ViewModels/VectorComponentCurveViewModel.cs` (197 linhas)
  - `Stride.Assets.Editor/AssetEditors/CurveEditor/ViewModels/VectorKeyFrameCurveViewModel.cs` (54 linhas)
  - `Stride.Assets.Editor/AssetEditors/CurveEditor/ViewModels/RotationKeyFrameCurveViewModel.cs` (25 linhas)
  - `Stride.Assets.Editor/AssetEditors/CurveEditor/ViewModels/ColorKeyFrameCurveViewModel.cs` (24 linhas)
  - Alinhamento de paridade dos ViewModels existentes (`CurveEditorViewModel.cs`, `CurveViewModelBase.cs`, `KeyFrameCurveViewModel.cs`, `FloatCurveViewModel.cs`).
- **Validação TDD**: Teste integrado `TestCurveEditorViewModelsIntegration.cs` com cenários de criação, decomposição de canais e cálculo de chaves.

### Fatia 2 (slice-2): Axes, Zoom, Pan & Range Renderers
- **Objetivo**: Renderização completa de eixos horizontais e verticais, medição de ticks principais/secundários, zoom e pan precisos.
- **Arquivos a portar/alinhar**:
  - `Stride.Assets.Editor/AssetEditors/CurveEditor/ViewModels/Axes/HorizontalAndVerticalAxisRenderer.cs` (495 linhas fiéis)
  - `Stride.Assets.Editor/AssetEditors/CurveEditor/ViewModels/Axes/AxisBase.cs` (garantir paridade completa com 1138 linhas)
  - `Stride.Assets.Editor/AssetEditors/CurveEditor/ViewModels/Axes/AxisChangedEventArgs.cs` (93 linhas)
  - `Stride.Assets.Editor/AssetEditors/CurveEditor/ViewModels/Axes/LinearAxis.cs` (26 linhas)
  - `Stride.Assets.Editor/AssetEditors/CurveEditor/ViewModels/Axes/LogarithmicAxis.cs` (28 linhas)
- **Validação TDD**: Teste integrado `TestCurveAxesAndRenderingIntegration.cs` com conversão de coordenadas tela-espaço, cálculo de ticks e limites de eixos.

### Fatia 3 (slice-3): Control Points, Avalonia Canvas & Interaction Behaviors
- **Objetivo**: Pontos de controle de componentes específicos, controles de desenho e comportamentos de manipulação interativa em Avalonia 12.
- **Arquivos a portar/alinhar**:
  - `Stride.Assets.Editor/AssetEditors/CurveEditor/ViewModels/ControlPoints/VectorComponentControlPointViewModel.cs` (184 linhas)
  - `Stride.Assets.Editor/AssetEditors/CurveEditor/ViewModels/ControlPoints/ColorComponentControlPointViewModel.cs` (77 linhas)
  - `Stride.Assets.Editor/AssetEditors/CurveEditor/ViewModels/ControlPoints/RotationComponentControlPointViewModel.cs` (69 linhas)
  - `Stride.Assets.Editor/AssetEditors/CurveEditor/ViewModels/ControlPoints/FloatControlPointViewModel.cs` (27 linhas)
  - `Stride.Assets.Editor.Avalonia/Views/Behaviors/AxisBehavior.cs` (59 linhas)
  - `Stride.Assets.Editor.Avalonia/Views/Behaviors/AxisPaneBehavior.cs` (149 linhas)
  - `Stride.Assets.Editor.Avalonia/Views/Behaviors/AxisZoomBehavior.cs` (80 linhas)
  - `Stride.Assets.Editor.Avalonia/Views/CurveCanvasControl.cs` & `CurveEditorView.axaml`
- **Validação TDD**: Teste integrado `TestCurveControlPointsAndBehaviorsIntegration.cs` testando tangentes, arrasto de pontos e sincronização com canvas.

---

## 3. Critérios de Homologação (Gauntlet Loop)
1. 0 erros de compilação em toda a solução sob .NET 10.
2. 100% dos testes unitários e de integração passando na suíte xUnit.
3. Testes RED criados antes de cada implementação conforme o Protocolo Obrigatório de Bugs / TDD Iron Law.
4. Paridade estrita de linhas de código preservada sem resumos artificiais (85-100%).
5. Atualização de `MAPPING_TASKLIST.tsv` e `MAPPING_README.md`.
