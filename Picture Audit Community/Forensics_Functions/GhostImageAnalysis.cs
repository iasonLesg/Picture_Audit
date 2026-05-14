using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Picture_Audit_Community
{
    public static class GhostImageAnalysis
    {
        /// <summary>
        /// Generates a JPEG Ghost analysis. 
        /// This compares the image against a specific quality level to find regions 
        /// that were originally saved at that quality.
        /// </summary>
        /// <param name="inputPath">Path to the suspect image.</param>
        /// <param name="outputPath">Path to save the resulting ghost map.</param>
        /// <param name="targetQuality">The quality level to test (0-100).</param>
        public static void GenerateGhostAnalysis(string inputPath, string outputPath, int targetQuality)
        {
            // 1. Load image
            using (Image<Rgba32> original = Image.Load<Rgba32>(inputPath))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    //Re-compress the image at the SPECIFIC target quality
                    JpegEncoder encoder = new JpegEncoder { Quality = targetQuality };
                    original.SaveAsJpeg(ms, encoder);
                    ms.Seek(0, SeekOrigin.Begin);

                    //Load the re-compressed version
                    using (Image<Rgba32> resaved = Image.Load<Rgba32>(ms))
                    {
                        using (Image<Rgba32> ghostMap = new Image<Rgba32>(original.Width, original.Height))
                        {
                            //Ghost Algorithm calculation:
                            // The "Ghost" is the squared difference between the two images, 
                            // often averaged across a small block or smoothed.
                            original.ProcessPixelRows(resaved, ghostMap, (accessorOrg, accessorRes, accessorGhost) =>
                            {
                                for (int y = 0; y < accessorOrg.Height; y++)
                                {
                                    Span<Rgba32> orgRow = accessorOrg.GetRowSpan(y);
                                    Span<Rgba32> resRow = accessorRes.GetRowSpan(y);
                                    Span<Rgba32> ghostRow = accessorGhost.GetRowSpan(y);

                                    for (int x = 0; x < orgRow.Length; x++)
                                    {
                                        // Calculate the difference for each channel
                                        int rDiff = orgRow[x].R - resRow[x].R;
                                        int gDiff = orgRow[x].G - resRow[x].G;
                                        int bDiff = orgRow[x].B - resRow[x].B;

                                        // We use the Squared Difference (normalized) 
                                        // which is the standard for JPEG Ghost detection
                                        int avgDiff = (rDiff * rDiff + gDiff * gDiff + bDiff * bDiff) / 3;

                                        // Amplify the result so it's visible. 
                                        // Standard ghost analysis uses a dynamic range, but a scale of 10-15 is common.
                                        byte component = (byte)Math.Min(255, avgDiff * 10);

                                        ghostRow[x] = new Rgba32(component, component, component, 255);
                                    }
                                }
                            });

                            // 5. Optional: Apply a Box Blur to the result
                            // This helps in visualizing "Ghost" regions by smoothing pixel noise.
                            ghostMap.Mutate(ctx => ctx.BoxBlur(2));

                            // 6. Save as PNG
                            ghostMap.SaveAsPng(outputPath);
                        }
                    }
                }
            }
        }



        /// <summary>
        /// Runs the analysis from Quality 40 to 100 (step 5) and returns the full path 
        /// of the file that represents the best "blackout" integration.
        /// </summary>
        public static void GetBestGhostAnalysisPath(string inputPath, string outputFile)
        {
            // Create a temporary directory based on the output file name 
            // to store the 40-100 quality variations
            string outputFolder = Path.Combine(Path.GetDirectoryName(outputFile), "Ghost_Analysis_Temp");

            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            string bestTempPath = string.Empty;
            double lowestIntensity = double.MaxValue;

            // 1. Iterate through quality levels in steps of 5
            for (int q = 40; q <= 100; q += 5)
            {
                string currentOutputPath = Path.Combine(outputFolder, $"ghost_q{q}.png");

                // Generate the analysis using your existing method
                GenerateGhostAnalysis(inputPath, currentOutputPath, q);

                // 2. Calculate the "blackout" level (Average Intensity)
                double averageIntensity = CalculateAverageIntensity(currentOutputPath);

                // 3. The "best" ghost is the one with the least difference (darkest image)
                if (averageIntensity < lowestIntensity)
                {
                    lowestIntensity = averageIntensity;
                    bestTempPath = currentOutputPath;
                }
            }

            // 4. Save the "Best" result to the final outputFile path
            if (!string.IsNullOrEmpty(bestTempPath) && File.Exists(bestTempPath))
            {
                // Overwrite if it already exists
                File.Copy(bestTempPath, outputFile, true);
            }

        }

        /// <summary>
        /// Helper to determine how "black" the ghost map is.
        /// Lower values indicate a better quality match.
        /// </summary>
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