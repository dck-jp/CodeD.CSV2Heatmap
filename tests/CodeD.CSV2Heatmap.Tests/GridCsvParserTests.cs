using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

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
    }
}