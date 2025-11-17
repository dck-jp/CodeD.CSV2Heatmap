using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeD;

namespace CodeD.Tests
{
    [TestClass]
    public class XyzCsvParserTests
    {
        [TestMethod]
        public async System.Threading.Tasks.Task XyzCsvParser_TabSeparated_WithHeader_Test()
        {
            // Arrange
            var filePath = TestHelpers.GetTestFilePath("sample_tab_separated.txt");
            var zColNum = 3; // z1 column

            // Act
            var xyz = await XyzCsvParser.CreateAsync(filePath, zColNum);
            var array = xyz.Data;
            var header = xyz.Header;

            // Assert
            Assert.IsNotNull(xyz, "XYZData object should be created");
            Assert.IsNotNull(array, "Array should be generated");
            Assert.IsTrue(header.Contains("header1"), "Header information should be included");
            Assert.IsTrue(header.Contains("header2"), "Header information should be included");

            // Array size check (3x2 grid data)
            Assert.AreEqual(3, array.GetLength(0), "X dimension size should be correct");
            Assert.AreEqual(2, array.GetLength(1), "Y dimension size should be correct");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task XyzCsvParser_CommaSeparated_Test()
        {
            // Arrange
            var filePath = TestHelpers.GetTestFilePath("sample_comma_separated.txt");
            var zColNum = 3; // z1 column

            // Act
            var xyz = await XyzCsvParser.CreateAsync(filePath, zColNum);
            var array = xyz.Data;
            var header = xyz.Header;

            // Assert
            Assert.IsNotNull(xyz, "XYZData object should be created");
            Assert.IsNotNull(array, "Array should be generated");
            Assert.IsTrue(header.Contains("テストデータ"), "Header information should be included");

            // Array size check (3x2 grid data)
            Assert.AreEqual(3, array.GetLength(0), "X dimension size should be correct");
            Assert.AreEqual(2, array.GetLength(1), "Y dimension size should be correct");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task XyzCsvParser_RealData_Test()
        {
            // Arrange
            var filePath = TestHelpers.GetTestFilePath("sample_real_data.txt");
            var zColNum = 3; // z1 column

            // Act
            var xyz = await XyzCsvParser.CreateAsync(filePath, zColNum);
            var header = xyz.Header;
            var array = xyz.Data;

            // Assert
            Assert.IsNotNull(xyz, "XYZData object should be created");
            Assert.IsNotNull(array, "Array should be generated");
            Assert.IsTrue(header.Contains("データ開始"), "Header information should be included");

            // Array size check (3x3 grid data)
            Assert.AreEqual(3, array.GetLength(0), "X dimension size should be correct");
            Assert.AreEqual(3, array.GetLength(1), "Y dimension size should be correct");

            // Test specific values
            Assert.AreNotEqual(double.NaN, array[0, 0], "First data point should be valid value");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task XyzCsvParser_InvalidColumnNumber_Test()
        {
            // Arrange
            var filePath = TestHelpers.GetTestFilePath("sample_tab_separated.txt");
            var zColNum = 10; // Invalid column number

            // Act
            var xyz = await XyzCsvParser.CreateAsync(filePath, zColNum);
            var array = xyz.Data;

            // Assert
            Assert.IsNotNull(xyz, "XYZData object should be created");
            Assert.IsNull(array, "Array should be null for invalid column number");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task XyzCsvParser_HeaderParsing_Test()
        {
            // Arrange
            var filePath = TestHelpers.GetTestFilePath("sample_tab_separated.txt");
            var zColNum = 3; // z1 column

            // Act
            var xyz = await XyzCsvParser.CreateAsync(filePath, zColNum);

            // Assert
            Assert.IsNotNull(xyz.Header, "Header should be retrieved");
            Assert.IsTrue(xyz.Header.Length > 0, "Header should not be empty");
            var headerLines = xyz.Header.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.IsTrue(headerLines.Length >= 2, "Header should have 2 or more lines");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task XyzCsvParser_CreateAsync_Test()
        {
            // Arrange
            var filePath = TestHelpers.GetTestFilePath("sample_tab_separated.txt");
            var zColNum = 3; // z1 column

            // Act
            var xyz = await XyzCsvParser.CreateAsync(filePath, zColNum);
            var array = xyz.Data;
            var header = xyz.Header;

            // Assert
            Assert.IsNotNull(xyz, "XyzCsvParser object should be created");
            Assert.IsNotNull(array, "Array should be generated");
            Assert.IsTrue(header.Contains("header1"), "Header information should be included");
            Assert.IsTrue(header.Contains("header2"), "Header information should be included");

            // Array size check (3x2 grid data)
            Assert.AreEqual(3, array.GetLength(0), "X dimension size should be correct");
            Assert.AreEqual(2, array.GetLength(1), "Y dimension size should be correct");
        }
    }
}