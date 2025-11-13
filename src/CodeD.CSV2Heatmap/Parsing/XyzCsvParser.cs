using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using G_PROJECT;

namespace CodeD
{
    public class XyzCsvParser
    {
        public string Header { get; private set; }

        private List<double> _X = new List<double>();
        private List<double> _Y = new List<double>();
        private List<double> _Z = new List<double>();
        private int _MaxZColNum; // Number of columns
        private bool _LoadingSuccess;
	
        public XyzCsvParser(string filePath, int zColNum)
        {
            Encoding encoding;
            using (var txtEnc = new TxtEnc())
            {
                encoding = txtEnc.SetFromTextFile(filePath);
            }
            // Use UTF-8 as default if encoding is null
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }
            var rawDataLines = File.ReadAllLines(filePath, encoding);
            rawDataLines = SplitHeader(rawDataLines);
            if (rawDataLines.Length == 0) return;

            var splitter = GetSplitChar(rawDataLines[0]);
            _MaxZColNum = rawDataLines[0].Trim().Split(splitter, StringSplitOptions.RemoveEmptyEntries).Length;
            if (_MaxZColNum < zColNum) return;

            ExtractXYZ(rawDataLines, zColNum, splitter);
            _LoadingSuccess = true;
        }

        private void ExtractXYZ(string[] rawDataLines, int zColNum, char[] splitter)
        {
            var dataNum = rawDataLines.Length;
            _X.Capacity = dataNum;
            _Y.Capacity = dataNum;
            _Z.Capacity = dataNum;

            var tempX = new double[dataNum];
            var tempY = new double[dataNum];
            var tempZ = new double[dataNum];

            Parallel.For(0, dataNum, i =>
            {
                var line = rawDataLines[i];
                string[] data = line.Trim().Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                tempX[i] = ConvertToDouble(data[0]);
                tempY[i] = ConvertToDouble(data[1]);
                tempZ[i] = ConvertToDouble(data[zColNum - 1]);
            });
            
            _X.AddRange(tempX);
            _Y.AddRange(tempY);
            _Z.AddRange(tempZ);
        }

        private double ConvertToDouble(string s)
        {
            return double.TryParse(s, out double parsed) ? parsed : double.NaN;
        }

        private char[] GetSplitChar(string line1)
        {
            if (line1.Contains("\t")) { return new[] { '\t' }; }
            else if (line1.Contains(",")) { return new[] { ',' }; }
            else { return new[] { ' ' }; }
        }
	
        /// <summary>
        /// Skip lines at the beginning of the file that contain non-numeric data
        /// until a line with only numeric values appears,
        /// then return that line and all subsequent lines
        /// </summary>
        /// <param name="data"></param>
        private string[] SplitHeader(string[] data)
        {
            List<string> bodyData = new List<string>();
            List<string> headerData = new List<string>();

            var splitter = new char[] { '\t', ',', ' ' };
            var bodyDataStart = 0;
            // Store header data + search for body data start position
            string[] tempLine;
            for (int i = 0; i != data.Length; i++)
            {
                tempLine = data[i].Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                if (ContainsString(tempLine)) { headerData.Add(data[i]); }
                else { bodyDataStart = i; break; }

                if (bodyDataStart == 0 && i == data.Length - 1) bodyDataStart = data.Length - 1;
            }
            // Store body data
            if (bodyDataStart != data.Length - 1)
            {
                for (int i = bodyDataStart; i != data.Length; i++) { bodyData.Add(data[i]); }
            }
            SetHeader(headerData);
            return bodyData.ToArray();
        }

        /// <summary>
        /// Check if the elements contain non-numeric data
        /// </summary>
        /// <param name="tempLine"></param>
        /// <returns></returns>
        private bool ContainsString(string[] tempLine)
        {
            if (tempLine.Length == 0) return true;
            
            for (int i = 0; i != tempLine.Length; i++)
            {
                if (!double.TryParse(tempLine[i], out _))
                {
                    return true; // Exit if not a number
                }
            }
            return false;
        }

        private void SetHeader(List<string> headerData)
        {
            StringBuilder sb = new StringBuilder();
            var maxItemCount = headerData.Count();
            for (int i = 0; i < maxItemCount; i++) { sb.AppendLine(headerData[i]); }

            Header = sb.ToString();
        }
        	
        public double[,] ToArray()
        {
            if (!_LoadingSuccess) return null;

            var x_max = _X.Max();
            var x_mins = _X.Distinct().OrderBy(i => i).Take(2).ToArray();
            var x_delta = x_mins[1] - x_mins[0];
            var xSize = (int)((x_max - x_mins[0]) / x_delta + 1);

            var y_max = _Y.Max();
            var y_mins = _Y.Distinct().OrderBy(i => i).Take(2).ToArray();
            var y_delta = y_mins[1] - y_mins[0];
            var ySize = (int)((y_max - y_mins[0]) / y_delta + 1);

            var output = new double[xSize, ySize];
            var maxItem = _X.Count();
            Parallel.For(0, maxItem, i =>
            {
                var xPos = (int)((_X[i] - x_mins[0]) / x_delta);
                var yPos = (int)((_Y[i] - y_mins[0]) / y_delta);
                output[xPos, yPos] = _Z[i];
            });
            return output;
        }
    }
}
