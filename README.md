# ProEssentials WPF Heatmap — Spectrogram — 2D Contour Chart

A ProEssentials v10 WPF .NET 8 demonstration of a full-featured heatmap,
spectrogram, and 2D contour chart using PesgoWpf — the ProEssentials
scientific graph object for continuous numeric X and Y axes.

![ProEssentials Heatmap Spectrogram](https://gigasoft.com/wpf-chart/screenshots/screen139.png)

➡️ [gigasoft.com/examples/139](https://gigasoft.com/examples/139)

---

## What This Demonstrates

183 subsets × 512 points = **93,696 Z values** rendered as a smooth
interpolated color heatmap with a logarithmic frequency Y axis — the
classic spectrogram layout used in signal analysis, acoustics, RF
engineering, and vibration analysis.

---

## PesgoWpf vs PegoWpf — Choosing the Right Control

This example uses **PesgoWpf** (Scientific Graph), not PegoWpf (Graph).

| Control | X Axis | Best For |
|---------|--------|----------|
| **PesgoWpf** | Continuous numeric | Heatmap, spectrogram, scatter, contour, scientific XY |
| PegoWpf | Categorical / date | Bar, OHLC, labeled line, financial |

When both X and Y carry continuous numeric values — as in frequency vs
time — PesgoWpf is the correct choice.

---

## ProEssentials Features Demonstrated

### ContourColors — The Heatmap Plotting Method

```csharp
Pesgo1.PePlot.Allow.ContourColors        = true;
Pesgo1.PePlot.Allow.ContourColorsShadows = true;
Pesgo1.PeColor.ContourColorBlends        = 10;  // set BEFORE ContourColorSet
Pesgo1.PeColor.ContourColorSet           = ContourColorSet.BlueCyanGreenYellowBrownWhite;
Pesgo1.PePlot.Method                     = SGraphPlottingMethod.ContourColors;
```

`ContourColors` fills the regions between contour lines with interpolated
color — the standard heatmap/spectrogram technique. Each Z value maps to
a color in the `BlueCyanGreenYellowBrownWhite` scale:

- **Blue** — low amplitude
- **Cyan → Green → Yellow** — mid range
- **Brown → White** — high amplitude

`ContourColorBlends` must always be set **before** `ContourColorSet`.

### Log Y Axis Scale

```csharp
Pesgo1.PeGrid.Configure.YAxisScaleControl = ScaleControl.Log;
```

Logarithmic Y axis gives equal visual space per octave — standard for
frequency data where the perceptually important differences are
proportional, not absolute.

### DuplicateData — Efficient Axis Storage

```csharp
Pesgo1.PeData.DuplicateDataX = DuplicateData.PointIncrement;
Pesgo1.PeData.DuplicateDataY = DuplicateData.SubsetIncrement;
```

Only one row of X values (512) and one column of Y values (183) are
stored. The chart duplicates them internally — avoids allocating and
passing 93,696 X and 93,696 Y values separately.

### ComputeShader + Direct3D

```csharp
Pesgo1.PeConfigure.Composite2D3D = Composite2D3D.Foreground;
Pesgo1.PeConfigure.RenderEngine  = RenderEngine.Direct3D;
Pesgo1.PeData.ComputeShader      = true;
```

`Composite2D3D.Foreground` renders the contour fill using the Direct3D
GPU pipeline, then composites the 2D axis, grid, and labels on top —
combining GPU performance with crisp 2D text rendering.

`ComputeShader` delegates contour color interpolation to the GPU —
significant speedup for large heatmap datasets.

### XYZ Cursor Prompt

```csharp
Pesgo1.PeUserInterface.Cursor.PromptTracking = true;
Pesgo1.PeUserInterface.Cursor.PromptStyle    = CursorPromptStyle.XYZValues;
Pesgo1.PeUserInterface.Cursor.PromptLocation = CursorPromptLocation.Text;
```

Hover over the heatmap to read the exact X frequency, Y frequency, and
Z amplitude at the cursor position — displayed as text inside the chart.

### Pre-allocation Pattern

```csharp
Pesgo1.PeData.X[0, 511]   = 0; // pre-allocate X
Pesgo1.PeData.Y[0, 182]   = 0; // pre-allocate Y
Pesgo1.PeData.Z[182, 511] = 0; // pre-allocate Z
```

Setting the last element of each array before the load loop guarantees
the internal arrays are fully sized upfront — no incremental
reallocations during data load.

---

## Data File

`Heatmap.txt` — 93,696 lines, tab-delimited X/Y/Z per line.
183 rows (subsets) × 512 columns (points).

- **X** — frequency offset per column
- **Y** — frequency per row (scaled for log axis)
- **Z** — signal amplitude (the heatmap color value)

Copied to the output directory automatically on build.

---

## Controls

| Input | Action |
|-------|--------|
| Left-click drag | Zoom box |
| Mouse wheel | Horizontal + vertical zoom |
| Right-click | Context menu — export, print, customize |

---

## Prerequisites

- Visual Studio 2022
- .NET 8 SDK
- Internet connection for NuGet restore

---

## How to Run

```
1. Clone this repository
2. Open HeatmapSpectrogram.sln in Visual Studio 2022
3. Build → Rebuild Solution (NuGet restore is automatic)
4. Press F5
```

---

## NuGet Package

References
[`ProEssentials.Chart.Net80.x64.Wpf`](https://www.nuget.org/packages/ProEssentials.Chart.Net80.x64.Wpf).
Package restore is automatic on build.

---

## Related Examples

- [WPF Quickstart — Simple Scientific Graph](https://github.com/GigasoftInc/wpf-chart-quickstart-proessentials)
- [Financial OHLC — Trading Signals](https://github.com/GigasoftInc/wpf-chart-financial-ohlc-trading-signals-proessentials)
- [3D Realtime Surface — ComputeShader](https://github.com/GigasoftInc/wpf-3d-surface-realtime-computeshader-proessentials)
- [All Examples — GigasoftInc on GitHub](https://github.com/GigasoftInc)
- [Full Evaluation Download](https://gigasoft.com/net-chart-component-wpf-winforms-download)
- [gigasoft.com](https://gigasoft.com)

---

## License

Example code is MIT licensed. ProEssentials requires a commercial
license for continued use.
