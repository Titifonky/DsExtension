using LogDebugging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace Cmds.Poinconner
{
    public static class RandomHelper
    {
        public static readonly Random Random = new Random();
    }

    public static class MathHelper
    {
        public const float Pi = (float)Math.PI;
        public const float HalfPi = (float)(Math.PI / 2);
        public const float TwoPi = (float)(Math.PI * 2);
        public static int Min(this List<int> liste)
        {
            int r = liste[0];
            foreach (var nb in liste)
                r = Math.Min(r, nb);

            return r;
        }

        public static int Max(this List<int> liste)
        {
            int r = liste[0];
            foreach (var nb in liste)
                r = Math.Max(r, nb);

            return r;
        }

        public static int FloorToInt(float n) { return (int)Math.Floor(n); }

        public static int FloorToInt(double n) { return (int)Math.Floor(n); }
    }

    public static class BitmapHelper
    {
        public static Bitmap Redimensionner(this Bitmap bmp, Size dim)
        {
            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)dim.Width / (float)bmp.Width);
            nPercentH = ((float)dim.Height / (float)bmp.Height);

            if (nPercentH < nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;

            int destWidth = (int)(bmp.Width * nPercent);
            int destHeight = (int)(bmp.Height * nPercent);

            Bitmap pBmp = new Bitmap(destWidth, destHeight);
            Graphics pGraphic = Graphics.FromImage((Image)pBmp);
            pGraphic.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            pGraphic.DrawImage(bmp, 0, 0, destWidth, destHeight);
            pGraphic.Dispose();

            return pBmp;
        }

        private static Bitmap _Bmp = null;
        private static BitmapData _BmpData = null;
        private static int _Depth = -1;
        private static Boolean _Verrouiller = false;
        public static void Verrouiller(Bitmap bmp)
        {
            try
            {
                if (!_Verrouiller)
                {
                    _Bmp = bmp;
                    _BmpData = _Bmp.LockBits(new Rectangle(0, 0, _Bmp.Width, _Bmp.Height), ImageLockMode.ReadWrite, _Bmp.PixelFormat);
                    _Depth = Bitmap.GetPixelFormatSize(_Bmp.PixelFormat);
                    _Verrouiller = true;
                }
            }
            catch (Exception ex)
            {
                Log.Message(ex);
                throw ex;
            }
        }
        public static void Liberer()
        {
            try
            {
                if (_Verrouiller)
                {
                    _Bmp.UnlockBits(_BmpData);
                    _Depth = -1;
                    _BmpData = null;
                    _Bmp = null;
                    _Verrouiller = false;
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static unsafe Color GetPixel(int x, int y)
        {
            if (_Verrouiller)
            {
                byte* pBp = (byte*)_BmpData.Scan0;

                switch (_Depth)
                {
                    case 32:
                        pBp += (x * 4) + (y * _BmpData.Stride);
                        return Color.FromArgb(*(pBp + 3), *(pBp + 2), *(pBp + 1), *pBp);
                    case 24:
                        pBp += (x * 3) + (y * _BmpData.Stride);
                        return Color.FromArgb(*(pBp + 2), *(pBp + 1), *pBp);
                    case 8:
                        pBp += x + (y * _BmpData.Stride);
                        return Color.FromArgb(*pBp, *pBp, *pBp);
                }
            }

            return Color.Empty;
        }
        public static unsafe void SetPixel(int x, int y, Color color)
        {
            if (_Verrouiller)
            {

                byte* pBp = (byte*)_BmpData.Scan0;

                switch (_Depth)
                {
                    case 32:
                        pBp += (x * 4) + (y * _BmpData.Stride);
                        *(pBp + 3) = color.A;
                        *(pBp + 2) = color.R;
                        *(pBp + 1) = color.G;
                        *pBp = color.B;
                        break;
                    case 24:
                        pBp += (x * 3) + (y * _BmpData.Stride);
                        *(pBp + 2) = color.R;
                        *(pBp + 1) = color.G;
                        *pBp = color.B;
                        break;
                    case 8:
                        pBp += x + (y * _BmpData.Stride);
                        *pBp = color.B;
                        break;
                }
            }
        }

        private delegate int FoncCanal(Color c);
        private static readonly Dictionary<Canal, FoncCanal> DicFonc = new Dictionary<Canal, FoncCanal>
            {
                { Canal.Rouge, c => Convert.ToInt32(c.R) },
                { Canal.Vert, c => Convert.ToInt32(c.G) },
                { Canal.Bleu, c => Convert.ToInt32(c.B) },
                { Canal.Alpha, c => Convert.ToInt32(c.A) },
                { Canal.Teinte, c => Convert.ToInt32((c.GetHue() * 255) / 360) },
                { Canal.Saturation, c => Convert.ToInt32(c.GetSaturation() * 255) },
                { Canal.Luminosite, c => Convert.ToInt32(c.GetBrightness() * 255) }
            };
        public static int ValeurCanal(this Color c, Canal canal) { return DicFonc[canal](c); }
        public static int ValeurCanal(int x, int y, Canal canal) { return DicFonc[canal](GetPixel(x, y)); }
        public enum Canal
        {
            Rouge,
            Vert,
            Bleu,
            Alpha,
            Teinte,
            Saturation,
            Luminosite
        }
        public static Dictionary<Canal, int[]> Histogramme(this Bitmap bmp)
        {
            int W = bmp.Width;
            int H = bmp.Height;
            var pixels_map = new Dictionary<Canal, int[]>();
            var histogramme = new Dictionary<Canal, int[]>();

            foreach (Canal canal in Enum.GetValues(typeof(Canal)))
            {
                pixels_map.Add(canal, new int[W * H]);
                histogramme.Add(canal, new int[255 + 1]);
            }

            Verrouiller(bmp);

            Parallel.For(0, H, y =>
            {
                for (int x = 0; x < W; x++)
                {
                    Color pix = GetPixel(x, y);
                    int pos = x + W * y;

                    foreach (Canal canal in Enum.GetValues(typeof(Canal)))
                        pixels_map[canal][pos] = DicFonc[canal](pix);
                }
            });

            Liberer();

            foreach (var couleur in pixels_map)
                for (int i = 0; i < couleur.Value.Length; i++)
                    histogramme[couleur.Key][couleur.Value[i]]++;

            return histogramme;
        }
    }
}
