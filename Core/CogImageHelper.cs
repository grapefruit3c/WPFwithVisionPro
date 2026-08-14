using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using Cognex.VisionPro;
using Cognex.VisionPro.ImageFile;

namespace VisionProVppHost.Core
{
    /// <summary>
    /// 把磁盘图像转换为 VisionPro 的 ICogImage。
    /// 支持 VisionPro 原生格式（.idb/.cdb）和标准位图（.bmp/.jpg/.png/.tif）。
    /// </summary>
    public static class CogImageHelper
    {
        /// <summary>
        /// 加载图像文件为 ICogImage。
        /// .idb/.cdb 文件使用 CogImageFile 读取（可能包含多张图，默认取第一张）；
        /// 标准位图使用 Bitmap → CogImage8Grey 转换。
        /// </summary>
        public static ICogImage LoadAsCogImage(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();

            // VisionPro 原生图像格式：.idb / .cdb
            if (ext == ".idb" || ext == ".cdb")
            {
                return LoadFromCogImageFile(path);
            }

            // 标准位图格式
            using (var src = new Bitmap(path))
            {
                Bitmap bmp8 = To8bppGrayscale(src);
                try
                {
                    return new CogImage8Grey(bmp8);
                }
                finally
                {
                    bmp8.Dispose();
                }
            }
        }

        /// <summary>
        /// 使用 CogImageFile 加载 .idb/.cdb 文件，返回第一张图像。
        /// </summary>
        private static ICogImage LoadFromCogImageFile(string path)
        {
            var imageFile = new CogImageFile();
            imageFile.Open(path, CogImageFileModeConstants.Read);
            try
            {
                if (imageFile.Count == 0)
                    throw new InvalidDataException("图像文件中不包含任何图像。");
                return imageFile[0];
            }
            finally
            {
                imageFile.Close();
            }
        }

        private static Bitmap To8bppGrayscale(Bitmap src)
        {
            if (src.PixelFormat == PixelFormat.Format8bppIndexed)
                return new Bitmap(src);

            var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format8bppIndexed);
            var pal = dst.Palette;
            for (int i = 0; i < 256; i++)
                pal.Entries[i] = Color.FromArgb(i, i, i);
            dst.Palette = pal;

            using (var g = Graphics.FromImage(dst))
            {
                var cm = new ColorMatrix(new[]
                {
                    new float[] { 0.299f, 0.299f, 0.299f, 0, 0 },
                    new float[] { 0.587f, 0.587f, 0.587f, 0, 0 },
                    new float[] { 0.114f, 0.114f, 0.114f, 0, 0 },
                    new float[] { 0, 0, 0, 1, 0 },
                    new float[] { 0, 0, 0, 0, 1 }
                });
                using (var ia = new ImageAttributes())
                {
                    ia.SetColorMatrix(cm);
                    g.DrawImage(src,
                        new Rectangle(0, 0, src.Width, src.Height),
                        0, 0, src.Width, src.Height, GraphicsUnit.Pixel, ia);
                }
            }
            return dst;
        }
    }
}
