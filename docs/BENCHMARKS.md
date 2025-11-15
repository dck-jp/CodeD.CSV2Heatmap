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

## 最適化v1: SIMD + 並列処理 + ゼロアロケーション（2025-11-16, commit 35ebeb2）

環境: Windows 11, .NET 8.0.22, 13th Gen Intel Core i9-13900F

**改善内容**: 
- `Vector<double>`によるSIMD演算
- `Parallel.For`によるX軸並列処理
- カラーインデックスバッファの再利用
- unsafeポインタによる直接ピクセルアクセス
- 配列スライスのゼロアロケーション化（stackalloc + Unsafe.Read）

| Case | Mean | Alloc | Baseline | 速度改善 | メモリ削減 |
|---|---:|---:|---:|---:|---:|
| ToBitmap Medium None | 40.588 us | 16.13 KB | 44.703 us | **9.2% 高速化** | **55.0% 削減** |
| ToBitmap Medium log | 42.284 us | 12.54 KB | 41.370 us | -2.2% | **61.4% 削減** |
| ToBitmap Medium ln | 44.355 us | 12.26 KB | 118.575 us | **62.6% 高速化** ⭐⭐⭐ | **62.1% 削減** |
| ToBitmap Large None | 122.978 us | 13.1 KB | 179.764 us | **31.6% 高速化** ⭐ | **95.4% 削減** ⭐⭐⭐ |
| ToBitmap XLarge None | 1.647 ms | 20.2 KB | 2.238 ms | **26.4% 高速化** ⭐⭐ | **99.5% 削減** ⭐⭐⭐ |
| PlaneCorrection Medium | 13.104 us | 32.12 KB | 13.192 us | ±0% | ±0% |
| RotateCW Medium | 4.212 us | 32.12 KB | 4.332 us | ±0% | ±0% |

## 最適化v2: カラーマップキャッシュ化（2025-11-16, branch perf/heatmap-renderer）

環境: Windows 11, .NET 8.0.22, 13th Gen Intel Core i9-13900F

**改善内容**:
- Rainbow/Monochrome/BlackPurpleWhiteカラーマップをstaticフィールドでキャッシュ
- 毎回の765要素SKColor配列生成を回避（約3KB削減/呼び出し）

| Case | Mean | Alloc | v1 | 速度改善 | メモリ削減 | vs Baseline |
|---|---:|---:|---:|---:|---:|---:|
| ToBitmap Medium None | 40.686 us | 13.12 KB | 40.588 us / 16.13 KB | ±0% | **18.7% 削減** | **9.0% 高速化 / 63.4% 削減** |
| ToBitmap Medium log | 38.561 us | 10.34 KB | 42.284 us / 12.54 KB | **8.8% 高速化** ⭐ | **17.5% 削減** | **6.8% 高速化 / 68.2% 削減** |
| ToBitmap Medium ln | 44.819 us | 9.0 KB | 44.355 us / 12.26 KB | -1.0% | **26.6% 削減** | **62.2% 高速化 / 72.2% 削減** ⭐⭐⭐ |
| ToBitmap Large None | 120.049 us | 10.01 KB | 122.978 us / 13.1 KB | **2.4% 高速化** | **23.6% 削減** | **33.2% 高速化 / 96.5% 削減** ⭐⭐⭐ |
| ToBitmap XLarge None | 1.687 ms | 17.24 KB | 1.647 ms / 20.2 KB | -2.4% | **14.6% 削減** | **24.6% 高速化 / 99.6% 削減** ⭐⭐⭐ |
| PlaneCorrection Medium | 12.982 us | 32.12 KB | 13.104 us / 32.12 KB | **0.9% 高速化** | ±0% | **1.6% 高速化 / ±0%** |
| RotateCW Medium | 4.300 us | 32.12 KB | 4.212 us / 32.12 KB | -2.1% | ±0% | **0.7% 高速化 / ±0%** |

**総合成果（Baseline → v2）**:
- **速度**: Medium ln 62.2%、Large 33.2%、XLarge 24.6%高速化
- **メモリ**: XLarge 99.6%、Large 96.5%、Medium 63-72%削減
- **技術**: ゼロアロケーション + カラーマップキャッシュで持続的な低メモリ実現


