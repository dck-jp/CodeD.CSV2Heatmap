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
        public void GridCsvParserTest1()
        {
            var filePath = TestHelpers.GetTestFilePath("grid_test1.csv");
            var p = new GridCsvParser(filePath);
            var y = (int)(p.AsDynamic().YSize);
            y.Is(1);
        }

        [TestMethod]
        public void GridCsvParserTest2()
        {
            var filePath = TestHelpers.GetTestFilePath("grid_test2.csv");
            var p = new GridCsvParser(filePath);
            var y = (int)(p.AsDynamic().YSize);
            y.Is(2);
        }

        [TestMethod()]
        public void CreateRawDataTest1()
        {
            var filePath = TestHelpers.GetTestFilePath("grid_test1.csv");
            var p = new GridCsvParser(filePath);
            var s = p.AsDynamic().CreateRawData(filePath) as string[];
            s.Length.Is(1);
        }

        [TestMethod()]
        public void CreateRawDataTest2()
        {
            var filePath = TestHelpers.GetTestFilePath("grid_test2.csv");
            var p = new GridCsvParser(filePath);
            var s = p.AsDynamic().CreateRawData(filePath) as string[];
            s.Length.Is(2);
        }

        [TestMethod()]
        public void GetSplitCharTest1()
        {
            var filePath = TestHelpers.GetTestFilePath("grid_test1.csv");
            var p = new GridCsvParser(filePath);
            var raw = p.AsDynamic().CreateRawData(filePath) as string[];
            var s = p.AsDynamic().SplitHeader(raw) as string[];
            s.Length.Is(1);
        }

        [TestMethod()]
        public void GetSplitCharTest2()
        {
            var filePath = TestHelpers.GetTestFilePath("grid_test2.csv");
            var p = new GridCsvParser(filePath);
            var raw = p.AsDynamic().CreateRawData(filePath) as string[];
            var s = p.AsDynamic().SplitHeader(raw) as string[];
            s.Length.Is(2);
        }
    }
}