using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Drawing;
using G_PROJECT;
using System.Linq;

namespace CodeD.Data
{
    /// <summary>
    /// ZMappingDataクラス
    /// (弐次元配列データ可視化・簡易画像処理クラス)
    /// </summary>

    public class ZMappingData
    {
        static int majourVersion = 2;
        static int minorVersion = 0;
        static int revisionVersion = 0;
        public static string VersionInfo { get { return majourVersion.ToString() + "." + minorVersion.ToString() + "." + revisionVersion; } } 

        static Color[] color;
        public enum ColorMode { Monochorome, Rainbow, BlackPurpleWhite};
        public enum ConvertMode { None, ln, log };

        public Color OutOfRangeColor { get; set; }
        public bool EnablesOutOfRangeColor { get; set; }

        public int XSize { get; private set; }
        public int YSize { get; private set; }

        public double PixelSize { get; private set; }
        public double[,] Data { get; private set; }
        public string Header{get; private set;}
        public double Max{get; private set;}
        public double Min {get; private set; }

        public ZMappingData(double[,] data, double pixelSize = 0, string header = "")
        {
            Data = data;
            XSize = data.GetLength(0);
            YSize = data.GetLength(1);
            PixelSize = pixelSize;

            Header = header;
            EnablesOutOfRangeColor = false;
        }
        public ZMappingData(string filename, double pixelSize = 0)
        {
            var parser = new ZMapParser(filename);
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
        /// bitmapに変換
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="colorMode"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public Bitmap ToBitmap(double? min = null, double? max = null, ColorMode colorMode = ColorMode.Rainbow, ConvertMode convertMode = ConvertMode.None)
        {
            CreateColorMap(colorMode);
            if (min == null ){ min = Data.Cast<double>().Min(); }
            if (max == null ){ max = Data.Cast<double>().Max(); }

            try
            {
                if (EnablesOutOfRangeColor) return CreateBitmapWithOutOfRangeColor(min,max,convertMode);

                Bitmap bitmap = new Bitmap(XSize, YSize, PixelFormat.Format24bppRgb);
                Rectangle rectangle = new Rectangle(0, 0, XSize , YSize);
                BitmapData bitmapData = bitmap.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                IntPtr adr = bitmapData.Scan0;
                int stride = bitmapData.Stride;
                int pos,number;

                Func<int, int, int> colorFunc = CreateFormula(min, max, convertMode);
                for (int x = 0; x < XSize; x++)
                {
                    for (int y = 0; y < YSize; y++)
                    {
                        pos = x * 3 + stride * y;
                        number = colorFunc(x, y);
                        if (number > 764) { number = 764; }
                        else if (number < 0) { number = 0; }
                        var _color = color[number];
                        Marshal.WriteByte(adr, pos, _color.B);
                        Marshal.WriteByte(adr, pos + 1, _color.G);
                        Marshal.WriteByte(adr, pos + 2, _color.R);
                    }
                }

                bitmap.UnlockBits(bitmapData);
                return bitmap;
            }
            catch(Exception e)
            {
                throw new ApplicationException("Cannot create Bitmap"　+ Environment.NewLine + e.Source + e.StackTrace);
            }
        }

        private void CreateColorMap(ColorMode colorMode)
        {
            color = new Color[765];
            switch (colorMode)
            {
                case ColorMode.Rainbow:
                    SetRainbowColors(ref color);
                    break;
                case ColorMode.Monochorome:
                    SetMonochromeColors(ref color);
                    break;
                case ColorMode.BlackPurpleWhite:
                    SetBlackPurpleWhiteColors(ref color);
                    break;
                default:
                    throw new ApplicationException("Illegal ColorMode");
            };

        }

        private Bitmap CreateBitmapWithOutOfRangeColor(double? min, double? max, ConvertMode convertMode)
        {
            Bitmap bitmap = new Bitmap(XSize, YSize, PixelFormat.Format24bppRgb);
            Rectangle rectangle = new Rectangle(0, 0, XSize, YSize);
            BitmapData bitmapData = bitmap.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            IntPtr adr = bitmapData.Scan0;
            int stride = bitmapData.Stride;
            int pos, number;

            Func<int, int, int> calcColor = CreateFormula(min, max, convertMode);
            for (int x = 0; x < XSize; x++)
            {
                for (int y = 0; y < YSize; y++)
                {
                    pos = x * 3 + stride * y;
                    Color _color;
                    if (Data[x, y] > max || Data[x, y] < min)
                    {
                        _color = OutOfRangeColor;
                    }
                    else
                    {
                        number = calcColor(x, y);
                        if (number > 765 || number < 0) number = 0;

                        _color = color[number];
                    }

                    Marshal.WriteByte(adr, pos, _color.B);
                    Marshal.WriteByte(adr, pos + 1, _color.G);
                    Marshal.WriteByte(adr, pos + 2, _color.R);
                }
            }

            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }

        private Func<int, int, int> CreateFormula(double? min, double? max, ConvertMode convertMode)
        {
            Func<int, int, int> calcColor;
            switch (convertMode)
            {
                case ConvertMode.None:
                    calcColor = (x, y) => { return (int)((Data[x, y] - min) / (max - min) * 764); };
                    break;
                case ConvertMode.log:
                    calcColor = (x, y) => { return (int)(Math.Log(Data[x, y] - (double)min + double.Epsilon) / Math.Log((double)(max - min)) * 764); };
                    break;
                case ConvertMode.ln:
                    calcColor = (x, y) => { return (int)(Math.Log10(Data[x, y] - (double)min + double.Epsilon) / Math.Log10((double)(max - min)) * 764); };
                    break;
                default:
                    throw new ArgumentException("");
            }

            return calcColor;
        }

        /// <summary>
        /// 黒→紫→白の配色作成
        /// </summary>
        /// <param name="color"></param>
        private void SetBlackPurpleWhiteColors(ref Color[] color)
        {
            int visibility;
            for (int i = 0; i < 510; i++)
            {
                visibility = (i + 1) / 2;
                color[i] = Color.FromArgb(visibility / 2, 0, visibility);
            }
            for (int i = 510; i < 765; i++)
            {
                visibility = i - 510;
                color[i] = Color.FromArgb(255, visibility, 255);
            }
        }

        /// <summary>
        /// モノクロ配色作成
        /// </summary>
        /// <param name="color"></param>
        private void SetMonochromeColors(ref Color[] color)
        {
            int visibility;
            for (int i = 0; i < 765; i++)
            {
                visibility = (i + 1) / 3;
                color[i] = Color.FromArgb(visibility, visibility, visibility);
            }
        }

        /// <summary>
        /// AV似非似の配色作成
        /// </summary>
        /// <param name="color"></param>
        private void SetRainbowColors(ref Color[] color)
        {
            for (int i = 0; i < 255; i++)
            {
                color[i] = Color.FromArgb(0, i, 255);
            }
            for (int i = 255; i < 510; i++)
            {
                color[i] = Color.FromArgb(i - 255, 255, 510 - i);
            }
            for (int i = 510; i < 765; i++)
            {
                color[i] = Color.FromArgb(255, 765 - i, 0);
            }
        }
        

        /// <summary>
        /// ファイルに保存
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
        /// 指定列のデータを配列で返す
        /// </summary>
        /// <param name="rowNumber"></param>
        /// <returns></returns>
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
        /// 指定行のデータを配列で返す
        /// </summary>
        /// <param name="columnNumber"></param>
        /// <returns></returns>
        public double[] GetRowData(int rowNumber)
        {
            double[] rowData = new double[XSize];
            for (int i = 0; i < XSize; i++)
            {
                rowData[i] = Data[i, rowNumber];
            }

            return rowData;
        }

        #region 簡易画像処理
        /// <summary>
        /// 平面補正した面を取得(要PixelPitchの設定)
        /// ApplicationException: PixelPitch未設定時
        /// </summary>
        /// <returns></returns>
        public ZMappingData GetPlaneCorrection()
        {
            if(PixelSize == 0) throw new ApplicationException("Set pixel size");

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

            return new ZMappingData(corrected, PixelSize);

        }

        /// <summary>
        /// トリミングした面を取得
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="XSize"></param>
        /// <param name="YSize"></param>
        /// <returns></returns>
        public ZMappingData GetTrim(int x0, int y0, int xSizeNew, int ySizeNew)
        {
            double[,] trimming = new double[xSizeNew, ySizeNew];
            for (int i = 0; i < xSizeNew; i++)
            {
                for (int j = 0; j < ySizeNew; j++)
                {
                    trimming[i, j] = Data[x0 + i, y0 + j];
                }
            }

            return new ZMappingData(trimming, PixelSize);
        }

        /// <summary>
        /// 反時計回りに90度回転した面を取得
        /// </summary>
        /// <returns></returns>
        public ZMappingData GetRotateCCW()
        {
            double[,] rotated = new double[YSize, XSize];
            for (int i = 0; i < XSize; i++)
            {
                for (int j = 0; j < YSize; j++)
                {
                    rotated[j, i] = Data[XSize - 1 - i,j];
                }
            }

            return new ZMappingData(rotated, PixelSize);
        }

        /// <summary>
        /// 時計回りに90度回転した面を取得
        /// </summary>
        /// <returns></returns>
        public ZMappingData GetRotateCW()
        {
            double[,] rotated = new double[YSize, XSize];
            for (int i = 0; i < XSize; i++)
            {
                for (int j = 0; j < YSize; j++)
                {
                    rotated[j, i] = Data[i, YSize - 1 - j];
                }
            }

            return new ZMappingData(rotated, PixelSize);
        }

        /// <summary>
        /// 反時計回りに任意角度回転した面を取得
        /// </summary>
        /// <param name="rad"></param>
        /// <returns></returns>
        public ZMappingData GetRotate(double rad)
        {
            double cos, sin;
            int int_sin, int_cos; //桁落ち防止用
            int dw, dh;
            int sCX, sCY, dCX, dCY;

            cos = Math.Cos(rad);
            sin = Math.Sin(rad);

            int_cos = (int)(Math.Cos(rad) * 1024);
            int_sin = (int)(Math.Sin(rad) * 1024);

            // +0.5で小数点切り上げ
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

            return new ZMappingData(newZMap);
        }
        #endregion
    }
}
