using System;
using System.IO;
using System.Reflection;

namespace CodeD.Tests
{
    /// <summary>
    /// Common helper methods for test classes
    /// </summary>
    public static class TestHelpers
    {
        /// <summary>
        /// Get the full path to a test data file in the TestData folder
        /// </summary>
        /// <param name="fileName">Name of the test data file</param>
        /// <returns>Full path to the test data file</returns>
        public static string GetTestFilePath(string fileName)
        {
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(assemblyLocation, "TestData", fileName);
        }
    }
}
