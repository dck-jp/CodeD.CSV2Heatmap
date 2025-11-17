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
    public class GridCsvParser : IDisposable
    {
        public string Header { get; private set; }
        private double[,] _data;
        private readonly object _dataLock = new object();
        private double[] _rowMajorBuffer; // internal row-major buffer used during parsing
        private bool _rowMajorBufferIsFromPool;
        private bool _rowMajorBufferWasExtracted;
        /// <summary>
        /// Two-dimensional data array. To avoid unnecessary copying, the parser stores
        /// an internal row-major buffer while parsing. Accessing this property will
        /// allocate and copy the data into a `double[,]` and then return the internal
        /// pooled buffer to the ArrayPool.
        /// </summary>
        public double[,] Data
        {
            get
            {
                if (_rowMajorBufferWasExtracted)
                {
                    throw new InvalidOperationException("Row-major buffer was extracted; Data[,] is not available. Create Data before extracting the buffer or use GetRowMajorBuffer/Create copy.");
                }
                if (_data == null)
                {
                    lock (_dataLock)
                    {
                        if (_data == null)
                        {
                            if (_rowMajorBuffer == null)
                            {
                                // uninitialized buffer -> create empty array sized by XSize/YSize
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
                                // return the rented buffer to the pool if it was one
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

        private bool _disposed;
        /// <summary>
        /// Dispose resources (return pooled buffers if still retained)
        /// </summary>
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

        ~GridCsvParser()
        {
            Dispose(false);
        }

        /// <summary>
        /// Returns a read-only memory view over the internal row-major buffer used for parsing.
        /// If no internal buffer exists and <paramref name="createIfMissing"/> is true, a pooled buffer
        /// will be created (and retained) and returned as a readonly view.
        /// If you need to take ownership of the buffer (for zero-copy consumption), use ExtractRowMajorBuffer().
        /// </summary>
        /// <param name="createIfMissing">Whether to create a pooled buffer from Data[,] if the row-major buffer is not present.</param>
        /// <returns>ReadOnlyMemory&lt;double&gt; referencing the buffer or an empty memory if no buffer is present and createIfMissing is false.</returns>
        public ReadOnlyMemory<double> GetRowMajorBuffer(bool createIfMissing = false)
        {
            if (_rowMajorBuffer != null) return new ReadOnlyMemory<double>(_rowMajorBuffer, 0, XSize * YSize);
            if (!createIfMissing) return ReadOnlyMemory<double>.Empty;
            if (_data == null) return ReadOnlyMemory<double>.Empty;
            // Create a pooled row-major buffer from Data[,] and retain it
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

        /// <summary>
        /// Extracts the internal row-major buffer and transfers ownership to the caller.
        /// After calling this method, the parser will no longer have the row-major buffer and
        /// `Data` will be unavailable (calling `Data` will throw an InvalidOperationException),
        /// unless you recreate Data by assigning it explicitly. Use this if you want zero-copy access
        /// to the parser results and you will return the buffer to ArrayPool when done.
        /// </summary>
        /// <param name="createIfMissing">If true and the internal row-major buffer is not present but Data is available, create a pooled buffer containing the Data contents and transfer it.</param>
        /// <returns>The row-major buffer (may be rented from ArrayPool). Caller takes ownership and must return the buffer to the pool when finished.</returns>
        public double[] ExtractRowMajorBuffer(bool createIfMissing = false)
        {
            if (_rowMajorBuffer == null)
            {
                if (!createIfMissing) throw new InvalidOperationException("No row-major buffer available; call GetRowMajorBuffer(true) to create one or pass createIfMissing=true.");
                if (_data == null) throw new InvalidOperationException("Data is not available to create a row-major buffer.");
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
            }
            // Transfer ownership
            var ret = _rowMajorBuffer;
            _rowMajorBuffer = null;
            _rowMajorBufferIsFromPool = false;
            _rowMajorBufferWasExtracted = true;
            return ret;
        }
        public int XSize { get; private set; }
        public int YSize { get; private set; }
        public double Max { get; private set; }
        public double Min { get; private set; }

        private GridCsvParser()
        {
        }

        // Row segment inside a pooled char buffer
        private struct RowSegment
        {
            public int RowIndex;
            public int Offset;
            public int Length;
        }

        // A pooled char buffer containing multiple row segments
        private class PooledChunk
        {
            public char[] Buffer;
            public int UsedLength;
            public List<RowSegment> Rows;
            public PooledChunk(char[] buffer, int usedLength)
            {
                Buffer = buffer;
                UsedLength = usedLength;
                Rows = new List<RowSegment>();
            }
        }

        // Delegate type used when double.TryParse(ReadOnlySpan<char>, NumberStyles, IFormatProvider, out double) exists
        private delegate bool SpanTryParseDelegate(ReadOnlySpan<char> span, NumberStyles style, IFormatProvider provider, out double result);
        private static readonly SpanTryParseDelegate TryParseSpanDelegate;

        static GridCsvParser()
        {
            try
            {
                var readOnlySpanType = typeof(ReadOnlySpan<char>);
                var method = typeof(double).GetMethod("TryParse", new Type[] { readOnlySpanType, typeof(NumberStyles), typeof(IFormatProvider), typeof(double).MakeByRefType() });
                if (method != null)
                {
                    // Create delegate for the static method
                    TryParseSpanDelegate = (SpanTryParseDelegate)Delegate.CreateDelegate(typeof(SpanTryParseDelegate), method);
                }
            }
            catch
            {
                // When not available (e.g., .NET Framework / older runtime), leave null and fallback to string parsing
                TryParseSpanDelegate = null;
            }
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
            // Determine encoding
            var detectionResult = CharsetDetector.DetectFromFile(filename);
            var encoding = detectionResult.Detected?.Encoding ?? Encoding.UTF8;

            // First pass: identify header lines and determine XSize/YSize
            var headerLines = new List<string>();
            char[] spliter = null;
            int xSize = 0;
            int ySize = 0;
            using (var sr = new StreamReader(filename, encoding))
            {
                string line;
                bool bodyStarted = false;
                while ((line = await sr.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    if (!bodyStarted && ContainsString(line))
                    {
                        headerLines.Add(line);
                        continue;
                    }
                    if (!bodyStarted)
                    {
                        bodyStarted = true;
                        spliter = GetSplitChar(line);
                        xSize = CountTokens(line.AsSpan(), spliter);
                        ySize = 1;
                    }
                    else
                    {
                        ySize++;
                    }
                }
            }

            if (ySize == 0)
            {
                SetHeader(headerLines);
                return;
            }

            XSize = xSize;
            YSize = ySize;
            // internal contiguous buffer (row-major) used during parsing
            var pool = ArrayPool<double>.Shared;
            var rentedBuffer = pool.Rent(XSize * YSize);
            // For safety, initialize the requested portion to NaN (optional: parsing will set values anyway)
            // but we rely on parse routines to fill missing entries with NaN.
            var buffer = rentedBuffer;
            bool keepRentedBuffer = false;
            // Prepare for parallel parsing using producer-consumer pattern
            int workerCount = Math.Max(1, Environment.ProcessorCount);
            // Zero-allocation producer: we pass pooled char buffers with row segments. Consumers must return buffers to pool.
            var queue = new System.Collections.Concurrent.BlockingCollection<PooledChunk>(boundedCapacity: 64);
            var workers = new Task[workerCount];
            var workerLocals = new (double min, double max)[workerCount];

            try
            {
                SetHeader(headerLines);
            for (int w = 0; w < workerCount; w++)
            {
                workerLocals[w] = (double.PositiveInfinity, double.NegativeInfinity);
                int wi = w;
                // workers will be started inside the pinned block to allow pointer-based writes
            }

            // Second pass: read the file again and enqueue body lines while workers parse via pointer
            unsafe
            {
                fixed (double* basePtr = buffer)
                {
                    var baseAddress = (IntPtr)basePtr;
                    // create worker tasks that call pointer-based parse routine (writing directly into the rented double[] buffer)
                    for (int w = 0; w < workerCount; w++)
                    {
                        int wi = w;
                        workers[w] = Task.Run(() =>
                        {
                            foreach (var chunk in queue.GetConsumingEnumerable())
                            {
                                try
                                {
                                    foreach (var seg in chunk.Rows)
                                    {
                                    unsafe
                                    {
                                        double* ptr = (double*)baseAddress.ToPointer();
                                        ReadOnlySpan<char> rowSpan = new ReadOnlySpan<char>(chunk.Buffer, seg.Offset, seg.Length);
                                        ParseTokensToRowToBuffer(rowSpan, spliter, seg.RowIndex, ptr, XSize, ref workerLocals[wi].min, ref workerLocals[wi].max);
                                    }
                                    }
                                }
                                finally
                                {
                                    // Return the chunk buffer to the pool
                                    if (chunk.Buffer != null)
                                    {
                                        ArrayPool<char>.Shared.Return(chunk.Buffer);
                                        chunk.Buffer = null; // prevent double return
                                    }
                                }
                            }
                        });
                    }

                    using (var sr = new StreamReader(filename, encoding))
                    {
                        string line;
                        // skip header lines (synchronous to avoid await inside unsafe block)
                        int skipped = 0;
                        while (skipped < headerLines.Count && (line = sr.ReadLine()) != null)
                        {
                            skipped++;
                        }
                        // Now use pooled char[] chunk-based reader to avoid allocations for each line
                        int rowIndex = 0;
                        const int CharChunkSize = 16 * 1024; // 16KB char chunks
                        var tailBuffer = ArrayPool<char>.Shared.Rent(1024);
                        int tailLen = 0;
                        try
                        {
                            while (!sr.EndOfStream)
                            {
                                // Rent a char buffer and prepare the start offset if we have partial from last read
                                var buf = ArrayPool<char>.Shared.Rent(CharChunkSize);
                                int writePos = 0;
                                if (tailLen > 0)
                                {
                                    // move partial tail into beginning of buf
                                    Buffer.BlockCopy(tailBuffer, 0, buf, 0, tailLen * sizeof(char));
                                    writePos = tailLen;
                                    tailLen = 0;
                                }
                                int read = sr.Read(buf, writePos, buf.Length - writePos);
                                if (read == 0)
                                {
                                    // no more chars
                                    ArrayPool<char>.Shared.Return(buf);
                                    break;
                                }
                                int used = writePos + read;
                                // Scan buffer for line breaks and create a chunk for each set of rows up to a batch limit
                                var chunk = new PooledChunk(buf, used);
                                int start = 0;
                                for (int i = 0; i < used; i++)
                                {
                                    if (buf[i] == '\n')
                                    {
                                        int lineEnd = i;
                                        // handle CRLF and CR only
                                        if (lineEnd > start && buf[lineEnd - 1] == '\r') lineEnd--;
                                        int len = lineEnd - start;
                                        chunk.Rows.Add(new RowSegment { RowIndex = rowIndex, Offset = start, Length = len });
                                        rowIndex++;
                                        start = i + 1;
                                        // nothing special here: send entire buffer as one chunk to queue
                                    }
                                }
                                // if we have trailing partial line, copy to tailBuffer
                                if (start < used)
                                {
                                    int partialLen = used - start;
                                    if (tailBuffer.Length < partialLen) { ArrayPool<char>.Shared.Return(tailBuffer); tailBuffer = ArrayPool<char>.Shared.Rent(partialLen); }
                                    Buffer.BlockCopy(buf, start * sizeof(char), tailBuffer, 0, partialLen * sizeof(char));
                                    tailLen = partialLen;
                                }
                                // If the chunk had any rows and its buffer differs from the current buf, return empty chunk; else add if has rows
                                if (chunk.Rows.Count > 0)
                                {
                                    chunk.UsedLength = used; // set used length for reference (not strictly necessary for parsing segments)
                                    queue.Add(chunk);
                                }
                                else
                                {
                                    // nothing to parse; return buffer now
                                    ArrayPool<char>.Shared.Return(buf);
                                }
                            }
                            // if we have partial tail remaining at EOF, enqueue it as a final row
                            if (tailLen > 0)
                            {
                                var finalBuf = ArrayPool<char>.Shared.Rent(tailLen);
                                Buffer.BlockCopy(tailBuffer, 0, finalBuf, 0, tailLen * sizeof(char));
                                var finalChunk = new PooledChunk(finalBuf, tailLen);
                                finalChunk.Rows.Add(new RowSegment { RowIndex = rowIndex, Offset = 0, Length = tailLen });
                                queue.Add(finalChunk);
                                rowIndex++;
                                tailLen = 0;
                            }
                        }
                        finally
                        {
                            if (tailBuffer != null) ArrayPool<char>.Shared.Return(tailBuffer);
                        }
                    }

                    // complete and wait for workers to finish (synchronous wait to avoid await in unsafe context)
                    queue.CompleteAdding();
                    Task.WaitAll(workers);

                    // After parsing into the buffer, retain the row-major buffer for lazy Data creation
                    _rowMajorBuffer = rentedBuffer;
                    _rowMajorBufferIsFromPool = true;
                    keepRentedBuffer = true;
                    // done with pinned buffer
                }
            }
            }
            finally
            {
                // If parse failed, return the rented array; otherwise, retained for lazy Data creation
                if (!keepRentedBuffer && rentedBuffer != null)
                {
                    pool.Return(rentedBuffer, clearArray: false);
                }
            }

            // combine worker local min/max
            double localMin = double.PositiveInfinity, localMax = double.NegativeInfinity;
            foreach (var (min, max) in workerLocals)
            {
                if (min < localMin) localMin = min;
                if (max > localMax) localMax = max;
            }
            Min = double.IsPositiveInfinity(localMin) ? double.NaN : localMin;
            Max = double.IsNegativeInfinity(localMax) ? double.NaN : localMax;

            // If Data has already been materialized (unlikely), we already returned the buffer. Otherwise we leave _rowMajorBuffer retained.
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

        private char[] GetSplitChar(string line1)
        {
            if (line1.Contains("\t")) { return new[] { '\t' }; }
            else if (line1.Contains(",")) { return new[] { ',' }; }
            else { return new[] { ' ' }; }
        }

        private static int CountTokens(ReadOnlySpan<char> span, char[] separators)
        {
            int count = 0;
            int i = 0;
            while (i < span.Length)
            {
                // skip separators
                while (i < span.Length && IsSeparator(span[i], separators)) i++;
                if (i >= span.Length) break;
                // token start
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

        private static bool TryParseToken(ReadOnlySpan<char> tokenSpan, out double parsed)
        {
#if NET8_0_OR_GREATER
            return double.TryParse(tokenSpan, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed);
#else
            if (TryParseSpanDelegate != null)
            {
                return TryParseSpanDelegate(tokenSpan, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed);
            }
            else
            {
                var s = tokenSpan.ToString();
                return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed);
            }
#endif
        }

        private static void ParseTokensToRow(ReadOnlySpan<char> span, char[] separators, int rowIndex, double[] buffer, int xSize, ref double localMin, ref double localMax)
        {
            int i = 0; // position in span
            int col = 0;
            while (i < span.Length && col < xSize)
            {
                while (i < span.Length && IsSeparator(span[i], separators)) i++;
                if (i >= span.Length) break;
                int start = i;
                while (i < span.Length && !IsSeparator(span[i], separators)) i++;
                var tokenSpan = span.Slice(start, i - start);
                double parsed;
                bool parsedOk = TryParseToken(tokenSpan, out parsed);
                if (parsedOk && !double.IsNaN(parsed))
                {
                    buffer[rowIndex * xSize + col] = parsed;
                    if (parsed < localMin) localMin = parsed;
                    if (parsed > localMax) localMax = parsed;
                }
                else buffer[rowIndex * xSize + col] = double.NaN;
                col++;
            }
            // fill rest with NaN
            for (; col < xSize; col++) buffer[rowIndex * xSize + col] = double.NaN;
        }

        private static unsafe void ParseTokensToRowPtr(ReadOnlySpan<char> span, char[] separators, int rowIndex, double* basePtr, int xSize, ref double localMin, ref double localMax)
        {
            int i = 0; // position in span
            int col = 0;
            while (i < span.Length && col < xSize)
            {
                while (i < span.Length && IsSeparator(span[i], separators)) i++;
                if (i >= span.Length) break;
                int start = i;
                while (i < span.Length && !IsSeparator(span[i], separators)) i++;
                var tokenSpan = span.Slice(start, i - start);
                double parsed;
                bool parsedOk = TryParseToken(tokenSpan, out parsed);
                if (parsedOk && !double.IsNaN(parsed))
                {
                    basePtr[rowIndex * xSize + col] = parsed;
                    if (parsed < localMin) localMin = parsed;
                    if (parsed > localMax) localMax = parsed;
                }
                else basePtr[rowIndex * xSize + col] = double.NaN;
                col++;
            }
            // fill rest with NaN
            for (; col < xSize; col++) basePtr[rowIndex * xSize + col] = double.NaN;
        }

        private static unsafe void ParseTokensToRowToData(ReadOnlySpan<char> span, char[] separators, int rowIndex, double* dataBasePtr, int xSize, int ySize, ref double localMin, ref double localMax)
        {
            int i = 0;
            int col = 0;
            while (i < span.Length && col < xSize)
            {
                while (i < span.Length && IsSeparator(span[i], separators)) i++;
                if (i >= span.Length) break;
                int start = i;
                while (i < span.Length && !IsSeparator(span[i], separators)) i++;
                var tokenSpan = span.Slice(start, i - start);
                double parsed;
                bool parsedOk = TryParseToken(tokenSpan, out parsed);
                if (parsedOk && !double.IsNaN(parsed))
                {
                    dataBasePtr[col * ySize + rowIndex] = parsed; // Data[col, rowIndex]
                    if (parsed < localMin) localMin = parsed;
                    if (parsed > localMax) localMax = parsed;
                }
                else
                {
                    dataBasePtr[col * ySize + rowIndex] = double.NaN;
                }
                col++;
            }
            for (; col < xSize; col++) dataBasePtr[col * ySize + rowIndex] = double.NaN;
        }

        private static unsafe void ParseTokensToRowToBuffer(ReadOnlySpan<char> span, char[] separators, int rowIndex, double* bufferPtr, int xSize, ref double localMin, ref double localMax)
        {
            int i = 0;
            int col = 0;
            while (i < span.Length && col < xSize)
            {
                while (i < span.Length && IsSeparator(span[i], separators)) i++;
                if (i >= span.Length) break;
                int start = i;
                while (i < span.Length && !IsSeparator(span[i], separators)) i++;
                var tokenSpan = span.Slice(start, i - start);
                double parsed;
                bool parsedOk = TryParseToken(tokenSpan, out parsed);
                if (parsedOk && !double.IsNaN(parsed))
                {
                    bufferPtr[rowIndex * xSize + col] = parsed;
                    if (parsed < localMin) localMin = parsed;
                    if (parsed > localMax) localMax = parsed;
                }
                else bufferPtr[rowIndex * xSize + col] = double.NaN;
                col++;
            }
            for (; col < xSize; col++) bufferPtr[rowIndex * xSize + col] = double.NaN;
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
            var separators = new char[] { '\t', ',', ' ' };
            var span = v.AsSpan();
            int i = 0;
            bool foundToken = false;
            while (i < span.Length)
            {
                while (i < span.Length && IsSeparator(span[i], separators)) i++;
                if (i >= span.Length) break;
                int start = i;
                while (i < span.Length && !IsSeparator(span[i], separators)) i++;
                var token = span.Slice(start, i - start);
                if (!TryParseToken(token, out double trash))
                {
                    return true; // contains non-numeric token
                }
                foundToken = true;
            }
            // no tokens means line is empty -> treat as not numeric
            if (!foundToken) return true;
            return false;
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