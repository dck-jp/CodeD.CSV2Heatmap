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


