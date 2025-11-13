using G_PROJECT;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeD
{
    public class ZMapParser
    {
        internal string Header { get; private set; }
        internal double[,] Data { get; private set; }
        internal int XSize { get; private set; }
        internal int YSize { get; private set; }
        internal double Max { get; private set; }
        internal double Min { get; private set; }

        private ZMapParser()
        {
        }

        public static async Task<ZMapParser> CreateAsync(string filename)
        {
            var parser = new ZMapParser();
            await parser.ParseZMapFileAsync(filename);
            return parser;
        }

        public ZMapParser(string filename)
        {
            ParseZMapFileAsync(filename).GetAwaiter().GetResult();
        }

        /// <summary>
        /// ZMapファイルをパースしてdouble型二次元配列をメンバ変数にセット
        /// ※１　区切り文字は、タブ、コンマ、半角空白
        /// ※２　データ点数が足りない場合、不正な値の場合、NaNを代入
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="p"></param>
        private async Task ParseZMapFileAsync(string filename)
        {
            string[] rawDataLines = CreateRawData(filename);
            rawDataLines = SplitHeader(rawDataLines);

            if (rawDataLines.Length == 0) return;

            var spliter = GetSplitChar(rawDataLines[0]);
            XSize = rawDataLines[0].Trim().Split(spliter, StringSplitOptions.RemoveEmptyEntries).Length;
            YSize = rawDataLines.Length;

            Data = new double[XSize, YSize];
            var threadNum = Environment.ProcessorCount;
            var range = new int[1 + threadNum];
            range[0] = 0;
            range[range.Length - 1] = YSize;
            for (var i = 1; i < threadNum; i++) { range[i] = YSize * i / threadNum; }

            Action<int, int> parser = (startLineNo, endLineNo) =>
            {
                double parsed;
                for (int j = startLineNo; j < endLineNo; j++)
                {
                    string[] line = rawDataLines[j].Trim().Split(spliter, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 0; i < XSize; i++)
                    {
                        if (!(i < line.Length)) Data[i, j] = double.NaN; //データ点数が足りない場合
                        else if (Double.TryParse(line[i], out parsed) && parsed != double.NaN) Data[i, j] = parsed;
                        else Data[i, j] = double.NaN; //parse不可能な場合
                    }
                }
            };

            // 非同期並列処理でパフォーマンスを向上
            var tasks = new Task[threadNum];
            for (int i = 0; i < threadNum; i++) 
            {
                var startRange = range[i];
                var endRange = range[i + 1];
                tasks[i] = Task.Run(() => parser(startRange, endRange));
            }
            await Task.WhenAll(tasks);

            Max = Data.Cast<double>().Max();
            Min = Data.Cast<double>().Min();
        }

        /// <summary>
        /// 末尾の空白行は削除する
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private string[] CreateRawData(string filename)
        {
            var enc = new TxtEnc();
            var encoding = enc.SetFromTextFile(filename);
            if (encoding == null)
            {
                encoding = System.Text.Encoding.UTF8; // デフォルトエンコーディングとしてUTF-8を使用
            }
            var rawDataLines = File.ReadAllLines(filename, encoding);
            if (rawDataLines[rawDataLines.Length - 1] == "")
            {
                Array.Copy(rawDataLines, rawDataLines, rawDataLines.Length - 1);
            }
            return rawDataLines;
        }

        private char[] GetSplitChar(string line1)
        {
            if (line1.Contains("\t")) { return new[] { '\t' }; }
            else if (line1.Contains(",")) { return new[] { ',' }; }
            else { return new[] { ' ' }; }
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
            var bodyDataBeginLine = SearchBodyBeginLine(data);
            if (bodyDataBeginLine == -1) // データが全くない場合
            {
                SetHeader(data.ToList());
                return new string[] { };
            }
            else //データがある場合
            {
                List<string> bodyData = new List<string>();
                //Headerデータ格納
                SetHeader(data.Take(bodyDataBeginLine - 1).ToList());
                //Bodyデータ返却
                return data.Skip(bodyDataBeginLine - 1).ToArray();
            }
        }

        /// <summary>
        /// データ本体のスタート位置を探す
        /// ※　データ全くない場合は -1 を返す
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private int SearchBodyBeginLine(string[] data)
        {
            var i = 0;
            while (ContainsString(data[i]))
            {
                if (i == data.Length - 1) // 最終行まで到達した場合
                {
                    i = -1;
                    break;
                }
                i++;
            }
            return i;
        }

        /// <summary>
        /// 文字列にしてHeaderプロパティにセット
        /// </summary>
        /// <param name="headerData"></param>
        private void SetHeader(List<string> headerData)
        {
            StringBuilder sb = new StringBuilder();
            var maxItemCount = headerData.Count();
            for (int i = 0; i < maxItemCount; i++)
            {
                sb.AppendLine(headerData[i]);
            }

            Header = sb.ToString();
        }

        /// <summary>
        /// データに数値以外(文字列)が含まれているか
        /// </summary>
        /// <param name="v">CSVファイルの１行分のデータ</param>
        /// <returns></returns>
        private bool ContainsString(string v)
        {
            var splitter = new char[] { '\t', ',', ' ' };
            var tempLine = v.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
            return ContainsString(tempLine);
        }

        /// <summary>
        /// 配列の要素内に数値以外が含まれているかチェック
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
    }
}