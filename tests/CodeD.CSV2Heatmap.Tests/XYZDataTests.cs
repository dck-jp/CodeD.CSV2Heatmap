using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeD;

namespace CodeD.Tests
{
    [TestClass]
    public class XYZDataTests
    {
        private string GetTestFilePath(string fileName)
        {
            // Get file path from TestData folder
            return Path.Combine(Path.GetDirectoryName(typeof(XYZDataTests).Assembly.Location), "TestData", fileName);
        }

        [TestMethod]
        public void XYZData_TabSeparated_WithHeader_Test()
        {
            // Arrange
            var filePath = GetTestFilePath("sample_tab_separated.txt");
            var zColNum = 3; // Use z1 column

            // Act
            var xyz = new XYZData(filePath, zColNum);
            var array = xyz.ToArray();
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
        public void XYZData_CommaSeparated_Test()
        {
            // Arrange
            var filePath = GetTestFilePath("sample_comma_separated.txt");
            var zColNum = 4; // Use 4th column

            // Act
            var xyz = new XYZData(filePath, zColNum);
            var array = xyz.ToArray();
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
        public void XYZData_RealData_Test()
        {
            // Arrange
            var filePath = GetTestFilePath("sample_real_data.txt");
            var zColNum = 4; // Use 4th Z value

            // Act
            var xyz = new XYZData(filePath, zColNum);
            var header = xyz.Header;
            var array = xyz.ToArray();

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
        public void XYZData_InvalidColumnNumber_Test()
        {
            // Arrange
            var filePath = GetTestFilePath("sample_tab_separated.txt");
            var zColNum = 10; // Non-existent column number

            // Act
            var xyz = new XYZData(filePath, zColNum);
            var array = xyz.ToArray();

            // Assert
            Assert.IsNotNull(xyz, "XYZData object should be created");
            Assert.IsNull(array, "Array should be null for invalid column number");
        }

        [TestMethod]
        public void XYZData_HeaderParsing_Test()
        {
            // Arrange
            var filePath = GetTestFilePath("sample_tab_separated.txt");
            var zColNum = 3;

            // Act
            var xyz = new XYZData(filePath, zColNum);

            // Assert
            Assert.IsNotNull(xyz.Header, "Header should be retrieved");
            Assert.IsTrue(xyz.Header.Length > 0, "Header should not be empty");
            var headerLines = xyz.Header.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.IsTrue(headerLines.Length >= 2, "Header should have 2 or more lines");
        }
    }
}