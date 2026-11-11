using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace GridLocked
{
    /// <summary>
    /// Fast 32bpp direct memory bitmap for real-time pixel rendering without GDI+ overhead.
    /// </summary>
    public sealed class DirectBitmap : IDisposable
    {
        public Bitmap Bitmap { get; private set; }
        public int[] Bits { get; private set; }
        public bool Disposed { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }

        private GCHandle _handle;

        public DirectBitmap(int width, int height)
        {
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            Bits = new int[Width * Height];
            _handle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
            Bitmap = new Bitmap(Width, Height, Width * 4, PixelFormat.Format32bppRgb, _handle.AddrOfPinnedObject());
        }

        public void Clear(int argb)
        {
            for (int i = 0; i < Bits.Length; i++)
                Bits[i] = argb;
        }

        public void SetPixel(int x, int y, int argb)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                Bits[y * Width + x] = argb;
            }
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            Bitmap?.Dispose();
            if (_handle.IsAllocated)
                _handle.Free();
        }
    }
}
