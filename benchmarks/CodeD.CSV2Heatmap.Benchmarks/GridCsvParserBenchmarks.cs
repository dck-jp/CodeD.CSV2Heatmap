using BenchmarkDotNet.Attributes;
using CodeD;
using System;
using System.IO;
using System.Threading.Tasks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 5)]
public class GridCsvParserBenchmarks
{
    private string sampleFilePath = default!;

    [GlobalSetup]
    public void Setup()
    {
        // Resolve sample path by walking up from the current base directory until a samples folder is found
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        string? found = null;
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "samples", "grid_sample_star.csv");
            if (File.Exists(candidate)) { found = candidate; break; }
            dir = dir.Parent;
        }
        sampleFilePath = found ?? throw new FileNotFoundException("Benchmark sample file not found: samples/grid_sample_star.csv");
        if (!File.Exists(sampleFilePath)) throw new FileNotFoundException("Benchmark sample file not found", sampleFilePath);
    }

    [Benchmark]
    public async Task Parse_GridSampleStar()
    {
        var p = await GridCsvParser.CreateAsync(sampleFilePath).ConfigureAwait(false);
        GC.KeepAlive(p);
    }
}
