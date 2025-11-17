using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtfUnknown;
using System.Globalization;
using System.Buffers;

namespace CodeD
{
    public class XyzCsvParser : IDisposable
    {
        public string Header { get; private set; }
        private double[,] _data;
        private readonly object _dataLock = new object();
        private double[] _rowMajorBuffer;
        private bool _rowMajorBufferIsFromPool;
        private bool _rowMajorBufferWasExtracted;
        public double[,] Data
        {
            get
            {
                if (!_LoadingSuccess) return null;
                if (_rowMajorBufferWasExtracted)
                {
                    throw new InvalidOperationException("Row-major buffer was extracted; Data[,] is not available. Create Data before extracting or use GetRowMajorBuffer/ExtractRowMajorBuffer.");
                }
                if (_data == null)
                {
                    lock (_dataLock)
                    {
                        if (_data == null)
                        {
                            if (_rowMajorBuffer == null)
                            {
                                _data = new double[XSize, YSize];
                            }
                            else
                            {
                                var data2d = new double[XSize, YSize];
                                for (int col = 0; col < XSize; col++)
                                {
                                    for (int row = 0; row < YSize; row++)
                                    {
                                        data2d[col, row] = _rowMajorBuffer[row * XSize + col];
                                    }
                                }
                                _data = data2d;
                                if (_rowMajorBufferIsFromPool)
                                {
                                    ArrayPool<double>.Shared.Return(_rowMajorBuffer, clearArray: false);
                                }
                                _rowMajorBuffer = null;
                                _rowMajorBufferIsFromPool = false;
                            }
                        }
                    }
                }
                return _data;
            }
            private set => _data = value;
        }
        public int XSize { get; private set; }
        public int YSize { get; private set; }
        public double Max { get; private set; }
        public double Min { get; private set; }

        private List<double> _X = new List<double>();
        private List<double> _Y = new List<double>();
        private List<double> _Z = new List<double>();
        private int _MaxZColNum; // Number of columns
        private bool _LoadingSuccess;

        private XyzCsvParser()
        {
        }

        private bool _disposed;
        public ReadOnlyMemory<double> GetRowMajorBuffer(bool createIfMissing = false)
        {
            if (_rowMajorBuffer != null) return new ReadOnlyMemory<double>(_rowMajorBuffer, 0, XSize * YSize);
            if (!createIfMissing) return ReadOnlyMemory<double>.Empty;
            if (_data == null) return ReadOnlyMemory<double>.Empty;
            var pool = ArrayPool<double>.Shared;
            var buf = pool.Rent(XSize * YSize);
            for (int col = 0; col < XSize; col++)
            {
                for (int row = 0; row < YSize; row++)
                {
                    buf[row * XSize + col] = _data[col, row];
                }
            }
            _rowMajorBuffer = buf;
            _rowMajorBufferIsFromPool = true;
            _rowMajorBufferWasExtracted = false;
            return new ReadOnlyMemory<double>(_rowMajorBuffer, 0, XSize * YSize);
        }

        public double[] ExtractRowMajorBuffer(bool createIfMissing = false)
        {
            if (_rowMajorBuffer == null)
            {
                if (!createIfMissing) throw new InvalidOperationException("No row-major buffer available; call GetRowMajorBuffer(true) or ExtractRowMajorBuffer(true) to create one.");
                if (_data == null) throw new InvalidOperationException("Data is not available to create a row-major buffer.");
                var pool = ArrayPool<double>.Shared;
                var buf = pool.Rent(XSize * YSize);
                for (int col = 0; col < XSize; col++)
                {
                    for (int row = 0; row < YSize; row++) buf[row * XSize + col] = _data[col, row];
                }
                _rowMajorBuffer = buf;
                _rowMajorBufferIsFromPool = true;
            }
            var ret = _rowMajorBuffer;
            _rowMajorBuffer = null;
            _rowMajorBufferIsFromPool = false;
            _rowMajorBufferWasExtracted = true;
            return ret;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;
            if (_rowMajorBuffer != null && _rowMajorBufferIsFromPool)
            {
                ArrayPool<double>.Shared.Return(_rowMajorBuffer, clearArray: false);
                _rowMajorBuffer = null;
                _rowMajorBufferIsFromPool = false;
            }
        }

        ~XyzCsvParser()
        {
            Dispose(false);
        }

        public static async Task<XyzCsvParser> CreateAsync(string filePath, int zColNum)
        {
            var parser = new XyzCsvParser();
            await parser.ParseXyzFileAsync(filePath, zColNum).ConfigureAwait(false);
            return parser;
        }

        private async Task ParseXyzFileAsync(string filePath, int zColNum)
        {
            var detectionResult = CharsetDetector.DetectFromFile(filePath);
            var encoding = detectionResult.Detected?.Encoding ?? Encoding.UTF8;
            // Stream the file and parse without allocating per-line strings when possible
            var splitter = default(char[]);
            var headerLines = new List<string>();
            bool bodyStarted = false;

            const int CharChunkSize = 16 * 1024; // 16KB
            var tailBuffer = ArrayPool<char>.Shared.Rent(1024);
            try
            {
                using (var sr = new StreamReader(filePath, encoding))
                {
                    while (!sr.EndOfStream)
                    {
                        var buf = ArrayPool<char>.Shared.Rent(CharChunkSize);
                        int writePos = 0;
                        if (tailBuffer.Length > 0 && tailBuffer[0] != '\0')
                        {
                            // move existing tail into buffer
                            // find tail length via sentinel? We store len separately; but we track tailLen below
                        }
                        int read = sr.Read(buf, writePos, buf.Length - writePos);
                        if (read == 0)
                        {
                            ArrayPool<char>.Shared.Return(buf);
                            break;
                        }
                        int used = writePos + read;
                        int start = 0;
                        for (int i = 0; i < used; i++)
                        {
                            if (buf[i] == '\n')
                            {
                                int lineEnd = i;
                                if (lineEnd > start && buf[lineEnd - 1] == '\r') lineEnd--;
                                int len = lineEnd - start;
                                var span = new ReadOnlySpan<char>(buf, start, len);
                                if (!bodyStarted)
                                {
                                    if (ContainsString(span)) { headerLines.Add(new string(span.ToArray())); }
                                    else
                                    {
                                        bodyStarted = true;
                                        splitter = GetSplitChar(new string(span.ToArray()));
                                        _MaxZColNum = CountTokens(span, splitter);
                                        if (_MaxZColNum < zColNum) { ArrayPool<char>.Shared.Return(buf); return; }
                                        // parse this first data line
                                        ParseLineToXYZ(span, splitter, zColNum, out double xx, out double yy, out double zz);
                                        _X.Add(xx); _Y.Add(yy); _Z.Add(zz);
                                    }
                                }
                                else
                                {
                                    // parse data line
                                    ParseLineToXYZ(span, splitter, zColNum, out double xx, out double yy, out double zz);
                                    _X.Add(xx); _Y.Add(yy); _Z.Add(zz);
                                }
                                start = i + 1;
                            }
                        }
                        // Handle tail partial
                        if (start < used)
                        {
                            int tailLen = used - start;
                            if (tailBuffer.Length < tailLen) { ArrayPool<char>.Shared.Return(tailBuffer); tailBuffer = ArrayPool<char>.Shared.Rent(tailLen); }
                            Buffer.BlockCopy(buf, start * sizeof(char), tailBuffer, 0, tailLen * sizeof(char));
                            // store tail into a temp string for next iteration
                            var tailStr = new string(tailBuffer, 0, tailLen);
                            // we will process tail in the next read by copying into the beginning of buf
                            // Simpler approach: rework to avoid tailBuffer complexity; for now, return buf and fallback to ReadLine for tail
                            ArrayPool<char>.Shared.Return(buf);
                            string tailLine = tailStr;
                            // Attempt to read the rest of the line via ReadLine() and parse
                            var rest = sr.ReadLine();
                            if (!string.IsNullOrEmpty(rest)) tailLine += rest;
                            var span2 = tailLine.AsSpan();
                            if (!bodyStarted)
                            {
                                if (ContainsString(span2)) { headerLines.Add(tailLine); }
                                else
                                {
                                    bodyStarted = true;
                                    splitter = GetSplitChar(tailLine);
                                    _MaxZColNum = CountTokens(span2, splitter);
                                    if (_MaxZColNum < zColNum) return;
                                    ParseLineToXYZ(span2, splitter, zColNum, out double xx, out double yy, out double zz);
                                    _X.Add(xx); _Y.Add(yy); _Z.Add(zz);
                                }
                            }
                            else
                            {
                                ParseLineToXYZ(span2, splitter, zColNum, out double xx, out double yy, out double zz);
                                _X.Add(xx); _Y.Add(yy); _Z.Add(zz);
                            }
                        }
                        else
                        {
                            ArrayPool<char>.Shared.Return(buf);
                        }
                    }
                }
            }
            finally
            {
                if (tailBuffer != null) ArrayPool<char>.Shared.Return(tailBuffer);
            }

            _LoadingSuccess = true;

            // Convert to 2D array (row-major buffer) and set properties
            await Task.Run(() => {
                var buflen = _X.Count;
            }).ConfigureAwait(false);
            // We will create the buffer via ToArray method
            var buf2d = ToArrayRowMajor();
            if (buf2d != null)
            {
                _rowMajorBuffer = buf2d;
                _rowMajorBufferIsFromPool = true;
                XSize = (int)((_X.Max() - _X.Distinct().OrderBy(i => i).Take(2).ToArray()[0]) / (_X.Distinct().OrderBy(i => i).Take(2).ToArray()[1] - _X.Distinct().OrderBy(i => i).Take(2).ToArray()[0]) + 1);
                YSize = (int)((_Y.Max() - _Y.Distinct().OrderBy(i => i).Take(2).ToArray()[0]) / (_Y.Distinct().OrderBy(i => i).Take(2).ToArray()[1] - _Y.Distinct().OrderBy(i => i).Take(2).ToArray()[0]) + 1);
                // Compute Min/Max
                double min = double.PositiveInfinity, max = double.NegativeInfinity;
                for (int i = 0; i < XSize * YSize; i++)
                {
                    var v = _rowMajorBuffer[i];
                    if (!double.IsNaN(v))
                    {
                        if (v < min) min = v;
                        if (v > max) max = v;
                    }
                }
                Min = double.IsPositiveInfinity(min) ? double.NaN : min;
                Max = double.IsNegativeInfinity(max) ? double.NaN : max;
            }
                // set header and mark parsing completed
                SetHeader(headerLines);
                // parsing completed
                if (!_LoadingSuccess) _LoadingSuccess = true;
            
            // Convert to 2D array and set properties
            Data = ToArray();
            if (Data != null)
            {
                XSize = Data.GetLength(0);
                YSize = Data.GetLength(1);
                Max = Data.Cast<double>().Max();
                Min = Data.Cast<double>().Min();
            }
        }

        private void ExtractXYZ(string[] rawDataLines, int zColNum, char[] splitter)
        {
            var dataNum = rawDataLines.Length;
            _X.Capacity = dataNum;
            _Y.Capacity = dataNum;
            _Z.Capacity = dataNum;

            var tempX = new double[dataNum];
            var tempY = new double[dataNum];
            var tempZ = new double[dataNum];

            Parallel.For(0, dataNum, i =>
            {
                var line = rawDataLines[i];
                string[] data = line.Trim().Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                tempX[i] = ConvertToDouble(data[0]);
                tempY[i] = ConvertToDouble(data[1]);
                tempZ[i] = ConvertToDouble(data[zColNum - 1]);
            });
            
            _X.AddRange(tempX);
            _Y.AddRange(tempY);
            _Z.AddRange(tempZ);
        }

        // Helper: Check span tokens for non-numeric content
        private static bool ContainsString(ReadOnlySpan<char> span)
        {
            var separators = new char[] { '\t', ',', ' ' };
            int i = 0;
            bool foundToken = false;
            while (i < span.Length)
            {
                while (i < span.Length && IsSeparator(span[i], separators)) i++;
                if (i >= span.Length) break;
                int start = i;
                while (i < span.Length && !IsSeparator(span[i], separators)) i++;
                var token = span.Slice(start, i - start);
                if (!TryParseToken(token, out _)) return true;
                foundToken = true;
            }
            if (!foundToken) return true;
            return false;
        }

        private static int CountTokens(ReadOnlySpan<char> span, char[] separators)
        {
            int count = 0;
            int i = 0;
            while (i < span.Length)
            {
                while (i < span.Length && IsSeparator(span[i], separators)) i++;
                if (i >= span.Length) break;
                count++;
                while (i < span.Length && !IsSeparator(span[i], separators)) i++;
            }
            return count;
        }

        private static bool IsSeparator(char c, char[] separators)
        {
            for (int i = 0; i < separators.Length; i++) if (c == separators[i]) return true;
            return false;
        }

        // Convert tokens to X,Y,Z values by position
        private static bool ParseLineToXYZ(ReadOnlySpan<char> span, char[] separators, int zColNum, out double x, out double y, out double z)
        {
            x = double.NaN; y = double.NaN; z = double.NaN;
            int i = 0; int col = 0;
            while (i < span.Length)
            {
                while (i < span.Length && IsSeparator(span[i], separators)) i++;
                if (i >= span.Length) break;
                int start = i;
                while (i < span.Length && !IsSeparator(span[i], separators)) i++;
                var token = span.Slice(start, i - start);
                if (col == 0) TryParseToken(token, out x);
                else if (col == 1) TryParseToken(token, out y);
                else if (col == zColNum - 1) TryParseToken(token, out z);
                col++;
            }
            return true;
        }

        private delegate bool SpanTryParseDelegate(ReadOnlySpan<char> span, System.Globalization.NumberStyles style, IFormatProvider provider, out double result);
        private static readonly SpanTryParseDelegate TryParseSpanDelegate;

        static XyzCsvParser()
        {
            try
            {
                var readOnlySpanType = typeof(ReadOnlySpan<char>);
                var method = typeof(double).GetMethod("TryParse", new Type[] { readOnlySpanType, typeof(System.Globalization.NumberStyles), typeof(IFormatProvider), typeof(double).MakeByRefType() });
                if (method != null)
                {
                    TryParseSpanDelegate = (SpanTryParseDelegate)Delegate.CreateDelegate(typeof(SpanTryParseDelegate), method);
                }
            }
            catch
            {
                TryParseSpanDelegate = null;
            }
        }

        private static bool TryParseToken(ReadOnlySpan<char> tokenSpan, out double parsed)
        {
            #if NET8_0_OR_GREATER
            return double.TryParse(tokenSpan, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out parsed);
            #else
            if (TryParseSpanDelegate != null)
            {
                return TryParseSpanDelegate(tokenSpan, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out parsed);
            }
            else
            {
                var s = tokenSpan.ToString();
                return double.TryParse(s, out parsed);
            }
            #endif
        }

        private double ConvertToDouble(string s)
        {
            return double.TryParse(s, out double parsed) ? parsed : double.NaN;
        }

        private char[] GetSplitChar(string line1)
        {
            if (line1.Contains("\t")) { return new[] { '\t' }; }
            else if (line1.Contains(",")) { return new[] { ',' }; }
            else { return new[] { ' ' }; }
        }
	
        /// <summary>
        /// Skip lines at the beginning of the file that contain non-numeric data
        /// until a line with only numeric values appears,
        /// then return that line and all subsequent lines
        /// </summary>
        /// <param name="data"></param>
        private string[] SplitHeader(string[] data)
        {
            List<string> bodyData = new List<string>();
            List<string> headerData = new List<string>();

            var splitter = new char[] { '\t', ',', ' ' };
            var bodyDataStart = 0;
            // Store header data + search for body data start position
            string[] tempLine;
            for (int i = 0; i != data.Length; i++)
            {
                tempLine = data[i].Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                if (ContainsString(tempLine)) { headerData.Add(data[i]); }
                else { bodyDataStart = i; break; }

                if (bodyDataStart == 0 && i == data.Length - 1) bodyDataStart = data.Length - 1;
            }
            // Store body data
            if (bodyDataStart != data.Length - 1)
            {
                for (int i = bodyDataStart; i != data.Length; i++) { bodyData.Add(data[i]); }
            }
            SetHeader(headerData);
            return bodyData.ToArray();
        }

        /// <summary>
        /// Check if the elements contain non-numeric data
        /// </summary>
        /// <param name="tempLine"></param>
        /// <returns></returns>
        private bool ContainsString(string[] tempLine)
        {
            if (tempLine.Length == 0) return true;
            
            for (int i = 0; i != tempLine.Length; i++)
            {
                if (!double.TryParse(tempLine[i], out _))
                {
                    return true; // Exit if not a number
                }
            }
            return false;
        }

        private void SetHeader(List<string> headerData)
        {
            StringBuilder sb = new StringBuilder();
            var maxItemCount = headerData.Count();
            for (int i = 0; i < maxItemCount; i++) { sb.AppendLine(headerData[i]); }

            Header = sb.ToString();
        }
        	
        private double[,] ToArray()
        {
            if (!_LoadingSuccess) return null;
            // If we already have a row-major buffer, construct 2D array from it
            if (_rowMajorBuffer != null)
            {
                var arr = new double[XSize, YSize];
                for (int col = 0; col < XSize; col++)
                {
                    for (int row = 0; row < YSize; row++)
                    {
                        arr[col, row] = _rowMajorBuffer[row * XSize + col];
                    }
                }
                return arr;
            }

            var x_max = _X.Max();
            var x_mins = _X.Distinct().OrderBy(i => i).Take(2).ToArray();
            var x_delta = x_mins[1] - x_mins[0];
            var xSize = (int)((x_max - x_mins[0]) / x_delta + 1);

            var y_max = _Y.Max();
            var y_mins = _Y.Distinct().OrderBy(i => i).Take(2).ToArray();
            var y_delta = y_mins[1] - y_mins[0];
            var ySize = (int)((y_max - y_mins[0]) / y_delta + 1);

            var output = new double[xSize, ySize];
            var maxItem = _X.Count();
            Parallel.For(0, maxItem, i =>
            {
                var xPos = (int)((_X[i] - x_mins[0]) / x_delta);
                var yPos = (int)((_Y[i] - y_mins[0]) / y_delta);
                output[xPos, yPos] = _Z[i];
            });
            return output;
        }

        private double[] ToArrayRowMajor()
        {
            if (!_LoadingSuccess) return null;
            var x_max = _X.Max();
            var x_mins = _X.Distinct().OrderBy(i => i).Take(2).ToArray();
            var x_delta = x_mins[1] - x_mins[0];
            var xSize = (int)((x_max - x_mins[0]) / x_delta + 1);

            var y_max = _Y.Max();
            var y_mins = _Y.Distinct().OrderBy(i => i).Take(2).ToArray();
            var y_delta = y_mins[1] - y_mins[0];
            var ySize = (int)((y_max - y_mins[0]) / y_delta + 1);

            var pool = ArrayPool<double>.Shared;
            var buf = pool.Rent(xSize * ySize);
            // Initialize to NaN
            for (int i = 0; i < xSize * ySize; i++) buf[i] = double.NaN;
            var maxItem = _X.Count;
            for (int i = 0; i < maxItem; i++)
            {
                var xPos = (int)((_X[i] - x_mins[0]) / x_delta);
                var yPos = (int)((_Y[i] - y_mins[0]) / y_delta);
                buf[yPos * xSize + xPos] = _Z[i];
            }
            // Set sizes for later
            XSize = xSize; YSize = ySize;
            return buf;
        }
    }
}
