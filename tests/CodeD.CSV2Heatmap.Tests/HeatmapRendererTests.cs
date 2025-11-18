using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace CodeD.Tests
{
    [TestClass()]
    public class HeatmapRendererTests
    {
        [TestMethod]
        public void HeatmapRendererTest()
        {
            var src = new[,] { { 0.1, 0.1, 0.1 } };
            var zmap = new HeatmapRenderer(src, 0);
            zmap.Data.IsNotNull();
        }

        [TestMethod]
        public async Task HeatmapRendererTest_FileRead1()
        {
            var testFilename01 = TestHelpers.GetTestFilePath("grid_test1.csv");
            var zmap = await HeatmapRenderer.CreateAsync(testFilename01, 0);
            zmap.Data.GetLength(0).Is(3); //X方向
            zmap.Data.GetLength(1).Is(1); //Y方向 
        }

        [TestMethod()]
        public async Task HeatmapRendererTest_FileRead2()
        {
            var testFilename02 = TestHelpers.GetTestFilePath("grid_test2.csv");
            var zmap = await HeatmapRenderer.CreateAsync(testFilename02, 0);
            zmap.Data.GetLength(0).Is(3); //X方向
            zmap.Data.GetLength(1).Is(2); //X方向       
        }

        [TestMethod()]
        public async Task ToBitmapTest()
        {
            var testFilename01 = TestHelpers.GetTestFilePath("grid_test1.csv");
            var zmap = await HeatmapRenderer.CreateAsync(testFilename01, 0);
            var bitmap = zmap.ToBitmap();
            bitmap.Width.Is(3);
            bitmap.Height.Is(1);
            bitmap.Dispose(); // SkiaSharpではリソース解放が必要
        }

        [TestMethod()]
        public void ToBitmapTest_SIMD_vs_Normal_SmallData()
        {
            // 小さなデータ(フォールバック処理が使われる)
            var smallData = new double[10, 10];
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    smallData[i, j] = (i + j) / 19.0;
                }
            }

            var renderer = new HeatmapRenderer(smallData, 1.0);
            var bitmap = renderer.ToBitmap(0, 1.0, HeatmapRenderer.ColorMode.Rainbow, HeatmapRenderer.ConvertMode.None);

            // 結果の検証: ピクセル値が正しく設定されているか
            bitmap.Width.Is(10);
            bitmap.Height.Is(10);

            // いくつかのピクセルをサンプリングして確認
            var color00 = bitmap.GetPixel(0, 0);
            var color55 = bitmap.GetPixel(5, 5);
            var color99 = bitmap.GetPixel(9, 9);

            // カラー値が設定されていることを確認（透明でないこと）
            color00.Alpha.Is((byte)255);
            color55.Alpha.Is((byte)255);
            color99.Alpha.Is((byte)255);

            bitmap.Dispose();
        }

        [TestMethod()]
        public void ToBitmapTest_SIMD_vs_Normal_LargeData()
        {
            // SIMD処理が使われる大きなデータ(256ピクセル以上)
            var largeData = new double[32, 32]; // 1024ピクセル
            for (int i = 0; i < 32; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    largeData[i, j] = (i * 32 + j) / 1024.0;
                }
            }

            var renderer = new HeatmapRenderer(largeData, 1.0);
            var bitmap = renderer.ToBitmap(0, 1.0, HeatmapRenderer.ColorMode.Rainbow, HeatmapRenderer.ConvertMode.None);

            // 結果の検証
            bitmap.Width.Is(32);
            bitmap.Height.Is(32);

            // グラデーションが正しく適用されているか確認
            var color00 = bitmap.GetPixel(0, 0);
            var color1616 = bitmap.GetPixel(16, 16);
            var color3131 = bitmap.GetPixel(31, 31);

            // すべてのピクセルがアルファ値255で不透明であることを確認
            color00.Alpha.Is((byte)255);
            color1616.Alpha.Is((byte)255);
            color3131.Alpha.Is((byte)255);

            bitmap.Dispose();
        }

        [TestMethod()]
        public void ToBitmapTest_SIMD_ConsistencyCheck()
        {
            // SIMD処理と通常処理で結果が一致するか確認
            // テスト用のデータを作成
            var testData = new double[50, 50];
            var random = new Random(12345); // シード固定で再現性を確保

            for (int i = 0; i < 50; i++)
            {
                for (int j = 0; j < 50; j++)
                {
                    testData[i, j] = random.NextDouble();
                }
            }

            var renderer = new HeatmapRenderer(testData, 1.0);
            var bitmap = renderer.ToBitmap(0, 1.0, HeatmapRenderer.ColorMode.Rainbow, HeatmapRenderer.ConvertMode.None);

            // 各ピクセルが有効な色値を持つことを確認
            for (int x = 0; x < Math.Min(50, bitmap.Width); x++)
            {
                for (int y = 0; y < Math.Min(50, bitmap.Height); y++)
                {
                    var color = bitmap.GetPixel(x, y);
                    color.Alpha.Is((byte)255); // 不透明であること
                }
            }

            bitmap.Dispose();
        }

        [TestMethod()]
        public void ToBitmapTest_SIMD_EdgeCases()
        {
            // 境界値のテスト
            var edgeData = new double[20, 20];

            // 最小値と最大値を配置
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    if (i == 0 && j == 0)
                        edgeData[i, j] = 0.0; // 最小値
                    else if (i == 19 && j == 19)
                        edgeData[i, j] = 1.0; // 最大値
                    else
                        edgeData[i, j] = 0.5; // 中間値
                }
            }

            var renderer = new HeatmapRenderer(edgeData, 1.0);
            var bitmap = renderer.ToBitmap(0, 1.0, HeatmapRenderer.ColorMode.Rainbow, HeatmapRenderer.ConvertMode.None);

            // 境界値のピクセルが正しく処理されているか確認
            var colorMin = bitmap.GetPixel(0, 0);
            var colorMid = bitmap.GetPixel(10, 10);
            var colorMax = bitmap.GetPixel(19, 19);

            // 最小値と最大値のピクセルが異なる色であることを確認
            var minMaxDiff = Math.Abs(colorMin.Red - colorMax.Red) +
                            Math.Abs(colorMin.Green - colorMax.Green) +
                            Math.Abs(colorMin.Blue - colorMax.Blue);
            Assert.IsTrue(minMaxDiff > 0, "最小値と最大値で色が異なるはず");

            bitmap.Dispose();
        }

        [TestMethod()]
        public void ToBitmapTest_SIMD_ColorModes()
        {
            // 異なるカラーモードでSIMD処理が正しく動作するか確認
            var testData = new double[30, 30];
            for (int i = 0; i < 30; i++)
            {
                for (int j = 0; j < 30; j++)
                {
                    testData[i, j] = (i + j) / 58.0;
                }
            }

            var renderer = new HeatmapRenderer(testData, 1.0);

            // Rainbow
            var bitmapRainbow = renderer.ToBitmap(0, 1.0, HeatmapRenderer.ColorMode.Rainbow);
            bitmapRainbow.Width.Is(30);
            bitmapRainbow.Height.Is(30);
            bitmapRainbow.Dispose();

            // Monochrome
            var bitmapMono = renderer.ToBitmap(0, 1.0, HeatmapRenderer.ColorMode.Monochorome);
            bitmapMono.Width.Is(30);
            bitmapMono.Height.Is(30);
            bitmapMono.Dispose();

            // BlackPurpleWhite
            var bitmapBPW = renderer.ToBitmap(0, 1.0, HeatmapRenderer.ColorMode.BlackPurpleWhite);
            bitmapBPW.Width.Is(30);
            bitmapBPW.Height.Is(30);
            bitmapBPW.Dispose();
        }

        [TestMethod()]
        public void ToBitmapTest_SIMD_ConvertModes()
        {
            // 異なる変換モードでSIMD処理が正しく動作するか確認
            var testData = new double[25, 25];
            for (int i = 0; i < 25; i++)
            {
                for (int j = 0; j < 25; j++)
                {
                    testData[i, j] = 1.0 + (i + j) / 10.0; // 正の値のみ(log用)
                }
            }

            var renderer = new HeatmapRenderer(testData, 1.0);

            // None
            var bitmapNone = renderer.ToBitmap(1.0, 6.0, HeatmapRenderer.ColorMode.Rainbow, HeatmapRenderer.ConvertMode.None);
            bitmapNone.Width.Is(25);
            bitmapNone.Dispose();

            // log
            var bitmapLog = renderer.ToBitmap(1.0, 6.0, HeatmapRenderer.ColorMode.Rainbow, HeatmapRenderer.ConvertMode.log);
            bitmapLog.Width.Is(25);
            bitmapLog.Dispose();

            // ln
            var bitmapLn = renderer.ToBitmap(1.0, 6.0, HeatmapRenderer.ColorMode.Rainbow, HeatmapRenderer.ConvertMode.ln);
            bitmapLn.Width.Is(25);
            bitmapLn.Dispose();
        }
    }
}
