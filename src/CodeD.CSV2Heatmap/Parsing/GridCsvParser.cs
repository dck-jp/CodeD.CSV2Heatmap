using G_PROJECT;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeD
{
    public class GridCsvParser
    {
        public string Header { get; private set; }
        public double[,] Data { get; private set; }
        public int XSize { get; private set; }
        public int YSize { get; private set; }
        public double Max { get; private set; }
        public double Min { get; private set; }

        private GridCsvParser()
        {
        }

        public static async Task<GridCsvParser> CreateAsync(string filename)
        {
            var parser = new GridCsvParser();
            await parser.ParseZMapFileAsync(filename);
            return parser;
        }

        public GridCsvParser(string filename)
        {
            ParseZMapFileAsync(filename).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Parse Grid data (CSV file) and set a two-dimensional double array to member variables
        /// Note 1: Delimiter characters are tab, comma, and space
        /// Note 2: If there are insufficient data points or invalid values, NaN is assigned
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
                        if (!(i < line.Length)) Data[i, j] = double.NaN; // When data points are insufficient
                        else if (Double.TryParse(line[i], out parsed) && parsed != double.NaN) Data[i, j] = parsed;
                        else Data[i, j] = double.NaN; // When parsing is not possible
                    }
                }
            };

            // Improve performance with asynchronous parallel processing
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
        /// Remove trailing blank lines
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private string[] CreateRawData(string filename)
        {
            var enc = new TxtEnc();
            var encoding = enc.SetFromTextFile(filename);
            if (encoding == null)
            {
                encoding = System.Text.Encoding.UTF8; // Use UTF-8 as default encoding
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
        /// At the beginning of the file,
        /// if non-numeric data is present,
        /// skip until a line with only numeric values appears,
        /// and return that line and subsequent lines
        /// </summary>
        /// <param name="data"></param>
        private string[] SplitHeader(string[] data)
        {
            var bodyDataBeginLine = SearchBodyBeginLine(data);
            if (bodyDataBeginLine == -1) // When there is no data at all
            {
                SetHeader(data.ToList());
                return new string[] { };
            }
            else // When there is data
            {
                List<string> bodyData = new List<string>();
                // Store header data
                SetHeader(data.Take(bodyDataBeginLine - 1).ToList());
                // Return body data
                return data.Skip(bodyDataBeginLine - 1).ToArray();
            }
        }

        /// <summary>
        /// Search for the start position of the data body
        /// Note: Returns -1 if there is no data at all
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private int SearchBodyBeginLine(string[] data)
        {
            var i = 0;
            while (ContainsString(data[i]))
            {
                if (i == data.Length - 1) // When reached the last line
                {
                    i = -1;
                    break;
                }
                i++;
            }
            return i;
        }

        /// <summary>
        /// Convert to string and set to Header property
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
        /// Check if the data contains non-numeric values (strings)
        /// </summary>
        /// <param name="v">Data for one line of the CSV file</param>
        /// <returns></returns>
        private bool ContainsString(string v)
        {
            var splitter = new char[] { '\t', ',', ' ' };
            var tempLine = v.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
            return ContainsString(tempLine);
        }

        /// <summary>
        /// Check if array elements contain non-numeric values
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
                    break; // Exit if not numeric
                }
            }

            return containsString;
        }
    }
}