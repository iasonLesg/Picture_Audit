using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Picture_Audit_Community
{
    public static class DoubleQuantizationAnalysis
    {
        /// <summary>
        /// Detects double quantization artifacts by comparing the image against a 
        /// shifted re-compression. This highlights areas where JPEG grids do not align.
        /// </summary>
        public static void GenerateDQAnalysis(string inputPath, string outputPath, int quality = 90)
        {
            // Load the image
            using (Image<Rgba32> original = Image.Load<Rgba32>(inputPath))
            {
                // create a "shifted" version to find the 8x8 JPEG grid misalignment
                // typical of tampered/double-compressed images.
                using (Image<Rgba32> shifted = original.Clone(x => x.Crop(new Rectangle(4, 4, original.Width - 4, original.Height - 4))))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        // Re-compress the shifted image
                        JpegEncoder encoder = new JpegEncoder { Quality = quality };
                        shifted.SaveAsJpeg(ms, encoder);
                        ms.Seek(0, SeekOrigin.Begin);

                        // Load the re-compressed shifted version
                        using (Image<Rgba32> resavedShifted = Image.Load<Rgba32>(ms))
                        {
                            // Create result canvas (aligned to the shifted size)
                            using (Image<Rgba32> dqImage = new Image<Rgba32>(resavedShifted.Width, resavedShifted.Height))
                            {
                                // Compare the original (at offset 4,4) with the re-compressed version
                                // Double quantization often leaves a "lattice" or "checkerboard" pattern
                                // in areas that have been modified.
                                original.ProcessPixelRows(resavedShifted, dqImage, (accessorOriginal, accessorResaved, accessorDq) =>
                                {
                                    for (int y = 0; y < accessorResaved.Height; y++)
                                    {
                                        // Offset the original by 4 pixels to check grid misalignment
                                        Span<Rgba32> orgRow = accessorOriginal.GetRowSpan(y + 4);
                                        Span<Rgba32> resRow = accessorResaved.GetRowSpan(y);
                                        Span<Rgba32> dqRow = accessorDq.GetRowSpan(y);

                                        for (int x = 0; x < resRow.Length; x++)
                                        {
                                            // Calculate the difference between original and re-compressed
                                            // We focus on the Luminance (Y) channel as JPEG quantization 
                                            int rDiff = Math.Abs(orgRow[x + 4].R - resRow[x].R);
                                            int gDiff = Math.Abs(orgRow[x + 4].G - resRow[x].G);
                                            int bDiff = Math.Abs(orgRow[x + 4].B - resRow[x].B);

                                            // Amplify the DQ artifacts (higher scale than ELA usually)
                                            const int scale = 30;
                                            dqRow[x] = new Rgba32(
                                                (byte)Math.Min(255, rDiff * scale),
                                                (byte)Math.Min(255, gDiff * scale),
                                                (byte)Math.Min(255, bDiff * scale),
                                                255
                                            );
                                        }
                                    }
                                });

                                // Save as PNG
                                dqImage.SaveAsPng(outputPath);
                            }
                        }
                    }
                }
            }
        }
    }
}