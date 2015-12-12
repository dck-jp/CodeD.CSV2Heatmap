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
    public class ZMappingDataTests
    {
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
            var filename = "test.csv";
            var contents = @"0.1, 0.1, 0.1
";
            File.WriteAllText(filename, contents);

            var zmap = new ZMappingData(filename, 0);
            zmap.Data.IsNotNull();

            File.Delete(filename);
        }

        [TestMethod()]
        public void ZMappingDataTest_FileRead2()
        {
            var filename = "test.csv";
            var contents = @"0.1, 0.1, 0.1
0.1, 0.1, 0.1
";
            File.WriteAllText(filename, contents);

            var zmap = new ZMappingData(filename, 0);
            //zmap.Data.GetLength(0).Is(3); //X方向
            //zmap.Data.GetLength(1).Is(2); //X方向
            zmap.Data.Is(new[,] { { 0.1, 0.1, 0.1 }, { 0.1, 0.1, 0.1 } });

            File.Delete(filename);
        }

    }
}