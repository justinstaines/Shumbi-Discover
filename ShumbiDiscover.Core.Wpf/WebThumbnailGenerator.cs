#region Copyright Statement
// Copyright © 2009 Shumbi Ltd.
//
// All rights are reserved. Reproduction or transmission in whole or
// in part, in any form or by any means, electronic, mechanical or
// otherwise, is prohibited without the prior written consent
// of the copyright owner.
//
#endregion

using System;
using ShumbiDiscover.Model;
using Obany.Api.Model;
using System.Drawing;
using System.Windows.Media;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ShumbiDiscover.Core
{
    /// <summary>
    /// Class for generating web thumbnails
    /// </summary>
    public class WebThumbnailGenerator : IThumbnailGenerator
    {
        #region Delegates
        /// <summary>
        /// Event handler definition for when a thumbnail is added
        /// </summary>
        /// <param name="size">The size of the additional thumbnail</param>
        public delegate void ThumbnailAddedDelegate(long size);
        #endregion

        #region Static Fields
        private static string _thumbnailCacheFolder;
        #endregion

        #region Fields
        private Obany.Render.Web.HtmlToImage _htmlToImage;
        #endregion

        #region Static Events
        /// <summary>
        /// Event handler triggered by a thumbnail being added
        /// </summary>
        public static event ThumbnailAddedDelegate ThumbnailAdded;
        #endregion

        #region Static Properties
        /// <summary>
        /// Set the thumbnail cache folder
        /// </summary>
        public static string ThumbnailCacheFolder
        {
            set
            {
                _thumbnailCacheFolder = value;
            }
        }
        #endregion

        #region IThumbnailGenerator Methods
        /// <summary>
        /// The thumbnail generation must be queued
        /// </summary>
        /// <param name="isPreview">Is this a request for a preview</param>
        /// <returns>True of the request should be queued</returns>
        public bool ShouldQueue(bool isPreview)
        {
            return (!isPreview);
        }

        /// <summary>
        /// Generate a thumbnail for the url
        /// </summary>
        /// <param name="url">The url to generate the thumbnail for</param>
        /// <param name="width">The width of the thumbnail</param>
        /// <param name="height">The height of the thumbnail</param>
        /// <param name="isPreview">Is this a request for a preview</param>
        /// <returns>The location of the generated thumbnail</returns>
        public string GenerateThumbnail(string url, int width, int height, bool isPreview)
        {
            string returl = "";

            if (System.IO.Directory.Exists(_thumbnailCacheFolder))
            {
                string filename = System.IO.Path.Combine(_thumbnailCacheFolder, Obany.Cryptography.MD5.HashHex(url) + ".jpg");

                if (System.IO.File.Exists(filename))
                {
                    returl = filename;
                }
                else
                {
                    if (!isPreview)
                    {
                        try
                        {
                            _htmlToImage = new Obany.Render.Web.HtmlToImage();

                            string imageMimeType;
                            int imageWidth = 0;
                            int imageHeight = 0;

                            byte[] data = _htmlToImage.RenderToBitmap(url, out imageWidth, out imageHeight, out imageMimeType);

                            if (data != null)
                            {
                                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(data))
                                {
                                    using (System.Drawing.Image image = new System.Drawing.Bitmap(ms))
                                    {
                                        using (System.Drawing.Image thumbImage = ResizImage(image, width, height))
                                        {
                                            if (thumbImage != null)
                                            {
                                                ImageCodecInfo codecInfo = GetEncoderInfo(ImageFormat.Jpeg);
                                                Encoder encoder = Encoder.Quality;
                                                EncoderParameter encoderParam = new EncoderParameter(encoder, 75L);
                                                EncoderParameters encoderParams = new EncoderParameters(1);
                                                encoderParams.Param[0] = encoderParam;

                                                thumbImage.Save(filename, ImageFormat.Jpeg);

                                                if (ThumbnailAdded != null)
                                                {
                                                    ThumbnailAdded(new System.IO.FileInfo(filename).Length);
                                                }

                                                returl = filename;
                                            }
                                        }
                                    }
                                }
                            }

                            _htmlToImage = null;
                        }
                        catch
                        {
                        }
                    }
                }
            }

            return(returl);
        }

        /// <summary>
        /// Cancel the thumbnail generation
        /// </summary>
        public void CancelGeneration()
        {
            if (_htmlToImage != null)
            {
                _htmlToImage.Abort();
            }
        }
        #endregion

        #region Private Methods
        private static System.Drawing.Image ResizImage(System.Drawing.Image bitmap, int thumbWidth, int thumbHeight) 
        {
            Bitmap newImage = new Bitmap(thumbWidth, thumbHeight);

            using (Graphics g = Graphics.FromImage(newImage))                    
            {                        
                g.SmoothingMode = SmoothingMode.AntiAlias;                        
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;                        
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                decimal ratio = (decimal)thumbWidth / bitmap.Width;
                int newHeight = Convert.ToInt32(ratio * bitmap.Height);

                if (newHeight >= thumbHeight)
                {
                    g.DrawImage(bitmap, new System.Drawing.Rectangle(0, 0, thumbWidth, thumbHeight), 0, 0, bitmap.Width, bitmap.Width, System.Drawing.GraphicsUnit.Pixel);
                }
                else
                {
                    g.FillRectangle(System.Drawing.Brushes.White, new System.Drawing.Rectangle(0, 0, thumbWidth, thumbHeight));

                    int offset = (thumbHeight - newHeight) / 2;

                    g.DrawImage(bitmap, new System.Drawing.Rectangle(0, 0, thumbWidth, newHeight), 0, 0, bitmap.Width, bitmap.Height, System.Drawing.GraphicsUnit.Pixel);
                }
            }                

            return(newImage);
        }

        private static ImageCodecInfo GetEncoderInfo(ImageFormat imageFormat)
        {
            ImageCodecInfo foundCodec = null;

            ImageCodecInfo[] encoders = ImageCodecInfo.GetImageEncoders();
            for (int i = 0; i < encoders.Length && foundCodec == null; i++)
            {
                if (encoders[i].FormatID == imageFormat.Guid)
                {
                    foundCodec = encoders[i];
                }
            }
            return(foundCodec);
        }
        #endregion
    }
}
