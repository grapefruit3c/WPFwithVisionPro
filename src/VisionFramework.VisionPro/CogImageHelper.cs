using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using Cognex.VisionPro;
using Cognex.VisionPro.ImageFile;

namespace VisionFramework.VisionPro
{
    /// <summary>
    /// 图像转换工具：磁盘图像 → ICogImage。
    /// 支持 VisionPro .idb/.cdb 和标准位图。
    /// 从原 CogImageHelper 迁移，增加多图索引支持。
    /// </summary>
    public static class CogImageHelper
    {
        public static ICogImage LoadAsCogImage(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".idb" || ext == ".cdb")
                return LoadFromCogImageFile(path, 0);
            using (var src = new Bitmap(path))
            {
                var bmp8 = To8bppGrayscale(src);
                try { return new CogImage8Grey(bmp8); }
                finally { bmp8.Dispose(); }
            }
        }

        public static ICogImage LoadFromCogImageFile(string path, int index = 0)
        {
            var imageFile = new CogImageFile();
            imageFile.Open(path, CogImageFileModeConstants.Read);
            try
            {
                if (imageFile.Count == 0)
                    throw new InvalidDataException("图像文件中不包含任何图像。");
                return imageFile[System.Math.Min(index, imageFile.Count - 1)];
            }
            finally { imageFile.Close(); }
        }

        public static int GetImageCount(string path)
        {
            var imageFile = new CogImageFile();
            imageFile.Open(path, CogImageFileModeConstants.Read);
            try { return imageFile.Count; }
            finally { imageFile.Close(); }
        }

        public static Bitmap To8bppGrayscale(Bitmap src)
        {
            if (src.PixelFormat == PixelFormat.Format8bppIndexed)
                return new Bitmap(src);
            var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format8bppIndexed);
            var pal = dst.Palette;
            for (int i = 0; i < 256; i++) pal.Entries[i] = Color.FromArgb(i, i, i);
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
                    g.DrawImage(src, new Rectangle(0, 0, src.Width, src.Height),
                        0, 0, src.Width, src.Height, GraphicsUnit.Pixel, ia);
                }
            }
            return dst;
        }
    }
}