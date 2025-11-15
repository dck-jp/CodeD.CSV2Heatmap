using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using System;
using System.IO;

internal class Program
{
    private static void Main(string[] args)
    {
        var artifactsPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "results"));
        Directory.CreateDirectory(artifactsPath);

        var config = DefaultConfig.Instance.WithArtifactsPath(artifactsPath);
        BenchmarkRunner.Run<HeatmapRendererBenchmarks>(config);
    }
}
