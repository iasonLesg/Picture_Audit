using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace Picture_Audit_Community
{
    public static class ErrorLevelAnalysis
    {
        public static void GenerateErrorLevelAnalysis(string inputPath, string outputPath, int quality = 95)
        {
            /// <summary>
            /// Loads the iamge and then makes it in a lower definition
            /// Creates an image copression board on top of it
           
            /// </summary>
            //  Load  image
            using (Image<Rgba32> original = Image.Load<Rgba32>(inputPath))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    // Re-compress the image to the baseline quality
                    JpegEncoder encoder = new JpegEncoder { Quality = quality };
                    original.SaveAsJpeg(ms, encoder);
                    ms.Seek(0, SeekOrigin.Begin);

                    // Load the re-compressed version
                    using (Image<Rgba32> resaved = Image.Load<Rgba32>(ms))
                    {
                        // Create a blank image for the result
                        using (Image<Rgba32> elaImage = new Image<Rgba32>(original.Width, original.Height))
                        {
                            // Use ProcessPixelRows to compare multiple images simultaneously
                            original.ProcessPixelRows(resaved, elaImage, (accessorOriginal, accessorResaved, accessorEla) =>
                            {
                                for (int y = 0; y < accessorOriginal.Height; y++)
                                {
                                    // Get spans for the current row across all three images
                                    Span<Rgba32> orgRow = accessorOriginal.GetRowSpan(y);
                                    Span<Rgba32> resRow = accessorResaved.GetRowSpan(y);
                                    Span<Rgba32> elaRow = accessorEla.GetRowSpan(y);

                                    for (int x = 0; x < orgRow.Length; x++)
                                    {
                                       
                                        const int scale = 20;
                                        const int brightnessBoost = 2; // Optional: Multiply the final result to see faint grids

                                        int rDiff = Math.Abs(orgRow[x].R - resRow[x].R) * scale * brightnessBoost;
                                        int gDiff = Math.Abs(orgRow[x].G - resRow[x].G) * scale * brightnessBoost;
                                        int bDiff = Math.Abs(orgRow[x].B - resRow[x].B) * scale * brightnessBoost;

                                        elaRow[x] = new Rgba32(
                                            (byte)Math.Clamp(rDiff, 0, 255),
                                            (byte)Math.Clamp(gDiff, 0, 255),
                                            (byte)Math.Clamp(bDiff, 0, 255),
                                            255
                                        );
                                    }
                                }
                            });

                            //Save as Image
                            elaImage.SaveAsPng(outputPath);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Runs ELA from Quality 70 to 100 and saves the most descriptive version to the outputFile.
        /// </summary>
        public static void GetBestELAAnalysisPath(string inputPath, string outputFile)
        {
            // Create a temporary directory for iterations
            string outputFolder = Path.Combine(Path.GetDirectoryName(outputFile), "ELA_Analysis_Temp");

            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            string bestTempPath = string.Empty;
            double maxIntensity = double.MinValue; // For ELA, we often want to see the errors clearly

            // ELA is typically most useful between 70 and 98
            for (int q = 70; q <= 100; q += 5)
            {
                string currentOutputPath = Path.Combine(outputFolder, $"ela_q{q}.png");

                GenerateErrorLevelAnalysis(inputPath, currentOutputPath, q);

                double averageIntensity = CalculateAverageIntensity(currentOutputPath);

                // For ELA, the "best" one is often where differences are most pronounced 
                // but not maxed out. Here we look for high descriptive detail.
                if (averageIntensity > maxIntensity)
                {
                    maxIntensity = averageIntensity;
                    bestTempPath = currentOutputPath;
                }
            }

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
