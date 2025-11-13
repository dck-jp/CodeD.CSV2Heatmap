# CodeD.CSV2Heatmap

[日本語](README.ja.md) | English

This is a .NET library that generates heatmap images from 2D map data in CSV format. It is ideal for visualizing scientific measurement data such as surface roughness measurements, X‑ray focusing intensity distributions, and point‑cloud data.

**Supported Data Formats:**
- **Grid Data** (Height Map / Surface Map / Intensity Map / Raster Map)  
- **Point Cloud Data** (XYZ Format / Scatter Data / Tabular XYZ Data)

Both grid‑based and XYZ‑based data structures are supported, enabling efficient image generation through fast parallel processing and SIMD optimization.

[![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-blue.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

## Features

- 📊 **Supports multiple data formats**: Grid Data and Point Cloud Data (XYZ Format)
- 🎨 **Multiple color modes**: Rainbow, Monochrome, Black‑Purple‑White
- 🚀 **High performance**: Parallel processing + SIMD‑optimized bitmap generation
- 🔧 **Image processing utilities**: Plane correction, rotation, trimming, and more
- 📈 **Data conversion**: Linear, logarithmic, and natural‑log scales
- 🔌 **Powered by SkiaSharp**: Cross‑platform graphics backend

## Use Cases

- Surface roughness data (Surface Map / Height Map)
- X‑ray intensity distribution (Intensity Map / Raster Map)
- Visualization of temperature or concentration fields
- 2D projection of point‑cloud data
- Gridding and visualization of scattered data
- Any other 2D map (Grid / Raster) data

## Installation

### NuGet Package (planned)

```bash
dotnet add package CodeD.CSV2Heatmap
```

### Manual Build

```bash
git clone https://github.com/dck-jp/CodeD.CSV2Heatmap.git
cd CodeD.CSV2Heatmap
dotnet build src/CodeD.CSV2Heatmap/CodeD.CSV2Heatmap.csproj -c Release
```

## Requirements

- .NET Standard 2.0 or later  
- SkiaSharp 2.88.8 or later

## Usage

### Generating a Heatmap from Grid Data

Generates an image from Grid Data (GridCSV, 2D Map Data, Height/Surface/Intensity Maps, etc.).

```csharp
using CodeD;
using SkiaSharp;

var heatmap = new HeatmapRenderer("grid_sample.txt");

SKBitmap bitmap = heatmap.ToBitmap(
    colorMode: HeatmapRenderer.ColorMode.Rainbow,
    convertMode: HeatmapRenderer.ConvertMode.None
);

using (var image = SKImage.FromBitmap(bitmap))
using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
using (var stream = File.OpenWrite("output.png"))
{
    data.SaveTo(stream);
}
```

### Processing Point Cloud (XYZ) Data

```csharp
using CodeD;

var parser = new XyzCsvParser("xyz_sample.txt", zColNum: 3);

double[,] gridData = parser.Data;

var heatmap = new HeatmapRenderer(gridData);

var bitmap = heatmap.ToBitmap();
```

### Color Modes

```csharp
var bitmap1 = heatmap.ToBitmap(colorMode: HeatmapRenderer.ColorMode.Rainbow);
var bitmap2 = heatmap.ToBitmap(colorMode: HeatmapRenderer.ColorMode.Monochorome);
var bitmap3 = heatmap.ToBitmap(colorMode: HeatmapRenderer.ColorMode.BlackPurpleWhite);
```

### Data Conversion Modes

```csharp
var bitmap1 = heatmap.ToBitmap(convertMode: HeatmapRenderer.ConvertMode.None);
var bitmap2 = heatmap.ToBitmap(convertMode: HeatmapRenderer.ConvertMode.log);
var bitmap3 = heatmap.ToBitmap(convertMode: HeatmapRenderer.ConvertMode.ln);
```

### Image Processing

```csharp
var heatmap = new HeatmapRenderer("data.txt", pixelSize: 1.0);
var corrected = heatmap.GetPlaneCorrection();
var trimmed = heatmap.GetTrim(10, 10, 50, 50);
var rotatedCW = heatmap.GetRotateCW();
var rotatedCCW = heatmap.GetRotateCCW();
var rotated45 = heatmap.GetRotate(Math.PI / 4);
```

### Accessing Data

```csharp
int xSize = heatmap.XSize;
int ySize = heatmap.YSize;

double max = heatmap.Max;
double min = heatmap.Min;

double[] rowData = heatmap.GetRowData(0);
double[] columnData = heatmap.GetColumnData(0);

double value = heatmap.Data[x, y];

heatmap.SaveAs("output.txt");
```

## Supported File Formats

### Grid Data (GridCSV / 2D Map Data)

Represents numerical data arranged in a grid—commonly used for surface roughness, X‑ray intensity, and raster data.

```
1.0  2.0  3.0  4.0
2.5  3.5  4.5  5.5
3.0  4.0  5.0  6.0
```

- **Delimiters**: Tab, comma, space  
- **Headers**: Rows containing strings are treated as headers  
- **Data**: Numeric rows only

### Point Cloud Data (XYZ Format)

Contains X, Y, and Z values (multiple Z columns allowed).

```
X   Y   Z1    Z2    Z3
0   0   10.5  20.0  30.5
1   0   11.0  21.5  31.0
0   1   12.0  22.0  32.0
```

- **Delimiters**: Tab, comma, space  
- **Columns**: X, Y, Z (multi‑value supported)  
- **Conversion**: Automatically transformed into grid data  

**Note:** Assumes regularly spaced grid points. Irregular point-cloud data may not interpolate correctly.

## Performance

Optimizations used:

- **Parallel processing**  
- **SIMD acceleration (Vector<T>)**  
- **Asynchronous file I/O**  
- **Memory‑efficient direct pixel access**

## Samples

Available in `samples/`:

- `grid_sample_simple.txt`
- `grid_sample_temperature.csv`
- `xyz_sample.txt`
- `xyz_sample2.txt`

See `samples/README.md` for details.

## Tests

```bash
cd tests/CodeD.CSV2Heatmap.Tests
dotnet test
```

Includes unit tests for:

- GridCsvParser  
- XyzCsvParser  
- HeatmapRenderer  
- File-format parsing

## License

Released under the MIT License. See [LICENSE](LICENSE).

## Contributing

Pull requests are welcome. For major changes, open an issue first.

## Author

**dck-jp**

## Related Links

- SkiaSharp — cross‑platform 2D graphics library  
- .NET Standard — common API set

## Changelog

### Version 1.0.0

- Initial release  
- GridCSV support  
- XYZ support  
- Multiple color modes  
- SIMD‑accelerated rendering  
- Basic image‑processing tools  

---

⭐ If you find this project useful, please consider giving it a star!

