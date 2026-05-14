using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Picture_Audit_Community
{
    public static class CAGIAnalysis
    {
        /// <summary>
        /// CAGI (Camera Artifacts & Grid Inconsistencies) Analysis.
        /// Extracts the sensor noise residual and analyzes local variance to find
        /// regions that lack the original camera's "fingerprint."
        /// </summary>
        public static void GenerateCAGIAnalysis(string inputPath, string outputPath)
        {
            //Load image
            using (Image<Rgba32> original = Image.Load<Rgba32>(inputPath))
            {
                //High-Pass Filter to extract Noise Residual
                // Clone the image to look at the noise
                using (Image<Rgba32> noiseResidual = original.Clone())
                {
                    //simulate a High-Pass by subtracting a blurred version from the original
                    using (Image<Rgba32> blurred = original.Clone(x => x.GaussianBlur(1.0f)))
                    {
                        original.ProcessPixelRows(blurred, noiseResidual, (accessorOrg, accessorBlur, accessorNoise) =>
                        {
                            for (int y = 0; y < accessorOrg.Height; y++)
                            {
                                Span<Rgba32> orgRow = accessorOrg.GetRowSpan(y);
                                Span<Rgba32> blurRow = accessorBlur.GetRowSpan(y);
                                Span<Rgba32> noiseRow = accessorNoise.GetRowSpan(y);

                                for (int x = 0; x < orgRow.Length; x++)
                                {
                                    // Extract the residual (Noise = Original - Blur) 
                                    // +Add 128 to center the noise around gray
                                    byte rN = (byte)Math.Clamp(128 + (orgRow[x].R - blurRow[x].R), 0, 255);
                                    byte gN = (byte)Math.Clamp(128 + (orgRow[x].G - blurRow[x].G), 0, 255);
                                    byte bN = (byte)Math.Clamp(128 + (orgRow[x].B - blurRow[x].B), 0, 255);
                                    noiseRow[x] = new Rgba32(rN, gN, bN, 255);
                                }
                            }
                        });
                    }

                    // Local Variance Analysis
                    // CAGI analyzes 4x4 or 8x8 blocks for noise consistency
                    using (Image<Rgba32> cagiMap = new Image<Rgba32>(original.Width, original.Height))
                    {
                        const int windowSize = 6; // Analysis window

                        noiseResidual.ProcessPixelRows(cagiMap, (accessorNoise, accessorCagi) =>
                        {
                            for (int y = windowSize; y < accessorNoise.Height - windowSize; y++)
                            {
                                Span<Rgba32> cagiRow = accessorCagi.GetRowSpan(y);

                                for (int x = windowSize; x < accessorNoise.Width - windowSize; x++)
                                {
                                    // Calculate variance in the local neighborhood
                                    float sum = 0;
                                    float sumSq = 0;
                                    int count = 0;

                                    for (int wy = -windowSize; wy <= windowSize; wy++)
                                    {
                                        Span<Rgba32> windowRow = accessorNoise.GetRowSpan(y + wy);
                                        for (int wx = -windowSize; wx <= windowSize; wx++)
                                        {
                                            float val = (windowRow[x + wx].R + windowRow[x + wx].G + windowRow[x + wx].B) / 3f;
                                            sum += val;
                                            sumSq += val * val;
                                            count++;
                                        }
                                    }

                                    // Statistical Variance Formula: E[X^2] - (E[X])^2
                                    float mean = sum / count;
                                    float variance = (sumSq / count) - (mean * mean);

                                    // Normalize and amplify the variance "Grid"
                                    // Real camera noise is low-variance; manipulations are high-variance
                                    byte intensity = (byte)Math.Clamp(variance * 15, 0, 255);
                                    cagiRow[x] = new Rgba32(intensity, intensity, intensity, 255);
                                }
                            }
                        });

                        // Post-process to highlight Grid Inconsistencies
                        cagiMap.Mutate(x => x.Contrast(1.5f).Saturate(1.2f));
                        cagiMap.SaveAsPng(outputPath);
                    }
                }
            }
        }
    }
}