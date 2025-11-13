# CodeD.CSV2Heatmap

日本語 | [English](README.md)

2Dマップデータ（CSV形式）からヒートマップ画像を生成する.NETライブラリです。表面粗さ測定、X線集光強度分布、点群データなど、科学計測データの可視化に最適です。

**対応データ形式:**
- **Grid Data** (Height Map / Surface Map / Intensity Map / Raster Map)
- **Point Cloud Data** (XYZ Format / Scatter Data / Tabular XYZ Data)

グリッド形式およびXYZ形式のデータをサポートし、高速な並列処理とSIMD最適化により効率的に画像を生成します。

[![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-blue.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

## 特徴

- 📊 **複数のデータ形式をサポート**: Grid Data形式とPoint Cloud Data（XYZ形式）の両方に対応
- 🎨 **多彩なカラーモード**: レインボー、モノクロ、ブラック-パープル-ホワイト
- 🚀 **高速処理**: 並列処理とSIMD最適化による高速なビットマップ生成
- 🔧 **画像処理機能**: 平面補正、回転、トリミングなどの基本的な画像処理
- 📈 **データ変換**: 線形、対数、自然対数スケールでのデータ変換
- 🔌 **SkiaSharp基盤**: クロスプラットフォーム対応のグラフィックスライブラリを使用

## 想定される用途

- 表面粗さ測定データ (Surface Map / Height Map)
- X線集光強度分布 (Intensity Map / Raster Map)
- 温度分布・濃度分布の可視化
- 点群データ (Point Cloud Data) の2D投影
- 散布データ (Scattered Data) のグリッド化と可視化
- その他の2Dマップデータ (Grid Data / Raster Data)

## インストール

### NuGetパッケージ（予定）

```bash
dotnet add package CodeD.CSV2Heatmap
```

### 手動ビルド

```bash
git clone https://github.com/dck-jp/CodeD.CSV2Heatmap.git
cd CodeD.CSV2Heatmap
dotnet build src/CodeD.CSV2Heatmap/CodeD.CSV2Heatmap.csproj -c Release
```

## 必要要件

- .NET Standard 2.0 以上
- SkiaSharp 2.88.8 以上

## 使い方

### Grid Data形式のファイルからヒートマップを生成

Grid Data（GridCSV形式、2D Map Data、Height Map、Surface Map、Intensity Mapなど）から画像を生成します。

```csharp
using CodeD;
using SkiaSharp;

// GridCSVファイルからヒートマップを生成
var heatmap = new HeatmapRenderer("grid_sample.txt");

// ビットマップに変換
SKBitmap bitmap = heatmap.ToBitmap(
    colorMode: HeatmapRenderer.ColorMode.Rainbow,
    convertMode: HeatmapRenderer.ConvertMode.None
);

// 画像として保存
using (var image = SKImage.FromBitmap(bitmap))
using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
using (var stream = File.OpenWrite("output.png"))
{
    data.SaveTo(stream);
}
```

### Point Cloud Data形式のファイルを処理

Point Cloud Data（XYZ Format、Scatter Data、Tabular XYZ Dataなど）をグリッドデータに変換して処理します。

```csharp
using CodeD;

// XYZファイルを解析（3列目をZ値として使用）
var parser = new XyzCsvParser("xyz_sample.txt", zColNum: 3);

// グリッドデータに変換
double[,] gridData = parser.Data;

// ヒートマップレンダラーを作成
var heatmap = new HeatmapRenderer(gridData);

// ビットマップに変換
var bitmap = heatmap.ToBitmap();
```

### カラーモードの選択

```csharp
// レインボーカラー（青→緑→赤）
var bitmap1 = heatmap.ToBitmap(colorMode: HeatmapRenderer.ColorMode.Rainbow);

// モノクロ（白黒）
var bitmap2 = heatmap.ToBitmap(colorMode: HeatmapRenderer.ColorMode.Monochorome);

// ブラック→パープル→ホワイト
var bitmap3 = heatmap.ToBitmap(colorMode: HeatmapRenderer.ColorMode.BlackPurpleWhite);
```

### データ変換モード

```csharp
// 線形スケール（デフォルト）
var bitmap1 = heatmap.ToBitmap(convertMode: HeatmapRenderer.ConvertMode.None);

// 常用対数変換
var bitmap2 = heatmap.ToBitmap(convertMode: HeatmapRenderer.ConvertMode.log);

// 自然対数変換
var bitmap3 = heatmap.ToBitmap(convertMode: HeatmapRenderer.ConvertMode.ln);
```

### 画像処理機能

```csharp
// 平面補正（ピクセルサイズの指定が必要）
var heatmap = new HeatmapRenderer("data.txt", pixelSize: 1.0);
var corrected = heatmap.GetPlaneCorrection();

// トリミング（x0, y0, width, height）
var trimmed = heatmap.GetTrim(10, 10, 50, 50);

// 90度回転（時計回り）
var rotatedCW = heatmap.GetRotateCW();

// 90度回転（反時計回り）
var rotatedCCW = heatmap.GetRotateCCW();

// 任意角度回転（ラジアン指定）
var rotated45 = heatmap.GetRotate(Math.PI / 4);
```

### データへのアクセス

```csharp
// データサイズの取得
int xSize = heatmap.XSize;
int ySize = heatmap.YSize;

// 最大値・最小値の取得
double max = heatmap.Max;
double min = heatmap.Min;

// 特定の行・列のデータを取得
double[] rowData = heatmap.GetRowData(0);
double[] columnData = heatmap.GetColumnData(0);

// 2次元配列に直接アクセス
double value = heatmap.Data[x, y];

// ファイルに保存
heatmap.SaveAs("output.txt");
```

## サポートされるファイル形式

### Grid Data形式（GridCSV / 2D Map Data）

グリッド状に配置された数値データを表すフォーマットです。表面粗さ測定（Height Map / Surface Map）、X線集光強度（Intensity Map）、ラスターデータ（Raster Map / Raster Data）などで広く使われています。

```
# ヘッダー行（オプション）
1.0	2.0	3.0	4.0
2.5	3.5	4.5	5.5
3.0	4.0	5.0	6.0
```

- **区切り文字**: タブ、カンマ、スペース
- **ヘッダー**: 文字列を含む行は自動的にヘッダーとして認識
- **データ**: 数値のみの行

### Point Cloud Data形式（XYZ Format）

X座標、Y座標、Z値を持つ点群データです。散布データ（Scatter Data / Scattered Data）、テーブル形式のXYZデータ（Tabular XYZ Data）、多値グリッド点（Multi-Value Grid Points）として、様々な科学計測で使用されます。

```
X	Y	Z1	Z2	Z3
0	0	10.5	20.0	30.5
1	0	11.0	21.5	31.0
0	1	12.0	22.0	32.0
```

- **区切り文字**: タブ、カンマ、スペース
- **カラム**: X座標、Y座標、Z値（複数列可能）
- **変換**: 自動的にグリッド形式に変換されます

**注意**: XYZ形式のデータは、等間隔のグリッド上に配置されたポイントを想定しています。不規則な配置の点群データの場合、適切に補間されない可能性があります。

## パフォーマンス

このライブラリは以下の最適化技術を使用しています：

- **並列処理**: マルチコアCPUを活用した並列計算
- **SIMD最適化**: Vector<T>を使用した高速演算
- **非同期処理**: ファイルI/Oの非同期化
- **メモリ効率**: 直接ピクセルアクセスによる高速化

## サンプル

`samples/`ディレクトリに以下のサンプルファイルがあります：

- `grid_sample_simple.txt` - シンプルな5x5グリッドデータ（Grid Data形式）
- `grid_sample_temperature.csv` - 温度分布の8x6グリッド（2D Map Data形式）
- `xyz_sample.txt` - XYZ座標の点群データ（Point Cloud Data形式）
- `xyz_sample2.txt` - 追加のXYZサンプル（Scatter Data形式）

詳細な使用例は`samples/README.md`を参照してください。

## テスト

```bash
cd tests/CodeD.CSV2Heatmap.Tests
dotnet test
```

テストには以下が含まれます：

- GridCsvParserの単体テスト
- XyzCsvParserの単体テスト
- HeatmapRendererの単体テスト
- 各種ファイル形式のパーステスト

## ライセンス

このプロジェクトはMITライセンスの下で公開されています。詳細は[LICENSE](LICENSE)ファイルを参照してください。

## 貢献

プルリクエストを歓迎します！大きな変更の場合は、まずissueを開いて変更内容を議論してください。

1. このリポジトリをフォーク
2. フィーチャーブランチを作成 (`git checkout -b feature/amazing-feature`)
3. 変更をコミット (`git commit -m 'Add some amazing feature'`)
4. ブランチにプッシュ (`git push origin feature/amazing-feature`)
5. プルリクエストを作成

## 作者

**dck-jp**

## 関連リンク

- [SkiaSharp](https://github.com/mono/SkiaSharp) - クロスプラットフォームな2Dグラフィックスライブラリ
- [.NET Standard](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) - .NET実装間の共通API

## 変更履歴

### Version 1.0.0

- 初回リリース
- GridCSV形式のサポート
- XYZ形式のサポート
- 複数のカラーモード実装
- SIMD最適化による高速化
- 基本的な画像処理機能

---

⭐ このプロジェクトが役に立った場合は、スターをつけていただけると嬉しいです！
