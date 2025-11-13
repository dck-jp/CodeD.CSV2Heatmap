# CodeD.CSV2Heatmap ライブラリ変更に伴うasterismでの修正ガイド

## 変更概要

CodeD.CSV2Heatmapライブラリのクラス名とファイル名が統一性向上のために以下のように変更されました。さらに、各パーサークラスの使用方法も統一され、より一貫性のあるAPIとなりました。

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

## 重要な変更: パーサーAPIの統一

`XyzCsvParser`と`GridCsvParser`のAPIが統一され、より一貫性のある使い方になりました。

### 主な変更点

1. **プロパティアクセス**: `ToArray()`メソッドから`Data`プロパティへの移行
2. **非同期サポート**: `CreateAsync()`メソッドの追加
3. **追加プロパティ**: `XSize`, `YSize`, `Max`, `Min`プロパティの追加

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
var array2D = xyzParser.Data;  // ToArray() → Data プロパティに変更
string header = xyzParser.Header;

// さらに利用可能になった新しいプロパティ
int xSize = xyzParser.XSize;
int ySize = xyzParser.YSize;
double max = xyzParser.Max;
double min = xyzParser.Min;
```

#### 非同期版の使用（推奨）
```csharp
// 新コード - 非同期版（推奨）
var xyzParser = await XyzCsvParser.CreateAsync(filePath, zColumnIndex);
var array2D = xyzParser.Data;
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

// 新コード（使い方は同じ）
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

## パーサークラスのAPI統一による利点

### 統一された使用パターン

すべてのパーサークラスで、以下のパターンが利用可能になりました：

```csharp
// パターン1: 同期的な読み込み
var parser = new XyzCsvParser(filePath, zColNum);  // または new GridCsvParser(filePath)
var data = parser.Data;
var header = parser.Header;

// パターン2: 非同期的な読み込み（大きなファイルに推奨）
var parser = await XyzCsvParser.CreateAsync(filePath, zColNum);  // または GridCsvParser.CreateAsync(filePath)
var data = parser.Data;
var header = parser.Header;

// 共通で利用可能なプロパティ
int xSize = parser.XSize;   // X方向のサイズ
int ySize = parser.YSize;   // Y方向のサイズ
double max = parser.Max;    // データの最大値
double min = parser.Min;    // データの最小値
```

### 統一による主な改善点

1. **学習コストの削減**: 一度使い方を覚えれば、すべてのパーサーで同じパターンが使える
2. **コードの可読性向上**: 統一されたAPIにより、コードが理解しやすくなる
3. **パフォーマンス**: 非同期版の使用により、大きなファイルでもUIをブロックしない
4. **情報へのアクセス**: すべてのパーサーで統計情報（サイズ、最大値、最小値）が利用可能

## 機能の変更点

- **APIの統一**: パーサークラスのメソッドとプロパティが統一されました
- **機能自体に変更はありません**: すべてのメソッド、機能は従来と同じです
- **パフォーマンス特性も変更ありません**: 内部処理の最適化はそのまま維持されています

## 互換性

- **破壊的変更**: 
  - クラス名が変更されているため、コードの修正が必要です
  - `XyzCsvParser`の`ToArray()`メソッドは`Data`プロパティに変更されています
- **NuGet パッケージ**: パッケージ名やバージョンに変更がある場合は別途通知します

## 修正手順

1. asterismプロジェクトで CodeD.CSV2Heatmap を最新バージョンに更新
2. コンパイルエラーが出る箇所を特定
3. 上記ガイドに従ってクラス名を変更
4. `XyzCsvParser`を使用している箇所で`ToArray()`を`Data`に変更
5. 可能であれば、大きなファイルを扱う箇所で`CreateAsync()`の使用を検討
6. ビルドしてエラーがないことを確認
7. テストを実行して機能が正常に動作することを確認

## よくある質問

### Q: なぜ`ToArray()`が`Data`プロパティに変わったのですか？

A: `GridCsvParser`との一貫性を保つためです。また、データは既にパース時に計算されているため、プロパティとしてアクセスする方が直感的です。

### Q: 同期版と非同期版、どちらを使うべきですか？

A: 小さなファイルや、UIをブロックしても問題ない場合は同期版で十分です。大きなファイルやUIアプリケーションでは、非同期版の使用を推奨します。

### Q: 既存のコードが動かなくなりますか？

A: はい、破壊的変更のため、クラス名の変更と`XyzCsvParser`での`ToArray()`から`Data`への変更が必要です。ただし、このガイドに従えば、すべての修正を簡単に行えます。

## 質問・サポート

修正作業で不明な点があれば、CodeD.CSV2HeatmapのリポジトリでIssueを作成してください。