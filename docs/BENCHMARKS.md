# Benchmarks

- Purpose: Compare HeatmapRenderer performance before/after optimizations and keep reproducible records.

## How to Run

```powershell
# Run from repository root
dotnet run -c Release -p benchmarks/CodeD.CSV2Heatmap.Benchmarks/CodeD.CSV2Heatmap.Benchmarks.csproj
```

## Benchmark Scope

- Input Data: Generated in code (64x64 = Medium, 256x256 = Large, 1024x1024 = XLarge)
- Measured:
  - `ToBitmap` (ColorMode: Rainbow + ConvertMode: None)
  - `ToBitmap` (ColorMode: Rainbow + ConvertMode: log / ln) (Medium only)
  - Plane correction `GetPlaneCorrection` (Medium only)
  - Rotation `GetRotateCW` (Medium only)
- Memory profile: `MemoryDiagnoser` enabled
- Config: `warmupCount = 1`, `iterationCount = 5`

## Operational Guidelines

- Correctness ensured by unit tests (`tests/CodeD.CSV2Heatmap.Tests`).
- Avoid storing large input data; if needed place under `benchmarks/**/data/` and ignore via `.gitignore`.
- Append representative results (date + commit hash) here after significant optimization steps.

### Recording Template

| Date | Commit | Environment | Benchmark | Value |
|---|---|---|---|---|
| 2025-11-16 | abcdef0 | Win11, .NET 8.0, SkiaSharp 2.88.8 | ToBitmap_Medium_None_Rainbow | Mean 12.34 ms, Alloc 1.2 KB |

## Baseline (2025-11-16, commit e50297d)

Environment: Windows 11, .NET 8.0.22, 13th Gen Intel Core i9-13900F

| Case | Mean | Alloc |
|---|---:|---:|
| ToBitmap Medium None | 44.703 us | 35.85 KB |
| ToBitmap Medium log | 41.370 us | 32.51 KB |
| ToBitmap Medium ln | 118.575 us | 32.33 KB |
| ToBitmap Large None | 179.764 us | 283.67 KB |
| ToBitmap XLarge None | 2.238 ms | 4174.51 KB |
| PlaneCorrection Medium | 13.192 us | 32.11 KB |
| RotateCW Medium | 4.332 us | 32.11 KB |

## Optimization v1: SIMD + Parallel + Zero-Allocation (2025-11-16, commit 35ebeb2)

Environment: Windows 11, .NET 8.0.22, 13th Gen Intel Core i9-13900F

Changes:
- SIMD via `Vector<double>`
- Parallel.For across X dimension
- Reusable color index buffer
- Unsafe pointer direct pixel writes
- Zero-allocation array slicing (stackalloc + Unsafe.Read)

| Case | Mean | Alloc | Baseline | Speed Δ | Memory Δ |
|---|---:|---:|---:|---:|---:|
| ToBitmap Medium None | 40.588 us | 16.13 KB | 44.703 us | +9.2% | -55.0% |
| ToBitmap Medium log  | 42.284 us | 12.54 KB | 41.370 us | -2.2% | -61.4% |
| ToBitmap Medium ln   | 44.355 us | 12.26 KB | 118.575 us | +62.6% ⭐⭐⭐ | -62.1% |
| ToBitmap Large None  | 122.978 us | 13.10 KB | 179.764 us | +31.6% ⭐ | -95.4% ⭐⭐⭐ |
| ToBitmap XLarge None | 1.647 ms | 20.20 KB | 2.238 ms | +26.4% ⭐⭐ | -99.5% ⭐⭐⭐ |
| PlaneCorrection Medium | 13.104 us | 32.12 KB | 13.192 us | ≈0% | ≈0% |
| RotateCW Medium        | 4.212 us  | 32.12 KB | 4.332 us  | ≈0% | ≈0% |

## Optimization v2: Color Map Caching (2025-11-16, branch perf/heatmap-renderer)

Environment: Windows 11, .NET 8.0.22, 13th Gen Intel Core i9-13900F

Changes:
- Cache Rainbow / Monochrome / BlackPurpleWhite color maps in static arrays.
- Avoid constructing the 765-element SKColor array each call (~3KB saved per invocation).

| Case | Mean | Alloc | v1 (Mean/Alloc) | Speed Δ vs v1 | Memory Δ vs v1 | vs Baseline (Speed / Memory) |
|---|---:|---:|---:|---:|---:|---:|
| ToBitmap Medium None | 40.686 us | 13.12 KB | 40.588 us / 16.13 KB | ≈0% | -18.7% | +9.0% / -63.4% |
| ToBitmap Medium log  | 38.561 us | 10.34 KB | 42.284 us / 12.54 KB | +8.8% ⭐ | -17.5% | +6.8% / -68.2% |
| ToBitmap Medium ln   | 44.819 us | 9.00 KB  | 44.355 us / 12.26 KB | -1.0% | -26.6% | +62.2% / -72.2% ⭐⭐⭐ |
| ToBitmap Large None  | 120.049 us | 10.01 KB | 122.978 us / 13.10 KB | +2.4% | -23.6% | +33.2% / -96.5% ⭐⭐⭐ |
| ToBitmap XLarge None | 1.687 ms | 17.24 KB | 1.647 ms / 20.20 KB | -2.4% | -14.6% | +24.6% / -99.6% ⭐⭐⭐ |
| PlaneCorrection Medium | 12.982 us | 32.12 KB | 13.104 us / 32.12 KB | +0.9% | ≈0% | +1.6% / ≈0% |
| RotateCW Medium        | 4.300 us  | 32.12 KB | 4.212 us / 32.12 KB  | -2.1% | ≈0% | +0.7% / ≈0% |

Overall (Baseline → v2):
- Speed: Medium ln +62.2%, Large +33.2%, XLarge +24.6%
- Memory: XLarge -99.6%, Large -96.5%, Medium -63–72%
- Techniques: Zero-allocation + color map caching achieve persistent low memory.


## Optimization v3: Pre-calculate Vector<double>.Count (2025-11-16, branch perf/heatmap-renderer)

Environment: Windows 11, .NET 8.0.22, 13th Gen Intel Core i9-13900F

Changes:
- Introduced `private static readonly int VectorSize = Vector<double>.Count;` in `HeatmapRenderer`.
- Replaced per-iteration `Vector<double>.Count` property reads in hot paths with the cached `VectorSize`.
- No functional changes; micro-optimization to reduce overhead in the SIMD loop.

Results (vs v2):

| Case | Mean | Alloc | v2 | Δ Speed | Δ Memory |
|---|---:|---:|---:|---:|---:|
| ToBitmap Medium None | 39.712 us | 13.02 KB | 40.686 us / 13.12 KB | +2.4% | -0.8% |
| ToBitmap Medium log | 41.767 us | 9.19 KB | 38.561 us / 10.34 KB | -8.3% | -11.1% |
| ToBitmap Medium ln | 43.479 us | 9.20 KB | 44.819 us / 9.00 KB | +3.0% | +2.2% |
| ToBitmap Large None | 114.303 us | 10.04 KB | 120.049 us / 10.01 KB | +4.8% | +0.3% |
| ToBitmap XLarge None | 1.638 ms | 17.14 KB | 1.687 ms / 17.24 KB | +2.9% | -0.6% |
| PlaneCorrection Medium | 13.162 us | 32.12 KB | 12.982 us / 32.12 KB | -1.4% | ±0% |
| RotateCW Medium | 4.391 us | 32.12 KB | 4.300 us / 32.12 KB | -2.1% | ±0% |

Notes:
- The change targets the rendering SIMD loop; non-render methods (Plane/Rotate) are unaffected functionally. Minor variance is expected.
- The largest gains appear on Large/XLarge sizes where loop overhead accumulates.

## Optimization v4: ConvertMode-specific fast paths (2025-11-16, branch perf/heatmap-renderer)

Environment: Windows 11, .NET 8.0.22, 13th Gen Intel Core i9-13900F

Changes:
- Removed per-pixel delegate calls in hot loops; inlined ConvertMode branches.
- SIMD vectorization for `ConvertMode.None` with precomputed `scale` and `min` vectors.
- Scalar specialized paths for `ConvertMode.log` (natural log) and `ConvertMode.ln` (log10), avoiding delegate overhead.

Results (vs v3):

| Case | Mean | Alloc | v3 | Δ Speed | Δ Memory |
|---|---:|---:|---:|---:|---:|
| ToBitmap Medium None | 39.217 us | 13.11 KB | 39.712 us / 13.02 KB | +1.2% | +0.7% |
| ToBitmap Large None | 92.655 us | 9.31 KB | 114.303 us / 10.04 KB | **+18.9%** | **-7.3%** |
| ToBitmap XLarge None | 1.417 ms | 14.47 KB | 1.638 ms / 17.14 KB | **+13.5%** | **-15.6%** |
| ToBitmap Medium Log | 34.920 us | 9.30 KB | 41.767 us / 9.19 KB | **+16.4%** | +1.2% |
| ToBitmap Medium Ln | 35.223 us | 9.39 KB | 43.479 us / 9.20 KB | **+19.0%** | +2.1% |
| PlaneCorrection Medium | 13.018 us | 32.12 KB | 13.162 us / 32.12 KB | +1.1% | ±0% |
| RotateCW Medium | 4.160 us | 32.12 KB | 4.391 us / 32.12 KB | +5.3% | ±0% |

Notes:
- Major gains on Large/XLarge from eliminating per-pixel delegate overhead and using SIMD for the `None` path.
- `log/ln` paths benefit from devirtualized scalar math; additional vectorization would require Math intrinsics or approximations.


