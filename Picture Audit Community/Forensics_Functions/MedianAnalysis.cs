using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Picture_Audit_Community
{
    public static class MedianAnalysis
    {
        /// <summary>
        /// Performs Median Residual Analysis.
        /// Highlights noise inconsistencies by comparing an image to its median-blurred version.
        /// </summary>
        /// <param name="inputPath">Path to the suspect image.</param>
        /// <param name="outputPath">Path to save the resulting noise map.</param>
        /// <param name="radius">The radius for the median blur (default 1 for 3x3 window).</param>
        public static void GenerateMedianAnalysis(string inputPath, string outputPath, int radius = 1)
        {
            // 1. Load the original image
            using (Image<Rgba32> original = Image.Load<Rgba32>(inputPath))
            {
                // 2. Create the median filtered version
                // We use preserveAlpha: true to ensure we only analyze color noise
                using (Image<Rgba32> filtered = original.Clone(x => x.MedianBlur(radius, true)))
                {
                    using (Image<Rgba32> medianMap = new Image<Rgba32>(original.Width, original.Height))
                    {
                        // 3. Process the differences
                        original.ProcessPixelRows(filtered, medianMap, (accessorOrg, accessorFil, accessorMed) =>
                        {
                            for (int y = 0; y < accessorOrg.Height; y++)
                            {
                                Span<Rgba32> orgRow = accessorOrg.GetRowSpan(y);
                                Span<Rgba32> filRow = accessorFil.GetRowSpan(y);
                                Span<Rgba32> medRow = accessorMed.GetRowSpan(y);

                                for (int x = 0; x < orgRow.Length; x++)
                                {
                                    // Get absolute difference for R, G, B
                                    int rDiff = Math.Abs(orgRow[x].R - filRow[x].R);
                                    int gDiff = Math.Abs(orgRow[x].G - filRow[x].G);
                                    int bDiff = Math.Abs(orgRow[x].B - filRow[x].B);

                                    // Scale the difference for visibility
                                    const int scale = 40;

                                    medRow[x] = new Rgba32(
                                        (byte)Math.Min(255, rDiff * scale),
                                        (byte)Math.Min(255, gDiff * scale),
                                        (byte)Math.Min(255, bDiff * scale),
                                        255 // Keep result fully opaque
                                    );
                                }
                            }
                        });

                        // 4. Save as PNG to keep the noise data intact
                        medianMap.SaveAsPng(outputPath);
                    }
                }
            }
        }
        /// <summary>
        /// Runs Median Analysis through multiple radii (1-5) and saves the best 
        /// result (most descriptive noise map) to the outputFile.
        /// </summary>
        public static void GetBestMedianAnalysisPath(string inputPath, string outputFile)
        {
            // Create temporary folder for iterations
            string outputFolder = Path.Combine(Path.GetDirectoryName(outputFile), "Median_Analysis_Temp");

            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            string bestTempPath = string.Empty;
            double maxIntensity = double.MinValue;

            // Iterate through radii 1 to 5 (Median analysis gets very blurry beyond 5)
            for (int r = 1; r <= 5; r++)
            {
                string currentOutputPath = Path.Combine(outputFolder, $"median_r{r}.png");

                GenerateMedianAnalysis(inputPath, currentOutputPath, r);

                // Calculate intensity to find the most "active" noise map
                double averageIntensity = CalculateAverageIntensity(currentOutputPath);

           

                if (averageIntensity > maxIntensity)
                {
                    maxIntensity = averageIntensity;
                    bestTempPath = currentOutputPath;
                }
            }

            // Copy the best variation to the final destination
            if (!string.IsNullOrEmpty(bestTempPath) && File.Exists(bestTempPath))
            {
                File.Copy(bestTempPath, outputFile, true);
            }
        }

        private static double CalculateAverageIntensity(string imagePath)
        {
            double totalIntensity = 0;
            using (Image<L8> img = Image.Load<L8>(imagePath))
            {
                img.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        Span<L8> row = accessor.GetRowSpan(y);
                        for (int x = 0; x < row.Length; x++)
                        {
                            totalIntensity += row[x].PackedValue;
                        }
                    }
                });
                return totalIntensity / (img.Width * img.Height);
            }
        }
    }
}