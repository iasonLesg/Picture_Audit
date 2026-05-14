using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
namespace Picture_Audit_Community
{
    public static class BlockDecompositionAnalysis
    {
        /// <summary>
        /// Analyzes the 8x8 JPEG blocking artifacts. 
        /// It highlights the discontinuities at block boundaries to identify 
        /// regions where the JPEG grid has been disturbed or misaligned.
        /// </summary>
        public static void GenerateBlockAnalysis(string inputPath, string outputPath)
        {
            //Load image
            using (Image<Rgba32> image = Image.Load<Rgba32>(inputPath))
            {
                //create a result map of the same size
                using (Image<Rgba32> blockMap = new Image<Rgba32>(image.Width, image.Height))
                {
                    // Extract the blocking artifacts
                    //analyze the image in 8x8 blocks to check for boundary energy
                    image.ProcessPixelRows(blockMap, (accessorSrc, accessorDest) =>
                    {
                        for (int y = 1; y < accessorSrc.Height - 1; y++)
                        {
                            Span<Rgba32> prevRow = accessorSrc.GetRowSpan(y - 1);
                            Span<Rgba32> currRow = accessorSrc.GetRowSpan(y);
                            Span<Rgba32> nextRow = accessorSrc.GetRowSpan(y + 1);
                            Span<Rgba32> destRow = accessorDest.GetRowSpan(y);

                            bool isHorizontalBoundary = (y % 8 == 0);

                            for (int x = 1; x < currRow.Length - 1; x++)
                            {
                                bool isVerticalBoundary = (x % 8 == 0);

                                //Calculate the 'Blockiness' 
                                int horizontalDiff = Math.Abs(currRow[x].R - nextRow[x].R) +
                                                     Math.Abs(currRow[x].G - nextRow[x].G) +
                                                     Math.Abs(currRow[x].B - nextRow[x].B);

                                int verticalDiff = Math.Abs(currRow[x].R - currRow[x + 1].R) +
                                                   Math.Abs(currRow[x].G - currRow[x + 1].G) +
                                                   Math.Abs(currRow[x].B - currRow[x + 1].B);

                                int energy = 0;

                                // JPEG grid line-there should be an artifact
                                if (isHorizontalBoundary || isVerticalBoundary)
                                {
                                    energy = (horizontalDiff + verticalDiff) / 2;
                                }
                                else
                                {
                                    // If we are INSIDE a block, a high difference is just image detail, 
                                    // not a blocking artifact.suppress this to reduce noise.
                                    energy = 0;
                                }

                                // Amplify for visualization
                                byte val = (byte)Math.Min(255, energy * 15);
                                destRow[x] = new Rgba32(val, val, val, 255);
                            }
                        }
                    });

                    //Post-processing to make the grid visible
                    blockMap.Mutate(x => x.GaussianBlur(0.5f));

                    //Save as PNG
                    blockMap.SaveAsPng(outputPath);
                }
            }
        }


        
    }
}