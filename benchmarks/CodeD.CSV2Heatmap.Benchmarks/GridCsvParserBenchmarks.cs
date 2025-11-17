using System;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using CodeD;

namespace CodeD.CSV2Heatmap.Benchmarks
{
    [MemoryDiagnoser]
    public class GridCsvParserBenchmarks
    {
        private string sampleFilePath = string.Empty;

        [GlobalSetup]
        public void Setup()
        {
            // samples folder is located relative to repository root; use directory traversal from bin
            sampleFilePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples", "grid_sample_temperature.csv"));
        }

        [Benchmark]
        public async Task Parse_Sample_Temperature()
        {
            var p = await GridCsvParser.CreateAsync(sampleFilePath);
        }
    }
}

