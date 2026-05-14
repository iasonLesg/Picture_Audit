using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Picture_Audit_Community
{
    public static class WaveletAnalysis
    {
        public static void GenerateWaveletAnalysis(string inputPath, string outputPath)
        {
            using (Image<Rgba32> original = Image.Load<Rgba32>(inputPath))
            {
                // Preserve original dimensions for the final output
                int origWidth = original.Width;
                int origHeight = original.Height;

                int width = origWidth % 2 == 0 ? origWidth : origWidth - 1;
                int height = origHeight % 2 == 0 ? origHeight : origHeight - 1;

                int halfW = width / 2;
                int halfH = height / 2;

                using (Image<Rgba32> llImg = new Image<Rgba32>(halfW, halfH))
                using (Image<Rgba32> lhImg = new Image<Rgba32>(halfW, halfH))
                using (Image<Rgba32> hlImg = new Image<Rgba32>(halfW, halfH))
                using (Image<Rgba32> hhImg = new Image<Rgba32>(halfW, halfH))
                {
                    original.ProcessPixelRows(accessorSrc =>
                    {
                        for (int y = 0; y < halfH; y++)
                        {
                            Span<Rgba32> row1 = accessorSrc.GetRowSpan(y * 2);
                            Span<Rgba32> row2 = accessorSrc.GetRowSpan(y * 2 + 1);

                            for (int x = 0; x < halfW; x++)
                            {
                                Rgba32 p1 = row1[x * 2];
                                Rgba32 p2 = row1[x * 2 + 1];
                                Rgba32 p3 = row2[x * 2];
                                Rgba32 p4 = row2[x * 2 + 1];

                                float a = (p1.R + p1.G + p1.B) / 3f;
                                float b = (p2.R + p2.G + p2.B) / 3f;
                                float c = (p3.R + p3.G + p3.B) / 3f;
                                float d = (p4.R + p4.G + p4.B) / 3f;

                                byte ll = (byte)Math.Clamp((a + b + c + d) / 4, 0, 255);
                                byte lh = (byte)Math.Clamp(Math.Abs(a - b + c - d) * 5, 0, 255);
                                byte hl = (byte)Math.Clamp(Math.Abs(a + b - c - d) * 5, 0, 255);
                                byte hh = (byte)Math.Clamp(Math.Abs(a - b - c + d) * 10, 0, 255);

                                llImg[x, y] = new Rgba32(ll, ll, ll, 255);
                                lhImg[x, y] = new Rgba32(lh, lh, lh, 255);
                                hlImg[x, y] = new Rgba32(hl, hl, hl, 255);
                                hhImg[x, y] = new Rgba32(hh, hh, hh, 255);
                            }
                        }
                    });

                    // Interpolate (Resize) each image back to the original size
                    // We use NearestNeighbor to keep the forensic "blocks" sharp
                    ResizeAndSave(llImg, origWidth, origHeight, outputPath, "_1"); //_LL
                    ResizeAndSave(lhImg, origWidth, origHeight, outputPath, "_2");//_LH
                    ResizeAndSave(hlImg, origWidth, origHeight, outputPath, "_3");//_HL
                    ResizeAndSave(hhImg, origWidth, origHeight, outputPath, "_4");//_HH
                }
            }
        }

        private static void ResizeAndSave(Image<Rgba32> img, int w, int h, string baseRoot, string suffix)
        {
            // Resize back to original dimensions
            img.Mutate(x => x.Resize(w, h, KnownResamplers.NearestNeighbor));

            string directory = Path.GetDirectoryName(baseRoot);
            string fileName = Path.GetFileNameWithoutExtension(baseRoot);
            string extention = Path.GetExtension(baseRoot);
            img.SaveAsPng(Path.Combine(directory, $"{fileName}{suffix}{extention}"));
        }
    }
}