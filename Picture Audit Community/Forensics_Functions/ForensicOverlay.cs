using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Picture_Audit_Community.Forensics_Functions
{
    public static class ForensicOverlay
    {
        public static void GenerateFinalHeatmap(string originalPath, string consensusPath, string outputPath,bool Annotate=false)
        {
            using (Image<Rgba32> original = Image.Load<Rgba32>(originalPath))
            using (Image<Rgba32> consensus = Image.Load<Rgba32>(consensusPath))
            {
                // Find the SMALLEST dimensions shared by both images to prevent IndexOutOfRange
                int width = Math.Min(original.Width, consensus.Width);
                int height = Math.Min(original.Height, consensus.Height);

                // Calculate density based on the consensus image size
                float[,] densityMap = CalculateDensityMap(consensus, 5);

                original.ProcessPixelRows(consensus, (origAcc, consAcc) =>
                {
                    // Use the 'height' variable we calculated above
                    for (int y = 0; y < height; y++)
                    {
                        Span<Rgba32> oRow = origAcc.GetRowSpan(y);
                        Span<Rgba32> cRow = consAcc.GetRowSpan(y);

                        // Use the 'width' variable we calculated above
                        for (int x = 0; x < width; x++)
                        {
                            // Safety: Even if width is correct, spans can sometimes be 
                            // shorter due to internal padding. Check x against spans.
                            if (x >= oRow.Length || x >= cRow.Length) break;

                            float intensity = (cRow[x].R / 255f);
                            if (intensity < 0.1f) continue;

                            float density = (x < densityMap.GetLength(0) && y < densityMap.GetLength(1))
                                            ? densityMap[x, y]
                                            : 0;
                            if (Annotate)
                            {
                                if (density > 0.5f) // Deep Red (Critical)
                                {
                                    oRow[x].R = (byte)Math.Clamp(oRow[x].R + (255 * intensity), 0, 255);
                                    oRow[x].G = (byte)(oRow[x].G * (1 - intensity));
                                    oRow[x].B = (byte)(oRow[x].B * (1 - intensity));
                                }
                                else if (density > 0.45f) // Orange/Red-Orange (Danger)
                                {
                                    oRow[x].R = (byte)Math.Clamp(oRow[x].R + (255 * intensity), 0, 255);
                                    oRow[x].G = (byte)Math.Clamp(oRow[x].G + (120 * intensity), 0, 255);
                                    oRow[x].B = (byte)(oRow[x].B * (1 - intensity));
                                }
                                else if (density > 0.4f) // Yellow (Warning)
                                {
                                    oRow[x].R = (byte)Math.Clamp(oRow[x].R + (255 * intensity), 0, 255);
                                    oRow[x].G = (byte)Math.Clamp(oRow[x].G + (255 * intensity), 0, 255);
                                    oRow[x].B = (byte)(oRow[x].B * (1 - intensity));
                                }


                            }
                            else {

                                // Multi-stage Gradient Logic: Blue -> Cyan -> Green -> Yellow -> Orange -> Red
                                if (density > 0.85f) // Deep Red (Critical)
                                {
                                    oRow[x].R = (byte)Math.Clamp(oRow[x].R + (255 * intensity), 0, 255);
                                    oRow[x].G = (byte)(oRow[x].G * (1 - intensity));
                                    oRow[x].B = (byte)(oRow[x].B * (1 - intensity));
                                }
                                else if (density > 0.65f) // Orange/Red-Orange (Danger)
                                {
                                    oRow[x].R = (byte)Math.Clamp(oRow[x].R + (255 * intensity), 0, 255);
                                    oRow[x].G = (byte)Math.Clamp(oRow[x].G + (120 * intensity), 0, 255);
                                    oRow[x].B = (byte)(oRow[x].B * (1 - intensity));
                                }
                                else if (density > 0.45f) // Yellow (Warning)
                                {
                                    oRow[x].R = (byte)Math.Clamp(oRow[x].R + (255 * intensity), 0, 255);
                                    oRow[x].G = (byte)Math.Clamp(oRow[x].G + (255 * intensity), 0, 255);
                                    oRow[x].B = (byte)(oRow[x].B * (1 - intensity));
                                }
                                else if (density > 0.30f) // Green (Anomalous)
                                {
                                    oRow[x].G = (byte)Math.Clamp(oRow[x].G + (255 * intensity), 0, 255);
                                    oRow[x].R = (byte)(oRow[x].R * (1 - intensity));
                                    oRow[x].B = (byte)(oRow[x].B * (1 - intensity));
                                }
                                else if (density > 0.15f) // Cyan (Trace levels)
                                {
                                    oRow[x].G = (byte)Math.Clamp(oRow[x].G + (255 * intensity), 0, 255);
                                    oRow[x].B = (byte)Math.Clamp(oRow[x].B + (255 * intensity), 0, 255);
                                    oRow[x].R = (byte)(oRow[x].R * (1 - intensity));
                                }
                                else // Blue (Background Noise)
                                {
                                    oRow[x].B = (byte)Math.Clamp(oRow[x].B + (255 * intensity), 0, 255);
                                    oRow[x].R = (byte)(oRow[x].R * (1 - intensity));
                                    oRow[x].G = (byte)(oRow[x].G * (1 - intensity));
                                }

                            }
                            
                        }
                    }
                });

                original.SaveAsPng(outputPath);
            }
        }

      
        private static float[,] CalculateDensityMap(Image<Rgba32> consensus, int radius)
        {
            int w = consensus.Width;
            int h = consensus.Height;
            float[,] map = new float[w, h]; // Initialized with 0s

            consensus.ProcessPixelRows(accessor =>
            {
                // We stay inside the safe boundaries for the 5x5 window
                for (int y = radius; y < h - radius; y++)
                {
                    for (int x = radius; x < w - radius; x++)
                    {
                        int activePixels = 0;
                        int totalInWindow = 0;

                        for (int wy = -radius; wy <= radius; wy++)
                        {
                            // Using the accessor to get the row safe from the loop
                            var windowRow = accessor.GetRowSpan(y + wy);
                            for (int wx = -radius; wx <= radius; wx++)
                            {
                                if (windowRow[x + wx].R > 50) activePixels++;
                                totalInWindow++;
                            }
                        }
                        map[x, y] = (float)activePixels / totalInWindow;
                    }
                }
            });
            return map;
        }
    }
}