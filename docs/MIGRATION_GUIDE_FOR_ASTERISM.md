# CodeD.CSV2Heatmap ライブラリ変更に伴うasterismでの修正ガイド

## 変更概要

CodeD.CSV2Heatmapライブラリのクラス名とファイル名が統一性向上のために以下のように変更されました：

### クラス名の変更

| 旧クラス名 | 新クラス名 | 機能 |
|------------|------------|------|
| `XYZData` | `XyzCsvParser` | XYZ座標形式CSVファイルのパース |
| `ZMapParser` | `GridCsvParser` | 2次元グリッド形式CSVファイルのパース |
| `ZMappingData` | `HeatmapRenderer` | 2次元データの画像化・可視化 |

### ファイル名の変更

| 旧ファイル名 | 新ファイル名 |
|-------------|-------------|
| `XYZData.cs` | `XyzCsvParser.cs` |
| `ZMapParser.cs` | `GridCsvParser.cs` |
| `ZMappingData.cs` | `HeatmapRenderer.cs` |

## asterismでの必要な修正

### 1. using文の修正
```csharp
// 変更不要（名前空間 CodeD は同じ）
using CodeD;
```

### 2. クラス名の変更

#### XYZData → XyzCsvParser
```csharp
// 旧コード
var xyzData = new XYZData(filePath, zColumnIndex);
var array2D = xyzData.ToArray();
string header = xyzData.Header;

// 新コード
var xyzParser = new XyzCsvParser(filePath, zColumnIndex);
var array2D = xyzParser.ToArray();
string header = xyzParser.Header;
```

#### ZMapParser → GridCsvParser
```csharp
// 旧コード
var parser = new ZMapParser(filePath);
// または
var parser = await ZMapParser.CreateAsync(filePath);
var data = parser.Data;
var header = parser.Header;

// 新コード
var parser = new GridCsvParser(filePath);
// または
var parser = await GridCsvParser.CreateAsync(filePath);
var data = parser.Data;
var header = parser.Header;
```

#### ZMappingData → HeatmapRenderer
```csharp
// 旧コード - データから直接作成
var renderer = new ZMappingData(data2D, pixelSize, header);
var bitmap = renderer.ToBitmap();

// 新コード - データから直接作成
var renderer = new HeatmapRenderer(data2D, pixelSize, header);
var bitmap = renderer.ToBitmap();

// 旧コード - ファイルから作成
var renderer = new ZMappingData(filePath, pixelSize);
// または
var renderer = await ZMappingData.CreateAsync(filePath, pixelSize);

// 新コード - ファイルから作成
var renderer = new HeatmapRenderer(filePath, pixelSize);
// または
var renderer = await HeatmapRenderer.CreateAsync(filePath, pixelSize);
```

### 3. メソッド戻り値型の変更

画像処理メソッドの戻り値型も変更されています：

```csharp
// 旧コード
ZMappingData rotated = renderer.GetRotateCW();
ZMappingData trimmed = renderer.GetTrim(x, y, width, height);
ZMappingData corrected = renderer.GetPlaneCorrection();

// 新コード
HeatmapRenderer rotated = renderer.GetRotateCW();
HeatmapRenderer trimmed = renderer.GetTrim(x, y, width, height);
HeatmapRenderer corrected = renderer.GetPlaneCorrection();
```

### 4. 変数名の推奨変更

読みやすさのため、以下のような変数名変更を推奨します：

```csharp
// 旧コード
var xyzData = new XYZData(...);
var zmapParser = new ZMapParser(...);
var zmappingData = new ZMappingData(...);

// 新コード（推奨）
var xyzParser = new XyzCsvParser(...);
var gridParser = new GridCsvParser(...);
var heatmapRenderer = new HeatmapRenderer(...);
```

## 機能の変更点

- **機能自体に変更はありません**
- すべてのメソッド、プロパティ、機能は従来と同じです
- パフォーマンス特性も変更ありません

## 互換性

- **破壊的変更**: クラス名が変更されているため、コードの修正が必要です
- **NuGet パッケージ**: パッケージ名やバージョンに変更がある場合は別途通知します

## 修正手順

1. asterismプロジェクトで CodeD.CSV2Heatmap を最新バージョンに更新
2. コンパイルエラーが出る箇所を特定
3. 上記ガイドに従ってクラス名を変更
4. ビルドしてエラーがないことを確認
5. テストを実行して機能が正常に動作することを確認

## 質問・サポート

修正作業で不明な点があれば、CodeD.CSV2HeatmapのリポジトリでIssueを作成してください。