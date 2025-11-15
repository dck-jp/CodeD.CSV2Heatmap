# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2025-11-16

### Changed
- **Major performance improvements in HeatmapRenderer.ToBitmap():**
  - **Optimization v1**: SIMD vectorization via `Vector<double>`, parallel processing with `Parallel.For`, zero-allocation array slicing, and unsafe pointer direct pixel access
  - **Optimization v2**: Color map caching (Rainbow/Monochrome/BlackPurpleWhite) in static arrays to avoid repeated allocations
  - **Optimization v3**: Pre-calculated `Vector<double>.Count` as static readonly to reduce property access overhead in hot loops
  - **Optimization v4**: ConvertMode-specific fast paths with inlined branches, removing per-pixel delegate overhead; SIMD for `None` mode, specialized scalar paths for `log`/`ln`

### Performance
- **Speed improvements (vs baseline):**
  - Medium None: **+12.3%** (44.7us → 39.2us)
  - Large None: **+48.5%** (179.8us → 92.7us)
  - XLarge None: **+36.7%** (2.238ms → 1.417ms)
  - Medium Log: **+15.6%** (41.4us → 34.9us)
  - Medium Ln: **+70.3%** (118.6us → 35.2us)
- **Memory reductions (vs baseline):**
  - Medium: **-63% to -74%** (35.9KB → 13.1KB / 9.3-9.4KB)
  - Large: **-96.7%** (283.7KB → 9.3KB)
  - XLarge: **-99.7%** (4174.5KB → 14.5KB)
- See `docs/BENCHMARKS.md` for detailed metrics

## [1.0.2] - 2025-11-13

### Changed
- Added ConfigureAwait(false) to all await calls to prevent deadlocks in GUI applications
- Removed synchronous constructors from GridCsvParser and XyzCsvParser to encourage async usage patterns

### Fixed
- Resolved potential deadlock issues when using the library in GUI applications

## [1.0.1] - 2025

### Changed
- Updated README for better NuGet.org display

## [1.0.0] - 2025

### Added
- Initial release
- Grid Data support (GridCSV, Height Map, Surface Map, Intensity Map, Raster Map)
- Point Cloud Data support (XYZ Format, Scatter Data, Tabular XYZ Data)
- Multiple color modes (Rainbow, Monochrome, Black-Purple-White)
- SIMD-optimized rendering for high performance
- Basic image-processing tools (plane correction, rotation, trimming)
- Data conversion modes (Linear, Logarithmic, Natural-log scales)
- Cross-platform support via SkiaSharp
- Support for multiple file formats and delimiters
- Parallel processing optimization
- Asynchronous file I/O
- Memory-efficient direct pixel access
- Comprehensive unit test coverage