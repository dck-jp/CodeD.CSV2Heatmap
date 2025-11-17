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
            // Locate the repository root by searching up from the AppContext base directory until 'samples' folder is found
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "samples", "grid_sample_temperature.csv");
                if (File.Exists(candidate))
                {
                    sampleFilePath = candidate;
                    return;
                }
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("Could not locate 'samples/grid_sample_temperature.csv' starting from AppContext.BaseDirectory");
        }

        [Benchmark]
        public async Task Parse_Sample_Temperature()
        {
            var p = await GridCsvParser.CreateAsync(sampleFilePath);
        }
    }
}

