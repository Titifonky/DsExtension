using LogDebugging;
using System;
using System.Collections.Generic;
using System.Drawing;
using static Cmds.Poinconner.BitmapHelper;

namespace Cmds.Poinconner
{
    public static class BitmapRejectionSampler
    {
        public static List<PointF> Run(DraftSight.Interop.dsAutomation.ReferenceImage img, int nbPoint, int niveauGrisMini = 2)
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

                int decal = 4;
                float decal2 = decal / 2f;

                BitmapHelper.Verrouiller(Bmp);

                int i = 0;

                while (i < nbPoint)
                {
                    float fx = RandomHelper.Random.Next(LgMM - decal);
                    float fy = RandomHelper.Random.Next(HtMM - decal);

                    int gris = BitmapHelper.ValeurCanal(MathHelper.FloorToInt(fx), MathHelper.FloorToInt(fy), Canal.Luminosite);

                    if (gris > niveauGrisMini && RandomHelper.Random.Next(255) <= gris)
                    {
                        sites.Add(new PointF(fx + decal2, fy + decal2));
                        i++;
                    }
                }

                BitmapHelper.Liberer();

                Bmp.Dispose();
            }
            catch (Exception ex) { Log.Message(ex); }

            return sites;
        }
    }
}
