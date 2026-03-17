using System;
using System.IO;
using System.Windows;
using System.Globalization;
using Gigasoft.ProEssentials;
using Gigasoft.ProEssentials.Enums;
using System.Windows.Media;

namespace HeatmapSpectrogram
{
    /// <summary>
    /// ProEssentials WPF Heatmap — Spectrogram — 2D Contour Chart
    ///
    /// Demonstrates a full-featured heatmap / spectrogram / 2D contour
    /// visualization using PesgoWpf — the ProEssentials scientific graph
    /// object for continuous numeric X and Y axes.
    ///
    /// Note: this example uses PesgoWpf (Scientific Graph), not PegoWpf
    /// (Graph). PesgoWpf is the correct choice whenever both X and Y axes
    /// carry continuous numeric values — as in frequency vs time data.
    /// PegoWpf is for categorical X-axis data (labels, dates, bar charts).
    ///
    /// Features:
    ///   - 183 subsets × 512 points = 93,696 data values (Z dimension)
    ///   - ContourColors plotting method — fills regions between contour
    ///     lines with interpolated color — the standard heatmap/spectrogram
    ///     visualization technique
    ///   - Log Y axis scale — matches the logarithmic nature of frequency data
    ///   - BlueCyanGreenYellowBrownWhite contour color scale
    ///   - DuplicateDataX / DuplicateDataY — X and Y arrays stored once,
    ///     duplicated internally — avoids passing 93,696 X and Y values
    ///   - ComputeShader — GPU-accelerated contour rendering
    ///   - Direct3D render engine with Composite2D3D foreground overlay
    ///   - XYZ cursor prompt — hover to read X, Y, Z values at mouse position
    ///   - Mouse wheel zoom, horizontal and vertical scroll zoom
    ///   - Contour color legend on the left
    ///
    /// Data file:
    ///   Heatmap.txt — 93,696 lines, tab-delimited X/Y/Z values
    ///   183 rows (subsets) × 512 columns (points)
    ///   X = frequency offset, Y = frequency (log scale), Z = signal amplitude
    ///
    /// Controls:
    ///   Left-click drag   — zoom box
    ///   Mouse wheel       — horizontal + vertical zoom
    ///   Right-click       — context menu (export, print, customize)
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // -----------------------------------------------------------------------
        // Pesgo1_Loaded — chart initialization
        //
        // Always initialize ProEssentials in the control's Loaded event.
        // Do NOT initialize in the Window's Loaded event — the window fires
        // before the control is fully initialized.
        // -----------------------------------------------------------------------
        void Pesgo1_Loaded(object sender, RoutedEventArgs e)
        {
            // =======================================================================
            // Step 1 — Declare data dimensions
            //
            // 183 subsets × 512 points = 93,696 Z values (the heatmap intensity).
            //
            // DuplicateDataX = PointIncrement: only one row of X values stored.
            //   The chart sets X[s,p] = X[0,p] for all subsets automatically.
            //   Saves storing 183 × 512 = 93,696 X values.
            //
            // DuplicateDataY = SubsetIncrement: only one column of Y values stored.
            //   The chart sets Y[s,p] = Y[0,s] for all points automatically.
            //   Saves storing another 93,696 Y values.
            //   Note: with SubsetIncrement, Y[0,s] holds the Y value for subset s.
            //
            // Pre-allocating the last element of each array before spoon-feeding
            // guarantees the internal arrays are fully sized before the load loop,
            // preventing incremental reallocations.
            // =======================================================================
            Pesgo1.PeData.Subsets = 183;
            Pesgo1.PeData.Points  = 512;

            Pesgo1.PeData.DuplicateDataX = DuplicateData.PointIncrement;
            Pesgo1.PeData.DuplicateDataY = DuplicateData.SubsetIncrement;

            Pesgo1.PeData.X[0, 511]   = 0; // pre-allocate X array (512 values, one per point)
            Pesgo1.PeData.Y[0, 182]   = 0; // pre-allocate Y array (183 values, one per subset)
            Pesgo1.PeData.Z[182, 511] = 0; // pre-allocate Z array (183 × 512 heatmap values)

            // =======================================================================
            // Step 2 — Load Heatmap.txt
            //
            // Tab-delimited file: X \t Y \t Z per line, 93,696 lines total.
            // Data is ordered left-to-right across columns (points), then
            // top-to-bottom across rows (subsets).
            //
            // X[0, nPointCount]: frequency offset for each column — loaded once
            //   from the first subset row only (nSubsetCount == 0).
            //
            // Y[0, nSubsetCount]: frequency for each row — loaded once from the
            //   first column only (nPointCount == 0), scaled to be more log-like
            //   to match the YAxisScaleControl = Log setting.
            //
            // Z[nSubsetCount, nPointCount]: signal amplitude — loaded every row.
            // =======================================================================
            int nSubsetCount = 0;
            int nPointCount  = 0;

            string[] fileArray = { "", "" };
            try
            {
                fileArray = File.ReadAllLines("Heatmap.txt");
            }
            catch
            {
                MessageBox.Show(
                    "Heatmap.txt not found.\n\nMake sure Heatmap.txt is in the same folder as the executable.",
                    "File Not Found", MessageBoxButton.OK);
                Application.Current.Shutdown();
                return;
            }

            for (int i = 0; i < fileArray.Length; i++)
            {
                string line = fileArray[i];
                if (line.Length < 3) continue;

                var columns = line.Split('\t');
                float fX = float.Parse(columns[0], CultureInfo.InvariantCulture.NumberFormat);
                float fY = float.Parse(columns[1], CultureInfo.InvariantCulture.NumberFormat);
                float fZ = float.Parse(columns[2], CultureInfo.InvariantCulture.NumberFormat);

                // X axis values — one per point column, loaded from first subset row only
                if (nSubsetCount == 0)
                    Pesgo1.PeData.X[0, nPointCount] = fX + 20.0F;

                // Y axis values — one per subset row, loaded from first column only.
                // Scaled to be more logarithmic to match YAxisScaleControl = Log.
                if (nPointCount == 0)
                    Pesgo1.PeData.Y[0, nSubsetCount] = fY * (i + 1000) / 100.0F;

                // Z value — heatmap intensity at this subset/point position
                Pesgo1.PeData.Z[nSubsetCount, nPointCount] = fZ;

                nPointCount++;
                if (nPointCount > 511)
                {
                    nPointCount = 0;
                    nSubsetCount++;
                }
            }

            // =======================================================================
            // Step 3 — Axis scale
            //
            // Log scale on the Y axis matches the logarithmic spacing of
            // frequency data — equal visual distance per octave rather than
            // equal visual distance per Hz.
            // =======================================================================
            Pesgo1.PeGrid.Configure.YAxisScaleControl = ScaleControl.Log;

            // =======================================================================
            // Step 4 — Visual styling — dark theme
            // =======================================================================
            Pesgo1.PeColor.BitmapGradientMode = true;
            Pesgo1.PeColor.QuickStyle         = QuickStyle.DarkNoBorder;
            Pesgo1.PeColor.GridBold           = true;

            // =======================================================================
            // Step 5 — Contour color plotting method
            //
            // ContourColors fills the regions between contour lines with
            // interpolated color — this is the standard heatmap / spectrogram
            // visualization. Each Z value maps to a color in the contour scale.
            //
            // ContourColorsShadows = true adds subtle shading between contour
            // bands for a smoother, more continuous appearance.
            //
            // ContourColorBlends = 10 controls how many interpolation steps
            // are used between each pair of contour colors.
            //
            // ContourColorSet defines the color ramp:
            //   Blue (low Z) → Cyan → Green → Yellow → Brown → White (high Z)
            //   This is the standard scientific spectrogram color scale.
            // =======================================================================
            Pesgo1.PePlot.Allow.ContourColors        = true;
            Pesgo1.PePlot.Allow.ContourColorsShadows = true;

            Pesgo1.PeColor.ContourColorBlends = 10;
            // ContourColorBlends must always be set BEFORE ContourColorSet
            Pesgo1.PeColor.ContourColorSet = ContourColorSet.BlueCyanGreenYellowBrownWhite;

            Pesgo1.PeLegend.ContourLegendPrecision = ContourLegendPrecision.ZeroDecimals;
            Pesgo1.PeLegend.ContourStyle           = true;

            // ContourColors is the plotting method — renders the 2D heatmap fill
            Pesgo1.PePlot.Method = SGraphPlottingMethod.ContourColors;

            Pesgo1.PeUserInterface.Menu.DataShadow = MenuControl.Hide;

            // =======================================================================
            // Step 6 — Zoom and interaction
            //
            // HorizontalVerticalZoom: mouse wheel zooms both axes simultaneously
            // — natural behavior for a 2D heatmap where both axes are meaningful.
            //
            // ScrollingHorzZoom / ScrollingVertZoom: enables scrollbar-based zoom
            // on each axis independently after a zoom box selection.
            // =======================================================================
            Pesgo1.PeUserInterface.Scrollbar.MouseWheelZoomFactor    = 1.4F;
            Pesgo1.PeUserInterface.Scrollbar.MouseWheelZoomSmoothness = 2;
            Pesgo1.PeGrid.GridBands                                   = false;

            Pesgo1.PeUserInterface.Allow.ZoomStyle    = ZoomStyle.Ro2Not;
            Pesgo1.PeUserInterface.Allow.Zooming      = AllowZooming.HorzAndVert;
            Pesgo1.PeUserInterface.Scrollbar.MouseWheelFunction = MouseWheelFunction.HorizontalVerticalZoom;

            Pesgo1.PeUserInterface.Scrollbar.ScrollingVertZoom = true;
            Pesgo1.PeUserInterface.Scrollbar.ScrollingHorzZoom = true;

            // =======================================================================
            // Step 7 — Legend and grid
            // =======================================================================
            Pesgo1.PeLegend.Location  = LegendLocation.Left;

            // InFront = true draws grid lines on top of the contour fill
            // — keeps axis grid visible over the heatmap color
            Pesgo1.PeGrid.InFront     = true;
            Pesgo1.PeGrid.LineControl = GridLineControl.Both;
            Pesgo1.PeGrid.Style       = GridStyle.Dot;

            // =======================================================================
            // Step 8 — Disable non-contour plot methods from the right-click menu
            //
            // This is a heatmap-only chart — line, point, bar, area and other
            // plot methods are not meaningful for this data and are hidden from
            // the context menu to keep the UI clean.
            // =======================================================================
            Pesgo1.PePlot.Allow.Line              = false;
            Pesgo1.PePlot.Allow.Point             = false;
            Pesgo1.PePlot.Allow.Bar               = false;
            Pesgo1.PePlot.Allow.Area              = false;
            Pesgo1.PePlot.Allow.Spline            = false;
            Pesgo1.PePlot.Allow.SplineArea        = false;
            Pesgo1.PePlot.Allow.PointsPlusLine    = false;
            Pesgo1.PePlot.Allow.PointsPlusSpline  = false;
            Pesgo1.PePlot.Allow.BestFitCurve      = false;
            Pesgo1.PePlot.Allow.BestFitLine       = false;
            Pesgo1.PePlot.Allow.Stick             = false;

            // =======================================================================
            // Step 9 — Titles and fonts
            // =======================================================================
            Pesgo1.PeString.MainTitle = "Wave Data - Heatmap - Spectrogram Example";
            Pesgo1.PeString.SubTitle  = "";

            // AutoMinMaxPadding = 0: contour fills to the exact edge of the grid
            // — no gap between the heatmap and the axis lines
            Pesgo1.PeGrid.Configure.AutoMinMaxPadding = 0;

            Pesgo1.PeFont.FontSize = Gigasoft.ProEssentials.Enums.FontSize.Large;
            Pesgo1.PeFont.Fixed    = true;

            // Disable dialog tabs not relevant for contour-only charts
            Pesgo1.PeUserInterface.Dialog.Axis    = false;
            Pesgo1.PeUserInterface.Dialog.Style   = false;
            Pesgo1.PeUserInterface.Dialog.Subsets = false;

            Pesgo1.PeConfigure.TextShadows    = TextShadows.BoldText;
            Pesgo1.PeFont.MainTitle.Bold      = true;
            Pesgo1.PeFont.SubTitle.Bold       = true;
            Pesgo1.PeFont.Label.Bold          = true;

            // =======================================================================
            // Step 10 — Export defaults
            // =======================================================================
            Pesgo1.PeSpecial.DpiX = 600;
            Pesgo1.PeSpecial.DpiY = 600;
            Pesgo1.PeUserInterface.Dialog.AllowEmfExport  = false;
            Pesgo1.PeUserInterface.Dialog.AllowWmfExport  = false;
            Pesgo1.PeUserInterface.Dialog.ExportSizeDef  = ExportSizeDef.NoSizeOrPixel;
            Pesgo1.PeUserInterface.Dialog.ExportTypeDef  = ExportTypeDef.Png;
            Pesgo1.PeUserInterface.Dialog.ExportDestDef  = ExportDestDef.Clipboard;
            Pesgo1.PeUserInterface.Dialog.ExportUnitXDef = "1280";
            Pesgo1.PeUserInterface.Dialog.ExportUnitYDef = "768";
            Pesgo1.PeUserInterface.Dialog.ExportImageDpi = 300;

            // =======================================================================
            // Step 11 — Rendering engine
            //
            // Composite2D3D.Foreground: renders the 2D contour fill using the
            // Direct3D GPU pipeline, then composites the 2D axis/grid/labels
            // on top in the foreground — best combination of GPU performance
            // and crisp 2D text/grid rendering.
            //
            // ComputeShader = true: GPU compute shaders accelerate the contour
            // color interpolation calculations — significant speedup for large
            // heatmap datasets.
            // =======================================================================
            Pesgo1.PeConfigure.Composite2D3D = Composite2D3D.Foreground;
            Pesgo1.PeConfigure.RenderEngine  = RenderEngine.Direct3D;
            Pesgo1.PeData.ComputeShader      = true;

            // =======================================================================
            // Step 12 — Cursor prompt
            //
            // XYZValues shows the X frequency, Y frequency, and Z amplitude
            // values at the current mouse position — essential for reading
            // specific values off a heatmap.
            //
            // HourGlassThreshold = 9999999 suppresses the hourglass cursor
            // during rendering — keeps the cursor responsive.
            // =======================================================================
            Pesgo1.PeUserInterface.Cursor.PromptTracking      = true;
            Pesgo1.PeUserInterface.Cursor.PromptStyle         = CursorPromptStyle.XYZValues;
            Pesgo1.PeUserInterface.Cursor.PromptLocation      = CursorPromptLocation.Text;
            Pesgo1.PeUserInterface.Cursor.HourGlassThreshold  = 9999999;

            // Force GPU to rebuild color and vertex data on first render
            Pesgo1.PeFunction.Force3dxNewColors      = true;
            Pesgo1.PeFunction.Force3dxVerticeRebuild = true;

            // ReinitializeResetImage applies all properties and renders.
            // Always call as the final step.
            Pesgo1.PeFunction.ReinitializeResetImage();
            Pesgo1.Invalidate();
        }

        // -----------------------------------------------------------------------
        // Window_Closing
        // -----------------------------------------------------------------------
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
    }
}
