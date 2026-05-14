using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Picture_Audit_Community
{
    public static class ImageHighlight
    {
        /// <summary>
        /// Takes a forensic analysis result and converts it into a transparent red overlay.
        /// </summary>
        /// <param name="originalPath">The path to the source image (used for dimensions).</param>
        /// <param name="analysisPath">The path to the forensic result (ELA, Noise, etc.).</param>
        /// <param name="outputPath">Where to save the resulting transparent PNG.</param>
        /// <param name="sensitivity">Higher values make faint alterations more visible (default 10).</param>
        public static void GenerateTransparentMask(string originalPath, string analysisPath, string outputPath, int sensitivity = 10, bool createhighlights=false)
        {
            if(createhighlights){ 
            // 1. Load both images
            using (Image<Rgba32> original = Image.Load<Rgba32>(originalPath))
            using (Image<Rgba32> analysis = Image.Load<Rgba32>(analysisPath))
            {
                // Ensure the mask matches the original image dimensions
                using (Image<Rgba32> highlightMask = new Image<Rgba32>(original.Width, original.Height))
                {
                    // 2. Process the analysis image to create the transparency map
                    // We iterate based on the original dimensions
                    analysis.ProcessPixelRows(highlightMask, (analysisAccessor, maskAccessor) =>
                    {
                        for (int y = 0; y < original.Height; y++)
                        {
                            // Safety check: Ensure we don't go out of bounds if images differ slightly in size
                            if (y >= analysisAccessor.Height) break;

                            Span<Rgba32> analysisRow = analysisAccessor.GetRowSpan(y);
                            Span<Rgba32> maskRow = maskAccessor.GetRowSpan(y);

                            for (int x = 0; x < original.Width; x++)
                            {
                                if (x >= analysisRow.Length) break;

                                // Get the "intensity" of the analysis at this pixel
                                // Most forensic algorithms output brighter pixels where anomalies exist
                                Rgba32 pixel = analysisRow[x];

                                // Calculate brightness/intensity (Luma-ish)
                                int intensity = (pixel.R + pixel.G + pixel.B) / 3;

                                // Map intensity to Alpha. 
                                // Areas with 0 intensity (black) become transparent.
                                // Areas with high intensity become solid red.
                                int alpha = intensity * sensitivity;

                                maskRow[x] = new Rgba32(
                                    255, // Red
                                    0,   // Green
                                    0,   // Blue
                                    (byte)Math.Clamp(alpha, 0, 255)
                                );
                            }
                        }
                    });

                    // 3. Export as PNG to preserve transparency
                    highlightMask.SaveAsPng(outputPath);
                    }
                }
            }
        }
    }
}