using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using UtfUnknown;

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
            await parser.ParseZMapFileAsync(filename).ConfigureAwait(false);
            return parser;
        }

        /// <summary>
        /// Parse Grid data (CSV file) and set a two-dimensional double array to member variables
        /// Note 1: Delimiter characters are tab, comma, and space
        /// Note 2: If there are insufficient data points or invalid values, NaN is assigned
        /// </summary>
        /// <param name="filename">Path to the CSV file to parse</param>
        private async Task ParseZMapFileAsync(string filename)
        {
            var detectionResult = CharsetDetector.DetectFromFile(filename);
            var encoding = detectionResult.Detected?.Encoding ?? Encoding.UTF8;
            var content = File.ReadAllText(filename, encoding);
            ReadOnlySpan<char> contentSpan = content.AsSpan();
            // build line start indices, avoiding allocation of string[] per line
            var lineStarts = new List<int>();
            lineStarts.Add(0);
            for (int p = 0; p < contentSpan.Length; p++)
            {
                if (contentSpan[p] == '\r')
                {
                    if (p + 1 < contentSpan.Length && contentSpan[p + 1] == '\n') p++;
                    if (p + 1 < contentSpan.Length) lineStarts.Add(p + 1);
                }
                else if (contentSpan[p] == '\n')
                {
                    if (p + 1 < contentSpan.Length) lineStarts.Add(p + 1);
                }
            }
            if (lineStarts.Count == 0) return;

            // local helper removed because ReadOnlySpan is a ref struct and cannot be captured by lambdas

            // find beginning of numeric data
            int firstBodyLine = -1;
            for (int i = 0; i < lineStarts.Count; i++)
            {
                int startl = lineStarts[i];
                int endl = (i + 1 < lineStarts.Count) ? lineStarts[i + 1] - 1 : contentSpan.Length;
                while (endl > startl && (contentSpan[endl - 1] == '\r' || contentSpan[endl - 1] == '\n')) endl--;
                var l = contentSpan.Slice(startl, endl - startl).Trim();
                if (!l.IsEmpty && !ContainsNonNumeric(l))
                {
                    firstBodyLine = i;
                    break;
                }
            }
            if (firstBodyLine == -1)
            {
                // no data; set header to entire file
                SetHeader(new List<string> { content });
                return;
            }
            // collect header lines
            if (firstBodyLine > 0)
            {
                var headerList = new List<string>(firstBodyLine);
                for (int idx = 0; idx < firstBodyLine; idx++)
                {
                    int start = lineStarts[idx];
                    int end = (idx + 1 < lineStarts.Count) ? lineStarts[idx + 1] - 1 : content.Length;
                    while (end > start && (content.AsSpan()[end - 1] == '\r' || content.AsSpan()[end - 1] == '\n')) end--;
                    headerList.Add(content.Substring(start, end - start));
                }
                SetHeader(headerList);
            }

            int start1 = lineStarts[firstBodyLine];
            int end1 = (firstBodyLine + 1 < lineStarts.Count) ? lineStarts[firstBodyLine + 1] - 1 : contentSpan.Length;
            while (end1 > start1 && (contentSpan[end1 - 1] == '\r' || contentSpan[end1 - 1] == '\n')) end1--;
            var firstLineSpan = contentSpan.Slice(start1, end1 - start1).Trim();
            var spliter = GetSplitChar(firstLineSpan);
            XSize = CountTokens(firstLineSpan, spliter);
            YSize = lineStarts.Count - firstBodyLine;
            Data = new double[XSize, YSize];
            var threadNum = Environment.ProcessorCount;
            var range = new int[1 + threadNum];
            range[0] = 0;
            range[range.Length - 1] = YSize;
            for (var i = 1; i < threadNum; i++) { range[i] = YSize * i / threadNum; }

            var maxPerTask = new double[threadNum];
            var minPerTask = new double[threadNum];
            for (int ti = 0; ti < threadNum; ti++) { maxPerTask[ti] = double.MinValue; minPerTask[ti] = double.MaxValue; }

            Action<int, int, int> parser = (startLineNo, endLineNo, threadIndex) =>
            {
                var contentSpanLocal = content.AsSpan();
                double parsed;
                double localMax = double.MinValue;
                double localMin = double.MaxValue;
                for (int j = startLineNo; j < endLineNo; j++)
                {
                    int idxLine = j + firstBodyLine;
                    int sstart = lineStarts[idxLine];
                    int send = (idxLine + 1 < lineStarts.Count) ? lineStarts[idxLine + 1] - 1 : contentSpanLocal.Length;
                    while (send > sstart && (contentSpanLocal[send - 1] == '\r' || contentSpanLocal[send - 1] == '\n')) send--;
                    var span = contentSpanLocal.Slice(sstart, send - sstart).Trim();
                    int i = 0;
                    int idx = 0;
                    while (i < XSize && idx < span.Length)
                    {
                        // Find next token boundary and trim
                        int start = idx;
                        while (idx < span.Length && span[idx] != spliter) idx++;
                        var token = span.Slice(start, idx - start).Trim();
                        if (token.Length == 0)
                        {
                            /* skip empty tokens (multiple delimiters) */
                            idx++; // skip delimiter
                            continue;
                        }

                        if (TryParseDouble(token, out parsed) && !double.IsNaN(parsed))
                        {
                            Data[i, j] = parsed;
                            if (parsed > localMax) localMax = parsed;
                            if (parsed < localMin) localMin = parsed;
                        }
                        else Data[i, j] = double.NaN; // When parsing is not possible
                        i++;

                        // move past delimiter
                        if (idx < span.Length && span[idx] == spliter) idx++;
                    }

                    // fill remaining with NaN when insufficient data
                    for (; i < XSize; i++) Data[i, j] = double.NaN;
                }
                maxPerTask[threadIndex] = localMax;
                minPerTask[threadIndex] = localMin;
            };

            // Improve performance with asynchronous parallel processing
            var tasks = new Task[threadNum];
            for (int i = 0; i < threadNum; i++) 
            {
                var startRange = range[i];
                var endRange = range[i + 1];
                var ti = i; // capture
                tasks[i] = Task.Run(() => parser(startRange, endRange, ti));
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);

            // Combine per-task min/max
            Max = maxPerTask.Max();
            Min = minPerTask.Min();
        }

        /// <summary>
        /// Remove trailing blank lines
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private string[] CreateRawData(string filename)
        {
            var detectionResult = CharsetDetector.DetectFromFile(filename);
            var encoding = detectionResult.Detected?.Encoding ?? Encoding.UTF8;
            var rawDataLines = File.ReadAllLines(filename, encoding);
            if (rawDataLines.Length > 0 && rawDataLines[rawDataLines.Length - 1] == "")
            {
                Array.Resize(ref rawDataLines, rawDataLines.Length - 1);
            }
            return rawDataLines;
        }

        private char GetSplitChar(string line1)
        {
            if (line1.Contains("\t")) { return '\t'; }
            else if (line1.Contains(",")) { return ','; }
            else { return ' '; }
        }

        private char GetSplitChar(ReadOnlySpan<char> line1)
        {
            if (line1.IndexOf('\t') >= 0) return '\t';
            if (line1.IndexOf(',') >= 0) return ',';
            return ' ';
        }

        private bool ContainsNonNumeric(ReadOnlySpan<char> span)
        {
            var splitter = GetSplitChar(span);
            if (span.Trim().IsEmpty) return true;
            int idx = 0;
            while (idx < span.Length)
            {
                int start = idx;
                while (idx < span.Length && span[idx] != splitter) idx++;
                var token = span.Slice(start, idx - start).Trim();
                if (token.Length == 0)
                {
                    idx++; // skip delimiter
                    continue;
                }
                if (!TryParseDouble(token, out _)) return true;
                if (idx < span.Length && span[idx] == splitter) idx++;
            }
            return false;
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
            while (ContainsNonNumeric(data[i]))
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
        private bool ContainsNonNumeric(string v)
        {
            var span = v.AsSpan();
            var splitter = GetSplitChar(v);
            // if the line is empty, consider it non-numeric (part of header)
            if (span.Trim().IsEmpty) return true;

            int idx = 0;
            while (idx < span.Length)
            {
                int start = idx;
                while (idx < span.Length && span[idx] != splitter) idx++;
                var token = span.Slice(start, idx - start).Trim();
                if (token.Length == 0)
                {
                    idx++; // skip delimiter
                    continue; // skip empty
                }
                if (!TryParseDouble(token, out _)) return true;
                if (idx < span.Length && span[idx] == splitter) idx++;
            }
            return false;
        }

        private static bool TryParseDouble(ReadOnlySpan<char> token, out double val)
        {
    #if NETCOREAPP3_1_OR_GREATER || NET5_0_OR_GREATER || NET6_0_OR_GREATER || NET7_0_OR_GREATER || NET8_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            return double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out val);
    #else
            // fall back to older frameworks that don't support TryParse(ReadOnlySpan<char>)
            return double.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out val);
    #endif
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

        private static int CountTokens(ReadOnlySpan<char> s, char delimiter)
        {
            if (s.IsEmpty) return 0;
            int count = 0;
            int idx = 0;
            while (idx < s.Length)
            {
                int start = idx;
                while (idx < s.Length && s[idx] != delimiter) idx++;
                var token = s.Slice(start, idx - start).Trim();
                if (token.Length > 0) count++;
                if (idx < s.Length && s[idx] == delimiter) idx++;
            }
            return count;
        }
    }
}