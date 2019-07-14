using LogDebugging;
using System;
using System.Collections.Generic;
using System.Drawing;
using static Cmds.Poinconner.BitmapHelper;

namespace Cmds.Poinconner
{
    public static class BitmapRejectionSampler
    {
        public static List<PointF> Run(DraftSight.Interop.dsAutomation.ReferenceImage img, int nbPoint)
        {
            var sites = new List<PointF>();

            try
            {
                int LgMM = (int)img.Width;
                int HtMM = (int)img.Height;
                int LgPx = LgMM + 1;
                int HtPx = HtMM + 1;
                var bmp = new Bitmap(img.GetPath());
                var Bmp = bmp.Redimensionner(new Size(LgPx, HtPx));
                bmp.Dispose();
                var Dimensions = new Size(LgMM, HtMM); ;

                BitmapHelper.Verrouiller(Bmp);

                int i = 0;

                while (i < nbPoint)
                {
                    float fx = RandomHelper.Random.Next(LgMM - 8);
                    float fy = RandomHelper.Random.Next(HtMM - 8);

                    int gris = BitmapHelper.ValeurCanal(MathHelper.FloorToInt(fx), MathHelper.FloorToInt(fy), Canal.Luminosite);

                    if (gris > 2) continue;

                    if (RandomHelper.Random.Next(255) <= gris)
                    {
                        sites.Add(new PointF(fx + 4, fy + 4));
                        i++;
                    }
                }

                BitmapHelper.Liberer();

                Bmp.Dispose();
            }
            catch (Exception ex) { LogDebugging.Log.Message(ex); }

            return sites;
        }
    }
}
