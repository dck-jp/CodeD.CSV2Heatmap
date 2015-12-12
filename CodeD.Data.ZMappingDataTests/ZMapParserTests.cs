using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeD.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CodeD.Data.Tests
{
    [TestClass()]
    public class ZMapParserTests
    {
        //データ行1行 + 空白行
        public readonly string testFilename01 = "test1.csv";
        //データ行2行 + 空白行
        public readonly string testFilename02 = "test2.csv";

        [TestInitialize()]
        public void Initialize()
        {
            var contents = @"0.1, 0.1, 0.1
";
            File.WriteAllText(testFilename01, contents);

            var contents2 = @"0.1, 0.1, 0.1
0.2, 0.2, 0.2
";
            File.WriteAllText(testFilename02, contents2);
        }
        [TestCleanup()]
        public void Cleanup()
        {
            File.Delete(testFilename01);
            File.Delete(testFilename02);
        }

        [TestMethod()]
        public void ZMapParserTest1()
        {
            var p = new ZMapParser(testFilename01);
            var y = (int)(p.AsDynamic().YSize);
            y.Is(1);
        }

        [TestMethod()]
        public void ZMapParserTest2()
        {
            var p = new ZMapParser(testFilename02);
            var y = (int)(p.AsDynamic().YSize);
            y.Is(2);
        }

        [TestMethod()]
        public void CreateRawDataTest1()
        {
            var p = new ZMapParser(testFilename01);
            var s = p.AsDynamic().CreateRawData(testFilename01) as string[];
            s.Length.Is(1);
        }

        [TestMethod()]
        public void CreateRawDataTest2()
        {
            var p = new ZMapParser(testFilename02);
            var s = p.AsDynamic().CreateRawData(testFilename02) as string[];
            s.Length.Is(2);
        }

        [TestMethod()]
        public void SplitHeaderTest1()
        {
            var p = new ZMapParser(testFilename01);
            var raw = p.AsDynamic().CreateRawData(testFilename01) as string[];
            var s = p.AsDynamic().SplitHeader(raw) as string[];
            s.Length.Is(1);
        }

        [TestMethod()]
        public void SplitHeaderTest2()
        {
            var p = new ZMapParser(testFilename02);
            var raw = p.AsDynamic().CreateRawData(testFilename02) as string[];
            var s = p.AsDynamic().SplitHeader(raw) as string[];
            s.Length.Is(2);
        }
    }
}