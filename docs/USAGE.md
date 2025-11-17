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

## Zero-copy / Row-major Buffer (Advanced)

When parsing large grid CSV files, copying the entire data into a `double[,]` can be expensive in both time and memory.
This library provides two advanced APIs to work with a row-major contiguous buffer of doubles which is faster and has lower memory overhead for many tasks.

1) `GetRowMajorBuffer(bool createIfMissing = false)`

- Returns `ReadOnlyMemory<double>` representing the contiguous row-major buffer that the parser uses internally.
- If the parser did not keep a buffer but `createIfMissing` is true, the method will allocate a pooled row-major buffer and copy `Data` into it.
- This method does not transfer ownership. The returned memory is valid only as long as the parser instance is alive and has not been disposed or had the buffer extracted.

Example (read-only):
```csharp
using (var p = await GridCsvParser.CreateAsync("samples/grid_sample_star.csv"))
{
    var mem = p.GetRowMajorBuffer(createIfMissing: true);
    if (!mem.IsEmpty)
    {
        var arr = mem.ToArray(); // copy if you need a long-lived buffer
        // Process arr as a row-major array; arr[row * width + col]
    }
}
```

2) `ExtractRowMajorBuffer(bool createIfMissing = false)`

- Transfers ownership of the internal row-major array to the caller and returns the `double[]` buffer. The caller must return the array to `ArrayPool<double>.Shared` once done.
- After extracting the buffer, the parser will no longer provide a `Data` 2D array; accessing `Data` will throw an `InvalidOperationException`.

Example (zero-copy, transfer ownership):
```csharp
var parser = await GridCsvParser.CreateAsync("samples/grid_sample_star.csv");
// Take ownership of the internal buffer (no copy)
var buf = parser.ExtractRowMajorBuffer(createIfMissing: true);

try
{
    // buf is row-major: index by row * width + col
    var v = buf[0];
}
finally
{
    // Return to ArrayPool when finished
    ArrayPool<double>.Shared.Return(buf);
    // Dispose parser if not needed anymore
    parser.Dispose();
}
```

Notes
- If you prefer to keep using `Data[,]` while also getting a copy of the row-major buffer, call `GetRowMajorBuffer(createIfMissing: true)` and then call `mem.ToArray()` to create your own copy.
- The parser implements `IDisposable`, and `Dispose()` will release any pooled resources retained by the parser. If you call `ExtractRowMajorBuffer()` and then call `Dispose()`, `Dispose()` will not free the transferred buffer; it is the caller's responsibility to return it to the pool.

