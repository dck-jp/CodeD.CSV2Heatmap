# Baseline benchmark: GridCsvParser (perf/gridcsvparser-zeroalloc)

Commit: c8a6902 (perf/gridcsvparser-zeroalloc)
Date: 2025-11-17T00:00:00+09:00

Benchmark: GridCsvParserBenchmarks.Parse_Sample_Temperature (DefaultJob)

Results (from `benchmarks/CodeD.CSV2Heatmap.Benchmarks/results/results/CodeD.CSV2Heatmap.Benchmarks.GridCsvParserBenchmarks-report.csv`):

- Mean: 117.9 μs
- StdDev: 0.91 μs
- Error (99.9% CI half-width): 1.17 μs
- Gen0: 0.2441
- Allocated: 19.98 KB

Command used to reproduce:
```powershell
dotnet run -c Release --project "benchmarks/CodeD.CSV2Heatmap.Benchmarks/CodeD.CSV2Heatmap.Benchmarks.csproj"
```

Notes:
- The benchmark runs a full parse of `samples/grid_sample_temperature.csv`. If you want to re-run the benchmark, ensure the `samples` folder is in the repository root.
- The benchmark harness creates CSV/HTML reports under `benchmarks/CodeD.CSV2Heatmap.Benchmarks/results/results`.
