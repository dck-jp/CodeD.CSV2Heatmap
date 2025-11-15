# Benchmarks

- 目的: `HeatmapRenderer` の最適化前後で性能を比較し、再現可能な形で残す。

## 実行方法

```powershell
# ルートで実行
dotnet run -c Release -p benchmarks/CodeD.CSV2Heatmap.Benchmarks/CodeD.CSV2Heatmap.Benchmarks.csproj
```
## ベンチ内容

- 入力データ: コードで生成（64x64 : Medium, 256x256, 1024x1024）
- 計測項目:
  - `ToBitmap`（`ColorMode: Rainbow` + `ConvertMode: None`）
  - `ToBitmap`（`ColorMode: Rainbow` + `ConvertMode: log/ln`） (Mediumのみ)
  - 平面補正 `GetPlaneCorrection`（Medium のみ）
  - 回転 `GetRotateCW`（Medium のみ）
- メモリプロファイル: `MemoryDiagnoser` 有効
- 設定: `warmupCount: 1`, `targetCount: 5`

## 運用指針

- 正しさの担保はユニットテスト（`tests/CodeD.CSV2Heatmap.Tests`）。
- 大きな入力ファイルは原則持たず、必要時は `benchmarks/**/data/` に置き `.gitignore` で除外。
- 最適化前後の代表結果は、このファイルに日付・コミットハッシュ付きで追記してください。

### 記録テンプレート

| 日付 | commit | 環境 | ベンチ | 値 |
|---|---|---|---|---|
| 2025-11-16 | abcdef0 | Win11, .NET 8.0, SkiaSharp 2.88.8 | ToBitmap_Medium_None_Rainbow | Mean 12.34 ms, Alloc 1.2 KB |

## ベースライン（2025-11-16, commit e50297d）

環境: Windows 11, .NET 8.0.22, 13th Gen Intel Core i9-13900F

| Case | Mean | Alloc |
|---|---:|---:|
| ToBitmap Medium None | 44.703 us | 35.85 KB |
| ToBitmap Medium log | 41.370 us | 32.51 KB |
| ToBitmap Medium ln | 118.575 us | 32.33 KB |
| ToBitmap Large None | 179.764 us | 283.67 KB |
| ToBitmap XLarge None | 2.238 ms | 4174.51 KB |
| PlaneCorrection Medium | 13.192 us | 32.11 KB |
| RotateCW Medium | 4.332 us | 32.11 KB |

## 最適化v1: SIMD + 並列処理 + ゼロアロケーション（2025-11-16, branch perf/heatmap-renderer）

環境: Windows 11, .NET 8.0.22, 13th Gen Intel Core i9-13900F

| Case | Mean | Alloc | Baseline | 速度改善 | メモリ削減 |
|---|---:|---:|---:|---:|---:|
| ToBitmap Medium None | 40.588 us | 16.13 KB | 44.703 us | **9.2% 高速化** | **55.0% 削減** |
| ToBitmap Medium log | 42.284 us | 12.54 KB | 41.370 us | -2.2% | **61.4% 削減** |
| ToBitmap Medium ln | 44.355 us | 12.26 KB | 118.575 us | **62.6% 高速化** ⭐⭐⭐ | **62.1% 削減** |
| ToBitmap Large None | 122.978 us | 13.1 KB | 179.764 us | **31.6% 高速化** ⭐ | **95.4% 削減** ⭐⭐⭐ |
| ToBitmap XLarge None | 1.647 ms | 20.2 KB | 2.238 ms | **26.4% 高速化** ⭐⭐ | **99.5% 削減** ⭐⭐⭐ |
| PlaneCorrection Medium | 13.104 us | 32.12 KB | 13.192 us | ±0% | ±0% |
| RotateCW Medium | 4.212 us | 32.12 KB | 4.332 us | ±0% | ±0% |

**主要な成果**:
- **XLarge (1024×1024)**: 2.238ms → 1.647ms (26.4%高速化)、メモリ4174KB → 20KB (99.5%削減)
- **Large (256×256)**: 179.764us → 122.978us (31.6%高速化)、メモリ283KB → 13KB (95.4%削減)
- **Medium ln変換**: 118.575us → 44.355us (62.6%高速化、最大の速度改善)

**技術詳細**:
- ゼロアロケーション: `Span<double>`から直接`Vector<double>`を構築
- SIMD処理でstackalloc領域を再利用、`.ToArray()`によるヒープ割り当てを回避
- 結果: 特に大規模データでGCプレッシャーが激減し、速度とメモリの両面で劇的改善


