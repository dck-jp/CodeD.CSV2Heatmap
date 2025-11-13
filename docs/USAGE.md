# Usage Guide

## Generating a Heatmap from Grid Data

Generates an image from Grid Data (GridCSV, 2D Map Data, Height/Surface/Intensity Maps, etc.).

```csharp
using CodeD;
using SkiaSharp;

// Load Grid Data asynchronously
var heatmap = await HeatmapRenderer.CreateAsync("grid_sample.txt");

// Convert to bitmap
SKBitmap bitmap = heatmap.ToBitmap(
    colorMode: HeatmapRenderer.ColorMode.Rainbow,
    convertMode: HeatmapRenderer.ConvertMode.None
);

// Save as image
using (var image = SKImage.FromBitmap(bitmap))
using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
using (var stream = File.OpenWrite("output.png"))
{
    data.SaveTo(stream);
}
```

## Processing Point Cloud (XYZ) Data

```csharp
using CodeD;

// Parse XYZ file asynchronously (use column 3 as Z value)
var parser = await XyzCsvParser.CreateAsync("xyz_sample.txt", zColNum: 3);

// Convert to grid data
double[,] gridData = parser.Data;

// Create HeatmapRenderer from grid data
var heatmap = new HeatmapRenderer(gridData);

// Convert to bitmap
var bitmap = heatmap.ToBitmap();
```

## Color Modes

```csharp
// Rainbow color (Blue → Green → Red)
var bitmap1 = heatmap.ToBitmap(colorMode: HeatmapRenderer.ColorMode.Rainbow);

// Monochrome (Black and White)
var bitmap2 = heatmap.ToBitmap(colorMode: HeatmapRenderer.ColorMode.Monochorome);

// Black → Purple → White
var bitmap3 = heatmap.ToBitmap(colorMode: HeatmapRenderer.ColorMode.BlackPurpleWhite);
```

## Data Conversion Modes

```csharp
// Linear scale (default)
var bitmap1 = heatmap.ToBitmap(convertMode: HeatmapRenderer.ConvertMode.None);

// Common logarithm conversion
var bitmap2 = heatmap.ToBitmap(convertMode: HeatmapRenderer.ConvertMode.log);

// Natural logarithm conversion
var bitmap3 = heatmap.ToBitmap(convertMode: HeatmapRenderer.ConvertMode.ln);
```

## Image Processing

```csharp
// Load data with pixel size specification (required for plane correction)
var heatmap = await HeatmapRenderer.CreateAsync("data.txt", pixelSize: 1.0);

// Plane correction
var corrected = heatmap.GetPlaneCorrection();

// Trimming (x0, y0, width, height)
var trimmed = heatmap.GetTrim(10, 10, 50, 50);

// Rotate 90 degrees clockwise
var rotatedCW = heatmap.GetRotateCW();

// Rotate 90 degrees counter-clockwise
var rotatedCCW = heatmap.GetRotateCCW();

// Rotate by arbitrary angle (in radians)
var rotated45 = heatmap.GetRotate(Math.PI / 4);
```

## Accessing Data

```csharp
// Get data dimensions
int xSize = heatmap.XSize;
int ySize = heatmap.YSize;

// Get maximum and minimum values
double max = heatmap.Max;
double min = heatmap.Min;

// Get specific row or column data
double[] rowData = heatmap.GetRowData(0);
double[] columnData = heatmap.GetColumnData(0);

// Direct access to 2D array
double value = heatmap.Data[x, y];

// Save to file
heatmap.SaveAs("output.txt");
```
