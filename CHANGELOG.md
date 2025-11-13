# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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