using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeD.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace CodeD.Data.Tests
{
    [TestClass()]
    public class ZMappingDataTests
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
        public void ZMappingDataTest()
        {
            var src = new[,] { { 0.1, 0.1, 0.1 } };
            var zmap = new ZMappingData(src, 0);
            zmap.Data.IsNotNull();
        }

        [TestMethod()]
        public void ZMappingDataTest_FileRead1()
        {
            var zmap = new ZMappingData(testFilename01, 0);
            zmap.Data.GetLength(0).Is(3); //X方向
            zmap.Data.GetLength(1).Is(1); //Y方向 
        }

        [TestMethod()]
        public void ZMappingDataTest_FileRead2()
        {
            var zmap = new ZMappingData(testFilename02, 0);
            zmap.Data.GetLength(0).Is(3); //X方向
            zmap.Data.GetLength(1).Is(2); //X方向       
        }

        [TestMethod()]
        public void ToBitmapTest()
        {
            var zmap = new ZMappingData(testFilename01, 0);
            var bitmap = zmap.ToBitmap();
            bitmap.Width.Is(3);
            bitmap.Height.Is(1);
            bitmap.Dispose(); // SkiaSharpではリソース解放が必要
        }
    }
}