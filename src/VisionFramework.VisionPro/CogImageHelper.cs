using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
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

            // Step 1: 用 32bppArgb 中间位图做灰度变换（Graphics 不支持索引像素格式）
            using (var temp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(temp))
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

                // Step 2: 手动将 32bppArgb 转为 8bppIndexed
                var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format8bppIndexed);
                var pal = dst.Palette;
                for (int i = 0; i < 256; i++) pal.Entries[i] = Color.FromArgb(i, i, i);
                dst.Palette = pal;

                var rect = new Rectangle(0, 0, src.Width, src.Height);
                var srcData = temp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                var dstData = dst.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
                try
                {
                    byte[] srcBytes = new byte[srcData.Stride * src.Height];
                    byte[] dstBytes = new byte[dstData.Stride * src.Height];
                    Marshal.Copy(srcData.Scan0, srcBytes, 0, srcBytes.Length);

                    for (int y = 0; y < src.Height; y++)
                    {
                        int srcOff = y * srcData.Stride;
                        int dstOff = y * dstData.Stride;
                        for (int x = 0; x < src.Width; x++)
                        {
                            // 32bppArgb 内存布局: B, G, R, A — 灰度变换后 B=G=R
                            dstBytes[dstOff + x] = srcBytes[srcOff + x * 4];
                        }
                    }

                    Marshal.Copy(dstBytes, 0, dstData.Scan0, dstBytes.Length);
                }
                finally
                {
                    temp.UnlockBits(srcData);
                    dst.UnlockBits(dstData);
                }
                return dst;
            }
        }
    }
}