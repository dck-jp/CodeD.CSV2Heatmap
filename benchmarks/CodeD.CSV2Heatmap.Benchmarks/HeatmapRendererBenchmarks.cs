using BenchmarkDotNet.Attributes;
using CodeD;
using System;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 5)]
public class HeatmapRendererBenchmarks
{
    private double[,] medium = default!;
    private double[,] large = default!;
    private double[,] xlarge = default!;
    
    private HeatmapRenderer mediumRenderer = default!;
    private HeatmapRenderer largeRenderer = default!;
    private HeatmapRenderer xlargeRenderer = default!;

    [GlobalSetup]
    public void Setup()
    {
        medium = MakeData(64, 64, (i, j) => (i * 64 + j) / (64.0 * 64.0 - 1));
        large = MakeData(256, 256, (i, j) => (i * 256 + j) / (256.0 * 256.0 - 1));
        xlarge = MakeData(1024, 1024, (i, j) => (i * 1024 + j) / (1024.0 * 1024.0 - 1));
        
        mediumRenderer = new HeatmapRenderer(medium, 1.0);
        largeRenderer = new HeatmapRenderer(large, 1.0);
        xlargeRenderer = new HeatmapRenderer(xlarge, 1.0);
    }

    [Benchmark]
    public void ToBitmap_Medium_None_Rainbow()
        => mediumRenderer.ToBitmap(0, 1.0, HeatmapRenderer.ColorMode.Rainbow, HeatmapRenderer.ConvertMode.None).Dispose();

    [Benchmark]
    public void ToBitmap_Large_None_Rainbow()
        => largeRenderer.ToBitmap(0, 1.0, HeatmapRenderer.ColorMode.Rainbow, HeatmapRenderer.ConvertMode.None).Dispose();

    [Benchmark]
    public void ToBitmap_XLarge_None_Rainbow()
        => xlargeRenderer.ToBitmap(0, 1.0, HeatmapRenderer.ColorMode.Rainbow, HeatmapRenderer.ConvertMode.None).Dispose();

    

    [Benchmark]
    public void ToBitmap_Medium_Log_Rainbow()
        => mediumRenderer.ToBitmap(0.0001, 1.0, HeatmapRenderer.ColorMode.Rainbow, HeatmapRenderer.ConvertMode.log).Dispose();

    [Benchmark]
    public void ToBitmap_Medium_Ln_Rainbow()
        => mediumRenderer.ToBitmap(0.0001, 1.0, HeatmapRenderer.ColorMode.Rainbow, HeatmapRenderer.ConvertMode.ln).Dispose();

    [Benchmark]
    public void PlaneCorrection_Medium()
        => GC.KeepAlive(mediumRenderer.GetPlaneCorrection());

    [Benchmark]
    public void RotateCW_Medium()
        => GC.KeepAlive(mediumRenderer.GetRotateCW());

    private static double[,] MakeData(int w, int h, Func<int, int, double> f)
    {
        var d = new double[w, h];
        for (int i = 0; i < w; i++)
            for (int j = 0; j < h; j++)
                d[i, j] = f(i, j);
        return d;
    }
}
