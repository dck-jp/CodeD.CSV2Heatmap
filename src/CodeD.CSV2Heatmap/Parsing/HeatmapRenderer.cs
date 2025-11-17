using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace CodeD
{
    /// <summary>
    /// HeatmapRenderer class
    /// (Two-dimensional array data visualization and simple image processing class)
    /// </summary>
    public class HeatmapRenderer
    {
        private static SKColor[] color;

        // Color map cache to avoid repeated allocation
        private static SKColor[] _rainbowCache;
        private static SKColor[] _monochromeCache;
        private static SKColor[] _blackPurpleWhiteCache;

        // SIMD vector size (pre-calculated for performance)
        private static readonly int VectorSize = Vector<double>.Count;

        public enum ColorMode { Monochorome, Rainbow, BlackPurpleWhite };

        public enum ConvertMode { None, ln, log };

        public SKColor OutOfRangeColor { get; set; }
        public bool EnablesOutOfRangeColor { get; set; }

        public int XSize { get; private set; }
        public int YSize { get; private set; }

        public double PixelSize { get; private set; }
        public double[,] Data { get; private set; }

        public string Header { get; private set; }
        public double Max { get; private set; }
        public double Min { get; private set; }

        #region Color Index Buffer
        private int[] _colorIndices = Array.Empty<int>();

        private void EnsureColorIndicesBuffer(int width, int height)
        {
            int needed = width * height;
            if (_colorIndices.Length < needed)
            {
                // サイズが変わったときだけ確保（定常状態では new しない）
                _colorIndices = new int[needed];
            }
        }
        #endregion

        public HeatmapRenderer(double[,] data, double pixelSize = 0, string header = "")
        {
            Data = data;
            XSize = data.GetLength(0);
            YSize = data.GetLength(1);
            PixelSize = pixelSize;

            Header = header;
            EnablesOutOfRangeColor = false;
        }

        private HeatmapRenderer()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetDataRange(out double minValue, out double maxValue)
        {
            minValue = double.MaxValue;
            maxValue = double.MinValue;
            var data = Data;
            int width = XSize;
            int height = YSize;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    double value = data[x, y];
                    if (value < minValue) minValue = value;
                    if (value > maxValue) maxValue = value;
                }
            }
        }

        public static async Task<HeatmapRenderer> CreateAsync(string filename, double pixelSize = 0)
        {
            var renderer = new HeatmapRenderer();
            await renderer.InitializeAsync(filename, pixelSize).ConfigureAwait(false);
            return renderer;
        }

        private async Task InitializeAsync(string filename, double pixelSize)
        {
            var parser = await GridCsvParser.CreateAsync(filename).ConfigureAwait(false);
            Header = parser.Header;
            Data = parser.Data;
            XSize = parser.XSize;
            YSize = parser.YSize;
            Max = parser.Max;
            Min = parser.Min;
            PixelSize = pixelSize;
            EnablesOutOfRangeColor = false;
        }

        /// <summary>
        /// Convert to bitmap
        /// </summary>
        /// <param name="min">Minimum value for color mapping (null for auto)</param>
        /// <param name="max">Maximum value for color mapping (null for auto)</param>
        /// <param name="colorMode">Color mode for the heatmap</param>
        /// <param name="convertMode">Data conversion mode (None, ln, log)</param>
        /// <returns>Generated bitmap image</returns>
        public SKBitmap ToBitmap(double? min = null, double? max = null, ColorMode colorMode = ColorMode.Rainbow, ConvertMode convertMode = ConvertMode.None)
        {
            CreateColorMap(colorMode);

            double minVal;
            double maxVal;
            if (min.HasValue && max.HasValue)
            {
                minVal = min.Value;
                maxVal = max.Value;
            }
            else
            {
                GetDataRange(out minVal, out maxVal);
                if (min.HasValue) minVal = min.Value;
                if (max.HasValue) maxVal = max.Value;
            }

            try
            {
                if (EnablesOutOfRangeColor) return CreateBitmapWithOutOfRangeColor(minVal, maxVal, convertMode);

                var bitmap = new SKBitmap(XSize, YSize, SKColorType.Rgba8888, SKAlphaType.Opaque);

                // Parallel processing version: Separate calculation and SetPixel for parallelization (only when data size is large enough)
                int totalPixels = XSize * YSize;
                if (Vector.IsHardwareAccelerated && totalPixels >= 256)
                {
                    ProcessBitmapSIMD(bitmap, minVal, maxVal, convertMode);
                }
                else
                {
                    // Fallback: Traditional processing without delegate overhead
                    double range = (maxVal - minVal);
                    double invDenLog = range > 0 ? 1.0 / Math.Log(range) : 0.0;
                    double invDenLog10 = range > 0 ? 1.0 / Math.Log10(range) : 0.0;
                    double scale = range != 0.0 ? 764.0 / range : 0.0;
                    for (int x = 0; x < XSize; x++)
                    {
                        for (int y = 0; y < YSize; y++)
                        {
                            double v = Data[x, y];
                            int number;
                            switch (convertMode)
                            {
                                case ConvertMode.None:
                                    number = (int)((v - minVal) * scale);
                                    break;
                                case ConvertMode.log:
                                    number = (int)(Math.Log(v - minVal + double.Epsilon) * invDenLog * 764.0);
                                    break;
                                case ConvertMode.ln:
                                    number = (int)(Math.Log10(v - minVal + double.Epsilon) * invDenLog10 * 764.0);
                                    break;
                                default:
                                    number = 0;
                                    break;
                            }
                            if (number > 764) number = 764; else if (number < 0) number = 0;
                            bitmap.SetPixel(x, y, color[number]);
                        }
                    }
                }

                return bitmap;
            }
            catch (Exception e)
            {
                throw new ApplicationException("Cannot create Bitmap" + Environment.NewLine + e.Source + e.StackTrace);
            }
        }

        private void CreateColorMap(ColorMode colorMode)
        {
            switch (colorMode)
            {
                case ColorMode.Rainbow:
                    if (_rainbowCache == null)
                    {
                        _rainbowCache = new SKColor[765];
                        SetRainbowColors(ref _rainbowCache);
                    }
                    color = _rainbowCache;
                    break;

                case ColorMode.Monochorome:
                    if (_monochromeCache == null)
                    {
                        _monochromeCache = new SKColor[765];
                        SetMonochromeColors(ref _monochromeCache);
                    }
                    color = _monochromeCache;
                    break;

                case ColorMode.BlackPurpleWhite:
                    if (_blackPurpleWhiteCache == null)
                    {
                        _blackPurpleWhiteCache = new SKColor[765];
                        SetBlackPurpleWhiteColors(ref _blackPurpleWhiteCache);
                    }
                    color = _blackPurpleWhiteCache;
                    break;

                default:
                    throw new ApplicationException("Illegal ColorMode");
            }
            ;
        }

        private SKBitmap CreateBitmapWithOutOfRangeColor(double min, double max, ConvertMode convertMode)
        {
            var bitmap = new SKBitmap(XSize, YSize, SKColorType.Rgba8888, SKAlphaType.Opaque);

            double range = (max - min);
            double invDenLog = range > 0 ? 1.0 / Math.Log(range) : 0.0;
            double invDenLog10 = range > 0 ? 1.0 / Math.Log10(range) : 0.0;
            double scale = range != 0.0 ? 764.0 / range : 0.0;
            for (int x = 0; x < XSize; x++)
            {
                for (int y = 0; y < YSize; y++)
                {
                    double value = Data[x, y];
                    SKColor _color;
                    if (value > max || value < min)
                    {
                        _color = OutOfRangeColor;
                    }
                    else
                    {
                        int number;
                        switch (convertMode)
                        {
                            case ConvertMode.None:
                                number = (int)((value - min) * scale);
                                break;
                            case ConvertMode.log:
                                number = (int)(Math.Log(value - min + double.Epsilon) * invDenLog * 764.0);
                                break;
                            case ConvertMode.ln:
                                number = (int)(Math.Log10(value - min + double.Epsilon) * invDenLog10 * 764.0);
                                break;
                            default:
                                number = 0;
                                break;
                        }
                        if (number > 764) number = 764;
                        else if (number < 0) number = 0;

                        _color = color[number];
                    }

                    bitmap.SetPixel(x, y, _color);
                }
            }

            return bitmap;
        }

        /// <summary>
        /// High-speed bitmap processing using parallel processing
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessBitmapSIMD(SKBitmap bitmap, double min, double max, ConvertMode convertMode)
        {
            int width = XSize;
            int height = YSize;
            EnsureColorIndicesBuffer(width, height); // new only when size changes
            var colorIndices = _colorIndices;
            double range = (max - min);
            double scale = range != 0.0 ? 764.0 / range : 0.0;
            double invDenLog = range > 0 ? 1.0 / Math.Log(range) : 0.0;
            double invDenLog10 = range > 0 ? 1.0 / Math.Log10(range) : 0.0;

            Parallel.For(0, width, x =>
            {
                int baseIndex = x * height;
                int y = 0;

                // SIMD processing: Process multiple elements simultaneously using Vector<double>
                if (VectorSize > 1 && convertMode == ConvertMode.None)
                {
                    var maxIdxVec = new Vector<double>(764.0);
                    var zeroVec = Vector<double>.Zero;
                    var scaleVec = new Vector<double>(scale);
                    var minVec = new Vector<double>(min);

                    //for zero allocation in .net standard2.0
                    unsafe
                    {
                        fixed (double* ptr = &Data[x, 0])
                        {
                            Span<double> values = stackalloc double[8];
                            int vectorSize = VectorSize > values.Length ? values.Length : VectorSize;

                            for (; y <= height - vectorSize; y += vectorSize)
                            {
                                var vec = Unsafe.Read<Vector<double>>(ptr + y);
                                // Normalize: ((v - min) * scale)
                                vec = (vec - minVec) * scaleVec;
                                // Clamp to [0, 764]
                                vec = Vector.Min(Vector.Max(vec, zeroVec), maxIdxVec);
                                for (int v = 0; v < vectorSize; v++)
                                    colorIndices[baseIndex + y + v] = (int)vec[v];
                            }
                        }
                    }
                }

                // Process remaining elements
                for (; y < height; y++)
                {
                    double v = Data[x, y];
                    int number;
                    switch (convertMode)
                    {
                        case ConvertMode.None:
                            number = (int)((v - min) * scale);
                            break;
                        case ConvertMode.log:
                            number = (int)(Math.Log(v - min + double.Epsilon) * invDenLog * 764.0);
                            break;
                        case ConvertMode.ln:
                            number = (int)(Math.Log10(v - min + double.Epsilon) * invDenLog10 * 764.0);
                            break;
                        default:
                            number = 0;
                            break;
                    }
                    if (number > 764) number = 764;
                    else if (number < 0) number = 0;
                    colorIndices[baseIndex + y] = number;
                }
            });

            // Direct access to pixel data for parallelization
            unsafe
            {
                var pixelPtr = (uint*)bitmap.GetPixels().ToPointer();
                int stride = bitmap.RowBytes / 4; // 4 bytes per pixel (RGBA8888)

                Parallel.For(0, width, x =>
                {
                    int baseIndex = x * height;

                    for (int y = 0; y < height; y++)
                    {
                        int idx = baseIndex + y;
                        var c = color[colorIndices[idx]];
                        pixelPtr[y * stride + x] =
                            (uint)((c.Alpha << 24) | (c.Blue << 16) | (c.Green << 8) | c.Red);
                    }
                });
            }
        }

        /// <summary>
        /// Create black → purple → white color scheme
        /// </summary>
        /// <param name="color"></param>
        private void SetBlackPurpleWhiteColors(ref SKColor[] color)
        {
            int visibility;
            for (int i = 0; i < 510; i++)
            {
                visibility = (i + 1) / 2;
                color[i] = new SKColor((byte)(visibility / 2), 0, (byte)visibility);
            }
            for (int i = 510; i < 765; i++)
            {
                visibility = i - 510;
                color[i] = new SKColor(255, (byte)visibility, 255);
            }
        }

        /// <summary>
        /// Create monochrome color scheme
        /// </summary>
        /// <param name="color"></param>
        private void SetMonochromeColors(ref SKColor[] color)
        {
            int visibility;
            for (int i = 0; i < 765; i++)
            {
                visibility = (i + 1) / 3;
                color[i] = new SKColor((byte)visibility, (byte)visibility, (byte)visibility);
            }
        }

        /// <summary>
        /// Create AV pseudo-like color scheme
        /// </summary>
        /// <param name="color"></param>
        private void SetRainbowColors(ref SKColor[] color)
        {
            for (int i = 0; i < 255; i++)
            {
                color[i] = new SKColor(0, (byte)i, 255);
            }
            for (int i = 255; i < 510; i++)
            {
                color[i] = new SKColor((byte)(i - 255), 255, (byte)(510 - i));
            }
            for (int i = 510; i < 765; i++)
            {
                color[i] = new SKColor(255, (byte)(765 - i), 0);
            }
        }

        /// <summary>
        /// Save to file
        /// </summary>
        /// <param name="filename"></param>
        public void SaveAs(string filename)
        {
            StringBuilder st = new StringBuilder();

            for (int j = 0; j < YSize; j++)
            {
                for (int i = 0; i < XSize; i++)
                {
                    st.Append(Data[i, j]).Append("\t");
                }
                st.AppendLine();
            }

            File.WriteAllText(filename, st.ToString());
        }

        /// <summary>
        /// Return the data for the specified column as an array
        /// </summary>
        /// <param name="columnNumber">Column index to retrieve</param>
        /// <returns>Array of column data</returns>
        public double[] GetColumnData(int columnNumber)
        {
            double[] columnData = new double[YSize];
            for (int j = 0; j < YSize; j++)
            {
                columnData[j] = Data[columnNumber, j];
            }

            return columnData;
        }

        /// <summary>
        /// Return the data for the specified row as an array
        /// </summary>
        /// <param name="rowNumber">Row index to retrieve</param>
        /// <returns>Array of row data</returns>
        public double[] GetRowData(int rowNumber)
        {
            double[] rowData = new double[XSize];
            for (int i = 0; i < XSize; i++)
            {
                rowData[i] = Data[i, rowNumber];
            }

            return rowData;
        }

        #region Image Processing

        /// <summary>
        /// Get plane-corrected surface (requires PixelPitch setting)
        /// ApplicationException: When PixelPitch is not set
        /// </summary>
        /// <returns></returns>
        public HeatmapRenderer GetPlaneCorrection()
        {
            if (PixelSize == 0) throw new ApplicationException("Set pixel size");

            double sa = 0, sb = 0, sc = 0, sd = 0, se = 0;
            double sf = 0, sg = 0, sh = 0, si = 0;
            for (double i = 0; i < XSize; i++)
            {
                for (double j = 0; j < YSize; j++)
                {
                    sa += i * j * PixelSize * PixelSize;
                    sb += i * j * PixelSize * PixelSize;
                    sc += i * PixelSize;
                    sd += j * j * PixelSize * PixelSize;
                    se += j * PixelSize;
                    sf += 1;
                    sg += i * PixelSize * Data[(int)i, (int)j];
                    sh += j * Data[(int)i, (int)j];
                    si += Data[(int)i, (int)j];
                }
            }

            double d11, d12, d13, d21, d22, d23, d31, d32, d33;
            double det;
            double a, b, c;
            d11 = sd * sf - se * se;
            d12 = se * sc - sb * sf;
            d13 = sb * se - sc * sd;
            d21 = sc * se - sb * sf;
            d22 = sa * sf - sc * sc;
            d23 = sb * sc - sa * se;
            d31 = sb * se - sc * sd;
            d32 = sb * sc - sa * se;
            d33 = sa * sd - sb * sb;
            det = sa * d11 + sb * d12 + sc * d13;
            a = (d11 * sg + d12 * sh + d13 * si) / det;
            b = (d21 * sg + d22 * sh + d23 * si) / det;
            c = (d31 * sg + d32 * sh + d33 * si) / det;

            double[,] corrected = new double[XSize, YSize];
            for (int i = 0; i < XSize; i++)
            {
                for (int j = 0; j < YSize; j++)
                {
                    corrected[i, j] = Data[i, j] - (a * (double)i * PixelSize + b * (double)j * PixelSize + c);
                }
            }

            return new HeatmapRenderer(corrected, PixelSize);
        }

        /// <summary>
        /// Get trimmed surface
        /// </summary>
        /// <param name="x0">Starting X coordinate</param>
        /// <param name="y0">Starting Y coordinate</param>
        /// <param name="xSizeNew">Width of trimmed area</param>
        /// <param name="ySizeNew">Height of trimmed area</param>
        /// <returns>Trimmed HeatmapRenderer instance</returns>
        public HeatmapRenderer GetTrim(int x0, int y0, int xSizeNew, int ySizeNew)
        {
            double[,] trimming = new double[xSizeNew, ySizeNew];
            for (int i = 0; i < xSizeNew; i++)
            {
                for (int j = 0; j < ySizeNew; j++)
                {
                    trimming[i, j] = Data[x0 + i, y0 + j];
                }
            }

            return new HeatmapRenderer(trimming, PixelSize);
        }

        /// <summary>
        /// Get surface rotated 90 degrees counterclockwise
        /// </summary>
        /// <returns></returns>
        public HeatmapRenderer GetRotateCCW()
        {
            double[,] rotated = new double[YSize, XSize];
            for (int i = 0; i < XSize; i++)
            {
                for (int j = 0; j < YSize; j++)
                {
                    rotated[j, i] = Data[XSize - 1 - i, j];
                }
            }

            return new HeatmapRenderer(rotated, PixelSize);
        }

        /// <summary>
        /// Get surface rotated 90 degrees clockwise
        /// </summary>
        /// <returns></returns>
        public HeatmapRenderer GetRotateCW()
        {
            double[,] rotated = new double[YSize, XSize];
            for (int i = 0; i < XSize; i++)
            {
                for (int j = 0; j < YSize; j++)
                {
                    rotated[j, i] = Data[i, YSize - 1 - j];
                }
            }

            return new HeatmapRenderer(rotated, PixelSize);
        }

        /// <summary>
        /// Get surface rotated counterclockwise by arbitrary angle
        /// </summary>
        /// <param name="rad"></param>
        /// <returns></returns>
        public HeatmapRenderer GetRotate(double rad)
        {
            double cos, sin;
            int int_sin, int_cos; //Guard against catastrophic cancellation
            int dw, dh;
            int sCX, sCY, dCX, dCY;

            cos = Math.Cos(rad);
            sin = Math.Sin(rad);

            int_cos = (int)(Math.Cos(rad) * 1024);
            int_sin = (int)(Math.Sin(rad) * 1024);

            // +0.5 for rounding up decimals
            dw = (int)(Math.Abs(XSize * cos) + Math.Abs(XSize * sin) + 0.5);
            dh = (int)(Math.Abs(YSize * cos) + Math.Abs(YSize * sin) + 0.5);

            sCX = XSize / 2;
            sCY = YSize / 2;
            dCX = dw / 2;
            dCY = dh / 2;

            double[,] newZMap = new double[dw, dh];
            int x1, y1;
            for (int y2 = 0; y2 < dh; y2++)
            {
                for (int x2 = 0; x2 < dw; x2++)
                {
                    x1 = (((x2 - dCX) * int_cos - (y2 - dCY) * int_sin) >> 10) + sCX;
                    y1 = (((x2 - dCX) * int_sin + (y2 - dCY) * int_cos) >> 10) + sCY;

                    if (x1 >= 0 && x1 < XSize && y1 >= 0 && y1 < YSize)
                    {
                        newZMap[x2, y2] = Data[x1, y1];
                    }
                }
            }

            return new HeatmapRenderer(newZMap);
        }

        #endregion Simple image processing
    }
}
