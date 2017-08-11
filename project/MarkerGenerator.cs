using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MarkerGenerator
{
    public enum MarkerType
    {
        Vuforia = 1,
        ARToolkit = 2
    }

    public sealed class MarkerData
    {
        public int MarkerID { get; set; }
        public Bitmap MarkerImage { get; set; }
    }

    public sealed class MarkerGenerator : IDisposable
    {
        private byte[] _idBytes = new byte[4];
        private RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();
        private StringFormat _stringFormat = null;

        public MarkerGenerator()
        {
            _stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
        }
        ~MarkerGenerator()
        {
            Dispose(false);
        }

        /// <summary>
        /// Generates a random marker ID or Seed that can be used to generate a marker image. This Seed
        /// allows you to re-generate the image from an integer if you want to store the marker without
        /// saving the image.
        /// </summary>
        /// <returns>Returns a randomly generated int between int.MinValue and int.MaxValue</returns>
        public int GenerateRandomID()
        {
            // Lock our _idBytes buffer so we are thread safe if we want to multi-thread this operation.
            // We are fine locking this because it should be relatively quick and minimize blocking.
            lock (_rng)
            {
                _rng.GetBytes(_idBytes); // Get some random bytes for our marker ID (seed)
                return BitConverter.ToInt32(_idBytes, 0); // Get our ID from our bytes
            }
        }

        public MarkerData CreateMarker(int? id, int size, string information = null, MarkerType type = MarkerType.Vuforia)
        {

            // If we were not provided an ID then generate a random one
            if (id.HasValue == false) { id = GenerateRandomID(); }

            Dictionary<string, object> systemParameters = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
            systemParameters.Add("id", id);

            using (Bitmap marker = GenerateMarkerImage(type, id.Value))
            {
                Bitmap fullRes = GenerateResizedMarker(marker, size);
                using (Graphics g = Graphics.FromImage(fullRes))
                {
                    if (string.IsNullOrWhiteSpace(information) == false)
                    {
                        // Fill in any system variables that are available
                        MatchCollection matches = Regex.Matches(information, @"\{([a-zA-Z0-9]+)\}");
                        if (matches.Count > 0)
                        {
                            StringBuilder infoStringBuilder = new StringBuilder();
                            int cursor = 0;
                            int matchIndex = 0;
                            Match match = matches[0];

                            while (cursor < information.Length)
                            {
                                if (match != null && cursor == match.Index)
                                {
                                    // Skip over this match
                                    cursor += match.Length;

                                    // Lookup the proper value to use here and insert it into the info string
                                    string key = match.Groups[1].Value;
                                    infoStringBuilder.Append(systemParameters.ContainsKey(key) ? systemParameters[key].ToString() : key);

                                    // Go to our next match and skip ahead in our info string
                                    matchIndex++;
                                    match = (matchIndex >= matches.Count) ? null : matches[matchIndex];
                                }
                                else
                                {
                                    infoStringBuilder.Append(information[cursor++]);
                                }
                            }

                            information = infoStringBuilder.ToString();
                        }

                        int padding = (int)Math.Round(size * 0.0625f);
                        using (Font infoFont = new Font(FontFamily.GenericMonospace, padding / 3))
                        {
                            g.SmoothingMode = SmoothingMode.HighQuality;
                            g.DrawString(information, infoFont, Brushes.White, new Rectangle(0, 0, size, padding), _stringFormat);
                        }
                    }
                }

                return new MarkerData
                {
                    MarkerID = id.Value,
                    MarkerImage = fullRes
                };
            }
        }

        /// <summary>
        /// Resizes a marker image using Nearest Neighbor Interpolation to preserve the hard edges
        /// of the marker image. This is important to keep a high score on Vuforia and the like since they
        /// mainly look at corners and other hard edges.
        /// </summary>
        /// <param name="markerBase">The source image that we are going to be scaling up</param>
        /// <param name="newSize">The new marker size (We always generate a square so we only need one dimension)</param>
        /// <returns></returns>
        public unsafe Bitmap GenerateResizedMarker(Bitmap markerBase, int newSize)
        {
            // Get our source image ready for reading
            BitmapData srcImageData = markerBase.LockBits(new Rectangle(0, 0, markerBase.Width, markerBase.Height), ImageLockMode.ReadOnly, markerBase.PixelFormat);
            byte* srcPixelPointer = (byte*)srcImageData.Scan0;
            int srcStride = srcImageData.Stride;

            // Get our destination image ready for writing
            Bitmap destImage = new Bitmap(newSize, newSize, markerBase.PixelFormat);
            BitmapData destImageData = destImage.LockBits(new Rectangle(0, 0, newSize, newSize), ImageLockMode.WriteOnly, destImage.PixelFormat);
            byte* destPixelPointer = (byte*)destImageData.Scan0;
            int destStride = destImageData.Stride;

            // Get our shared characteristics
            int pixelOffset = Image.GetPixelFormatSize(destImage.PixelFormat) / 8;
            double scale = (markerBase.Width - 1) / (double)(newSize - 1);

            // Setup our working variables for the loop
            int srcIndex, destIndex, sx, sy;

            // Loop through our new image pixels and downscale to the original marker image (This is nearest neighbor interpolation)
            for (int x = 0; x < newSize; x++)
            {
                for (int y = 0; y < newSize; y++)
                {
                    // Downscale our source coordinates to our sample image
                    sx = (int)Math.Round(x * scale);
                    sy = (int)Math.Round(y * scale);

                    // We don't need to do this full calculation per pixel but the cost is nominal
                    srcIndex = sy * srcStride + sx * pixelOffset;
                    destIndex = y * destStride + x * pixelOffset;

                    // Read and write directly via our pointer for performance
                    destPixelPointer[destIndex] = srcPixelPointer[srcIndex];
                    destPixelPointer[destIndex + 1] = srcPixelPointer[srcIndex + 1];
                    destPixelPointer[destIndex + 2] = srcPixelPointer[srcIndex + 2];
                }
            }

            return destImage;
        }

        /// <summary>
        /// Generates a base marker image of the specified type for the provided seed.
        /// </summary>
        /// <param name="type">The type of marker you would like to generate</param>
        /// <param name="seed">The seed for the Random object</param>
        /// <returns>A new base marker image. This is a low resolution image and should be scaled before use.</returns>
        public Bitmap GenerateMarkerImage(MarkerType type, int seed)
        {
            // Decide which type of marker was requested and generate it.
            switch(type)
            {
                case MarkerType.ARToolkit:
                    return GenerateMarkerImage(seed, 32, 8, 1);

                case MarkerType.Vuforia:
                default:
                    return GenerateMarkerImage(seed, 64, 4, 3);
            }
        }

        /// <summary>
        /// Generates a marker based on the provided parameters. This is the base marker images and should
        /// be scaled up before use.
        /// </summary>
        /// <param name="seed">The random seed to use for generating the marker</param>
        /// <param name="baseMarkerSize">The size for this marker image before being scaled</param>
        /// <param name="borderSize">Thickness of the border for the marker image</param>
        /// <param name="borderPadding"></param>
        /// <returns></returns>
        public Bitmap GenerateMarkerImage(int seed, int baseMarkerSize, int borderSize, int borderPadding)
        {
            // Setup some additional size calculations before we start drawing
            int borderAndPadding = borderSize + borderPadding;
            int sizeLessBorder = baseMarkerSize - borderSize;
            int sizeLessBorderAndPadding = baseMarkerSize - borderAndPadding;

            // Create our base marker image
            Bitmap marker = new Bitmap(baseMarkerSize, baseMarkerSize, PixelFormat.Format24bppRgb);

            // If we have a border, let's use the Graphics object to draw that for performance
            if (borderSize > 0)
            {
                using (Graphics g = Graphics.FromImage(marker))
                {
                    g.SmoothingMode = SmoothingMode.None;

                    g.Clear(Color.White);
                    g.FillRectangle(Brushes.Black, 0, 0, baseMarkerSize, borderSize); // Top
                    g.FillRectangle(Brushes.Black, sizeLessBorder, 0, borderSize, baseMarkerSize); // Right
                    g.FillRectangle(Brushes.Black, 0, sizeLessBorder, baseMarkerSize, borderSize); // Bottom
                    g.FillRectangle(Brushes.Black, 0, 0, borderSize, baseMarkerSize); // Left)
                }
            }

            // Create our random number generated seeded with our unique ID
            Random r = new Random(seed);

            // Generate our unique marker pattern
            for (int x = borderAndPadding; x < sizeLessBorderAndPadding; x++)
            {
                for (int y = borderAndPadding; y < sizeLessBorderAndPadding; y++)
                {
                    marker.SetPixel(x, y, (r.Next(2) == 1) ? Color.Black : Color.White);
                }
            }

            return marker;
        }

        public void Dispose() { Dispose(true); }
        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                _rng.Dispose();
            }

            _rng = null;
        }
    }
}

