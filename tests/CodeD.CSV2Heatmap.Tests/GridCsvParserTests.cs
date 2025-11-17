using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Buffers;
using System.Runtime.InteropServices;

namespace CodeD.Tests
{
    [TestClass()]
    public class ZMapParserTests
    {
        [TestMethod]
        public async Task GridCsvParserTest1()
        {
            var filePath = TestHelpers.GetTestFilePath("grid_test1.csv");
            var p = await GridCsvParser.CreateAsync(filePath);
            p.YSize.Is(1);
        }

        [TestMethod]
        public async Task GridCsvParserTest2()
        {
            var filePath = TestHelpers.GetTestFilePath("grid_test2.csv");
            var p = await GridCsvParser.CreateAsync(filePath);
            p.YSize.Is(2);
        }

        // Note: CreateRawData is a private method. This test uses reflection (AsDynamic).
        // Consider removing if internal method testing is not needed.
        [TestMethod()]
        public async Task CreateRawDataTest1()
        {
            var filePath = TestHelpers.GetTestFilePath("grid_test1.csv");
            var p = await GridCsvParser.CreateAsync(filePath);
            var s = p.AsDynamic().CreateRawData(filePath) as string[];
            s.Length.Is(1);
        }

        [TestMethod()]
        public async Task CreateRawDataTest2()
        {
            var filePath = TestHelpers.GetTestFilePath("grid_test2.csv");
            var p = await GridCsvParser.CreateAsync(filePath);
            var s = p.AsDynamic().CreateRawData(filePath) as string[];
            s.Length.Is(2);
        }

        [TestMethod()]
        public async Task GetSplitCharTest1()
        {
            var filePath = TestHelpers.GetTestFilePath("grid_test1.csv");
            var p = await GridCsvParser.CreateAsync(filePath);
            var raw = p.AsDynamic().CreateRawData(filePath) as string[];
            var s = p.AsDynamic().SplitHeader(raw) as string[];
            s.Length.Is(1);
        }

        [TestMethod()]
        public async Task GetSplitCharTest2()
        {
            var filePath = TestHelpers.GetTestFilePath("grid_test2.csv");
            var p = await GridCsvParser.CreateAsync(filePath);
            var raw = p.AsDynamic().CreateRawData(filePath) as string[];
            var s = p.AsDynamic().SplitHeader(raw) as string[];
            s.Length.Is(2);
        }

        [TestMethod]
        public async Task GridSampleStarParsingTest()
        {
            // Resolve samples path relative to the test assembly location
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var root = Path.GetFullPath(Path.Combine(assemblyLocation, "..", "..", "..", "..", ".."));
            var filePath = Path.Combine(root, "samples", "grid_sample_star.csv");

            var p = await GridCsvParser.CreateAsync(filePath);

            // Basic assertions to ensure parser returns some sensible results
            Assert.IsTrue(p.YSize > 0);
            Assert.IsTrue(p.XSize > 0);
            Assert.IsNotNull(p.Data);
            Assert.IsTrue(p.Min <= p.Max);
            // Header should contain width info from sample header
            Assert.IsFalse(string.IsNullOrEmpty(p.Header));
            Assert.IsTrue(p.Header.Contains("width"));
        }

        [TestMethod]
        public async Task GetRowMajorBuffer_Extract_And_Constraints()
        {
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var root = Path.GetFullPath(Path.Combine(assemblyLocation, "..", "..", "..", "..", ".."));
            var filePath = Path.Combine(root, "samples", "grid_sample_star.csv");

            var p = await GridCsvParser.CreateAsync(filePath);

            // Prepare expected row-major array from Data for comparison
            var expected = new double[p.XSize * p.YSize];
            for (int c = 0; c < p.XSize; c++)
            {
                for (int r = 0; r < p.YSize; r++)
                {
                    expected[r * p.XSize + c] = p.Data[c, r];
                }
            }

            // Get the row-major buffer without creating Data copy (should exist and be non-empty)
            var mem = p.GetRowMajorBuffer(createIfMissing: true);
            Assert.IsFalse(mem.IsEmpty);
            Assert.IsTrue(mem.Length >= p.XSize * p.YSize);
            // Compare first X*Y elements
            Assert.IsTrue(MemoryMarshal.TryGetArray(mem, out ArraySegment<double> seg));
            for (int i = 0; i < expected.Length; i++) Assert.AreEqual(expected[i], seg.Array[seg.Offset + i]);

            // Extract buffer ownership, after this Data property should be invalid
            var buf = p.ExtractRowMajorBuffer(createIfMissing: true);
            Assert.IsNotNull(buf);
            Assert.IsTrue(buf.Length >= p.XSize * p.YSize);
            for (int i = 0; i < expected.Length; i++) Assert.AreEqual(expected[i], buf[i]);

            // Data access should now throw
            Assert.ThrowsException<InvalidOperationException>(() => { var d = p.Data; });

            // Caller is responsible for returning the pooled buffer
            ArrayPool<double>.Shared.Return(buf);
        }
    }
}