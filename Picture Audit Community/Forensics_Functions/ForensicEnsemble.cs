using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Picture_Audit_Community.Forensics_Functions
{
    public static class ForensicEnsemble
    {
        public static void GenerateConsensusMap(List<string> analysisPaths, string outputPath)
        {
            if (analysisPaths == null || analysisPaths.Count == 0) return;
            //Load Image
            using (Image<Rgba32> first = Image.Load<Rgba32>(analysisPaths[0]))
            {
                int width = first.Width;
                int height = first.Height;
                long[,] intensitySum = new long[width, height];
                int validImageCount = 0;

                // Accumulate pixel values
                foreach (string path in analysisPaths)
                {
                    if (!File.Exists(path)) continue;
                    using (Image<Rgba32> img = Image.Load<Rgba32>(path))
                    {
                        if (img.Width != width || img.Height != height) continue;
                        validImageCount++;
                        img.ProcessPixelRows(accessor =>
                        {
                            for (int y = 0; y < height; y++)
                            {
                                Span<Rgba32> row = accessor.GetRowSpan(y);
                                for (int x = 0; x < width; x++)
                                {
                                    intensitySum[x, y] += (row[x].R + row[x].G + row[x].B) / 3;
                                }
                            }
                        });
                    }
                }

                if (validImageCount == 0) return;

                // Calculate the "Average Map" and a Histogram to find the Auto-Floor
                int[] finalAverages = new int[width * height];
                int[] histogram = new int[256];

                int i = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int avg = (int)(intensitySum[x, y] / validImageCount);
                        finalAverages[i++] = avg;
                        histogram[avg]++;
                    }
                }

                // Adaptive Sensitivity Logic (Binary Search style)
                // find a noise floor where only ~2% of the image is "High Intensity"
                int autoNoiseFloor = CalculateAutoFloor(histogram, width * height, 0.02);

                // Generate Final Image using the adaptive floor
                using (Image<L8> result = new Image<L8>(width, height))
                {
                    int pixelIdx = 0;
                    result.ProcessPixelRows(accessor =>
                    {
                        for (int y = 0; y < height; y++)
                        {
                            Span<L8> row = accessor.GetRowSpan(y);
                            for (int x = 0; x < width; x++)
                            {
                                int val = finalAverages[pixelIdx++];

                                // Subtract adaptive floor
                                int processed = Math.Max(0, val - autoNoiseFloor);

                                // Boost the anomaly visibility
                                processed = Math.Min(255, processed * 4);

                                row[x] = new L8((byte)processed);
                            }
                        }
                    });

                    result.SaveAsPng(outputPath);
                }
            }
        }
        public static void GenerateConsensusMapWTA(List<string> analysisPaths, string outputPath)
        {
            if (analysisPaths == null || analysisPaths.Count == 0) return;

            // --- STEP 1: CHOOSE ONE ---
            // analyze all paths to find the single one with the most "tampered" pixels (Red)
            string winningPath = analysisPaths[0];
            int maxRedCount = -1;

            foreach (string path in analysisPaths)
            {
                if (!File.Exists(path)) continue;

                using (Image<Rgba32> img = Image.Load<Rgba32>(path))
                {
                    int redCount = 0;
                    img.ProcessPixelRows(accessor =>
                    {
                        for (int y = 0; y < accessor.Height; y++)
                        {
                            var row = accessor.GetRowSpan(y);
                            for (int x = 0; x < row.Length; x++)
                            {
                                // Intensity > 40 is our threshold for "Actual Tampering"
                                if (row[x].R > 40) redCount++;
                            }
                        }
                    });

                    if (redCount > maxRedCount)
                    {
                        maxRedCount = redCount;
                        winningPath = path;
                    }
                }
            }

            // --- STEP 2: RUN INITIAL FUNCTION WITH ONLY ONE INPUT ---
            // Now we process ONLY the winning path using your established logic
            using (Image<Rgba32> winningImage = Image.Load<Rgba32>(winningPath))
            {
                int width = winningImage.Width;
                int height = winningImage.Height;

                // Create a histogram of the winner to find its specific noise floor
                int[] histogram = new int[256];
                winningImage.ProcessPixelRows(accessor => {
                    for (int y = 0; y < height; y++)
                    {
                        foreach (var p in accessor.GetRowSpan(y)) histogram[p.R]++;
                    }
                });

                // Calculate the adaptive floor for this specific algorithm
                int autoNoiseFloor = CalculateAutoFloor(histogram, width * height, 0.02);

                using (Image<L8> result = new Image<L8>(width, height))
                {
                    // Transfer the winning data to the final mask
                    winningImage.ProcessPixelRows(result, (sourceAcc, destAcc) =>
                    {
                        for (int y = 0; y < height; y++)
                        {
                            var sourceRow = sourceAcc.GetRowSpan(y);
                            var destRow = destAcc.GetRowSpan(y);

                            for (int x = 0; x < width; x++)
                            {
                                int val = sourceRow[x].R;

                                // Subtract noise floor
                                int processed = Math.Max(0, val - autoNoiseFloor);

                                // Boost visibility (x4 as in your initial function)
                                processed = Math.Min(255, processed * 4);

                                destRow[x] = new L8((byte)processed);
                            }
                        }
                    });

                    result.SaveAsPng(outputPath);
                }
            }
        }
        private static int CalculateAutoFloor(int[] histogram, int totalPixels, double targetRatio)
        {
            // We look from the brightest pixels down until we hit our target ratio
            long pixelCount = 0;
            long thresholdCount = (long)(totalPixels * targetRatio);

            for (int intensity = 255; intensity >= 0; intensity--)
            {
                pixelCount += histogram[intensity];
                if (pixelCount >= thresholdCount)
                {
                    // This intensity level represents the point where 2% of the image is brighter.
                    // We set our floor slightly below this to capture the anomaly clearly.
                    return Math.Max(5, intensity - 10);
                }
            }
            return 10; // Fallback
        }
    }
}