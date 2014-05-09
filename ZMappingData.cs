using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Drawing;
using G_PROJECT;

namespace CodeD.Data
{
    /// <summary>
    /// ZMappingDataクラス
    /// (弐次元配列データ可視化・簡易画像処理クラス)
    /// </summary>

    public class ZMappingData
    {
        double[,] data;
        string[] header; //ヘッダ情報(データ部分以外全て)
        int xSize, ySize;
        double pixelSize;

        static Color[] color;
        public enum ColorMode { Monochorome, Rainbow, BlackPurpleWhite};
        public enum ConvertMode { None, ln, log };

        public Color OutOfRangeColor { get; set; }
        public bool EnablesOutOfRangeColor { get; set; }

        public int XSize
        {
            get { return xSize; }
        }
        public int YSize
        {
            get { return ySize; }
        }
        public double[,] Data
        {
            get { return data; }
        }
        public string Header
        {
            get 
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < header.Length; i++)
                {
                    sb.AppendLine(header[i]);
                }

                return sb.ToString(); 
            }
        }

        public double Max
        {
            get
            {
                double min = Double.MinValue;
                foreach(double i in data)
                {
                    if (i > min) min = i;
                }
                return min;
            }
        }
        public double Min
        {
            get
            {
                double max = Double.MaxValue;
                foreach (double i in data)
                {
                    if (i < max) max = i;
                }
                return max;
            }
        }

        #region コンストラクタ
        public ZMappingData(double[,] data)
        {
            this.data = data;
            xSize = data.GetLength(0);
            ySize = data.GetLength(1);
            pixelSize = 0;
            EnablesOutOfRangeColor = false;
        }
        public ZMappingData(double[,] data, double pixelSize)
        {
            this.data = data;
            xSize = data.GetLength(0);
            ySize = data.GetLength(1);
            this.pixelSize = pixelSize;
            EnablesOutOfRangeColor = false;
        }
        public ZMappingData(string filename)
        {
            ParseZMapFile(filename);
            pixelSize = 0;
            EnablesOutOfRangeColor = false;
        }
        public ZMappingData(string filename, double pixelSize)
        {
            ParseZMapFile(filename);
            this.pixelSize = pixelSize;
            EnablesOutOfRangeColor = false;
        }

        /// <summary>
        /// ZMapファイルをパースしてdouble型二次元配列をメンバ変数にセット
        /// ※１　区切り文字は、タブ、コンマ、半角空白
        /// ※２　データ点数が足りない場合、不正な値の場合、NaNを代入
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="p"></param>
        private void ParseZMapFile(string filename)
        {
            TxtEnc enc = new TxtEnc();
            string[] data = File.ReadAllLines(filename, enc.SetFromTextFile(filename));

            data = SplitHeader(data);

            if (data.Length == 0)
            {
                data = null;
                return;
            }  // __________________________ データなし ___________________

            char[] spliter = new char[1];

            if (data[0].Contains("\t")) //区切り文字 \t
            {
                spliter[0] = '\t';
            }
            else if (data[0].Contains(",")) //区切り文字 ,
            {
                spliter[0] = ',';
            }
            else //区切り文字 \s
            {
                spliter[0] = ' ';
            }

            xSize = data[0].Trim().Split(spliter, StringSplitOptions.RemoveEmptyEntries).Length;
            ySize = data.Length;
            this.data = new double[xSize, ySize];


            int threadNum = Environment.ProcessorCount;
            int[] range = new int[1 + threadNum];
            range[0] = 0;
            range[range.Length - 1] = ySize;
            for (int i = 1; i < threadNum; i++) { range[i] = ySize * i / threadNum; }

            Action<int,int> parser = (startLineNo, endLineNo) =>
                {
                    double parsed;
                    for (int j = startLineNo; j < endLineNo; j++)
                    {
                        string[] line = data[j].Trim().Split(spliter, StringSplitOptions.RemoveEmptyEntries);

                        for (int i = 0; i < xSize; i++)
                        {
                            if (!(i < line.Length)) this.data[i, j] = double.NaN; //データ点数が足りない場合
                            else if (Double.TryParse(line[i], out parsed) && parsed != double.NaN) this.data[i, j] = parsed;
                            else this.data[i, j] = double.NaN; //parse不可能な場合
                        }
                    }
                };

            IAsyncResult[] iar = new IAsyncResult[threadNum];
            for (int i = 0; i < threadNum; i++) { iar[i] = parser.BeginInvoke(range[i], range[i+1], null, null); }
            for (int i = 0; i < threadNum; i++) { parser.EndInvoke(iar[i]); }

        }

        /// <summary>
        /// ファイルの先頭部分において
        /// 数値以外のものが含まれている場合
        /// 数値のみのラインが現れるまでスキップし、
        /// 当該ライン以降を返す
        /// </summary>
        /// <param name="data"></param>
        private string[] SplitHeader(string[] data)
        {
            List<string> bodyData = new List<string>();
            List<string> headerData = new List<string>();

            char[] splitter = new char[] { '\t', ',', ' ' };
            string[] tempLine;
            int bodyDataStart = 0;
            //Headerデータの格納＋Bodyデータのスタート位置のサーチ
            for(int i=0; i != data.Length; i++)
            {
                tempLine = data[i].Split(splitter, StringSplitOptions.RemoveEmptyEntries);

                if (ContainsString(tempLine))
                {
                    headerData.Add(data[i]);
                }
                else
                {
                    bodyDataStart = i;
                    break;
                }

                if (bodyDataStart == 0 && i == data.Length - 1) bodyDataStart = data.Length - 1;
            }


            //Bodyデータ格納
            if (bodyDataStart != data.Length - 1)
            {
                for (int i = bodyDataStart; i != data.Length; i++)
                {
                    bodyData.Add(data[i]);
                }
            }
  
            header = headerData.ToArray();
            return bodyData.ToArray();
        }

        /// <summary>
        /// 要素内に数値以外が含まれているかチェック
        /// </summary>
        /// <param name="tempLine"></param>
        /// <returns></returns>
        private bool ContainsString(string[] tempLine)
        {
            bool containsString = false;
            double trash;

            if (tempLine.Length == 0) containsString = true;
            for (int i = 0; i != tempLine.Length; i++)
            {
                if (!double.TryParse(tempLine[i], out trash))
                {
                    containsString = true;
                    break; //数値じゃなかったら終了
                }
            }

            return containsString;
        }
        #endregion

        #region 可視化
        /// <summary>
        /// Bitmapに変換
        /// </summary>
        /// <returns></returns>
        public Bitmap ToBitmap()
        {
            return ToBitmap(null, null, ColorMode.Rainbow);
        }

        /// <summary>
        /// Bitmapに変換(最小値・最大値指定)
        /// </summary>
        /// <returns></returns>
        public Bitmap ToBitmap(double? min , double? max)
        {
            return ToBitmap(min, max, ColorMode.Rainbow);
        }

        /// <summary>
        /// Bitmapに変換(カラーモード指定)
        /// </summary>
        /// <param name="colorMode"></param>
        /// <returns></returns>
        public Bitmap ToBitmap(ColorMode colorMode)
        {
            return ToBitmap(null, null, colorMode);
        }

        /// <summary>
        /// 最小値・最大値, ColorModeを指定してBitmapに変換<br>
        /// ApplicationException: 不正なColorMode指定時、bitmap作成失敗時
        /// </summary>
        /// <param name="min">最小値</param>
        /// <param name="max">最大値</param>
        /// <returns></returns>
        public Bitmap ToBitmap(double? min, double? max, ColorMode colorMode)
        {
            return ToBitmap(min, max, colorMode, ConvertMode.None);
        }
        public Bitmap ToBitmap(double? min, double? max, ColorMode colorMode, ConvertMode convertMode)
        {
            CreateColorMap(colorMode);
            SetMinMaxValue(ref min, ref max);

            try
            {
                if (EnablesOutOfRangeColor) return CreateBitmapWithOutOfRangeColor(min,max,convertMode);

                Bitmap bitmap = new Bitmap(xSize, ySize, PixelFormat.Format24bppRgb);
                Rectangle rectangle = new Rectangle(0, 0, xSize , ySize);
                BitmapData bitmapData = bitmap.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                IntPtr adr = bitmapData.Scan0;
                int stride = bitmapData.Stride;
                int pos,number;

                Func<int, int, int> calcColor = CreateFormula(min, max, convertMode);

                for (int x = 0; x < xSize; x++)
                {
                    for (int y = 0; y < ySize; y++)
                    {
                        pos = x * 3 + stride * y;
                        number = calcColor(x, y);
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
                throw new ApplicationException("Bitmapの作成に失敗しました"　+ Environment.NewLine + e.Source + e.StackTrace);
            }
        }

        private void SetMinMaxValue(ref double? min, ref double? max)
        {
            if (max == null)
            {
                max = double.MinValue;
                foreach (double element in data) { if (max < element) { max = element; } }
            }

            if (min == null)
            {
                min = double.MaxValue;
                foreach (double element in data) { if (min > element) { min = element; } }
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
                    throw new ApplicationException("不正なColorModeが指定されました。");
            };

        }

        private Bitmap CreateBitmapWithOutOfRangeColor(double? min, double? max, ConvertMode convertMode)
        {
            Bitmap bitmap = new Bitmap(xSize, ySize, PixelFormat.Format24bppRgb);
            Rectangle rectangle = new Rectangle(0, 0, xSize, ySize);
            BitmapData bitmapData = bitmap.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            IntPtr adr = bitmapData.Scan0;
            int stride = bitmapData.Stride;
            int pos, number;

            Func<int, int, int> calcColor = CreateFormula(min, max, convertMode);

            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    pos = x * 3 + stride * y;
                    Color _color;
                    if (data[x, y] > max || data[x, y] < min)
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
                    calcColor = (x, y) => { return (int)((data[x, y] - min) / (max - min) * 764); };
                    break;
                case ConvertMode.log:
                    calcColor = (x, y) => { return (int)(Math.Log(data[x, y] - (double)min + 1) / Math.Log((double)(max - min)) * 764); };
                    break;
                case ConvertMode.ln:
                    calcColor = (x, y) => { return (int)(Math.Log10(data[x, y] - (double)min + 1) / Math.Log10((double)(max - min)) * 764); };
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
        #endregion

        /// <summary>
        /// ファイルに保存
        /// </summary>
        /// <param name="filename"></param>
        public void SaveAs(string filename)
        {
            StringBuilder st = new StringBuilder();

            for (int j = 0; j < ySize; j++)
            {
                for (int i = 0; i < xSize; i++)
                {
                    st.Append(data[i, j]).Append("\t");
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
                columnData[j] = data[columnNumber, j];
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
                rowData[i] = data[i, rowNumber];
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
            if(pixelSize == 0) throw new ApplicationException("ピクセル長が指定されていません。");

            double sa = 0, sb = 0, sc = 0, sd = 0, se = 0;
            double sf = 0, sg = 0, sh = 0, si = 0;
            for (double i = 0; i < xSize; i++)
            {
                for (double j = 0; j < ySize; j++)
                {
                    sa += i * j * pixelSize * pixelSize;
                    sb += i * j * pixelSize * pixelSize;
                    sc += i * pixelSize;
                    sd += j * j * pixelSize * pixelSize;
                    se += j * pixelSize;
                    sf += 1;
                    sg += i * pixelSize * data[(int)i, (int)j];
                    sh += j * data[(int)i, (int)j];
                    si += data[(int)i, (int)j];
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

            double[,] corrected = new double[xSize, ySize];
            for (int i = 0; i < xSize; i++)
            {
                for (int j = 0; j < ySize; j++)
                {
                    corrected[i, j] = data[i, j] - (a * (double)i * pixelSize + b * (double)j * pixelSize + c);
                }
            }

            return new ZMappingData(corrected, pixelSize);

        }

        /// <summary>
        /// トリミングした面を取得
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="xSize"></param>
        /// <param name="ySize"></param>
        /// <returns></returns>
        public ZMappingData GetTrim(int x0, int y0, int xSizeNew, int ySizeNew)
        {
            double[,] trimming = new double[xSizeNew, ySizeNew];
            for (int i = 0; i < xSizeNew; i++)
            {
                for (int j = 0; j < ySizeNew; j++)
                {
                    trimming[i, j] = data[x0 + i, y0 + j];
                }
            }

            return new ZMappingData(trimming, pixelSize);
        }

        /// <summary>
        /// 反時計回りに90度回転した面を取得
        /// </summary>
        /// <returns></returns>
        public ZMappingData GetRotateCCW()
        {
            double[,] rotated = new double[ySize, xSize];
            for (int i = 0; i < xSize; i++)
            {
                for (int j = 0; j < ySize; j++)
                {
                    rotated[j, i] = data[xSize - 1 - i,j];
                }
            }

            return new ZMappingData(rotated, pixelSize);
        }

        /// <summary>
        /// 時計回りに90度回転した面を取得
        /// </summary>
        /// <returns></returns>
        public ZMappingData GetRotateCW()
        {
            double[,] rotated = new double[ySize, xSize];
            for (int i = 0; i < xSize; i++)
            {
                for (int j = 0; j < ySize; j++)
                {
                    rotated[j, i] = data[i, ySize - 1 - j];
                }
            }

            return new ZMappingData(rotated, pixelSize);
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
            dw = (int)(Math.Abs(xSize * cos) + Math.Abs(xSize * sin) + 0.5);
            dh = (int)(Math.Abs(ySize * cos) + Math.Abs(ySize * sin) + 0.5);

            sCX = xSize / 2;
            sCY = ySize / 2;
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

                    if (x1 >= 0 && x1 < xSize && y1 >= 0 && y1 < ySize)
                    {
                        newZMap[x2, y2] = data[x1, y1];
                    }
                }
            }

            return new ZMappingData(newZMap);
        }
        #endregion
    }
}
