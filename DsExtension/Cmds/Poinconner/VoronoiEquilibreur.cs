using LogDebugging;
using System;
using System.Collections.Generic;
using System.Drawing;
using VoronoiMap;
using static Cmds.Poinconner.BitmapHelper;

namespace Cmds.Poinconner
{
    public static class VoronoiEquilibreur
    {
        public struct Cellule
        {
            public Site Site;
            public double CercleInscrit;
            public float GrisCercleInscrit;
            public float GrisCellule;
            public Cellule(Site s, double diam)
            {
                Site = s;
                CercleInscrit = diam;
                GrisCercleInscrit = 0;
                GrisCellule = 0;
                InitFacteurGris();
                InitGrisCellule();
            }

            private void InitFacteurGris()
            {
                var nb = 0f;
                var gris = 0;
                var r2 = (CercleInscrit * CercleInscrit) * 0.25;
                for (int i = 0; i <= CercleInscrit; i++)
                    for (int j = 0; j <= CercleInscrit; j++)
                    {
                        var x = Site.X + i; var y = Site.Y + j;
                        var dx = i - (CercleInscrit * 0.5); var dy = j - (CercleInscrit * 0.5);
                        if (x > 0 && x < Settings.Dimensions.Width && y > 0 && y < Settings.Dimensions.Height && ((dx * dx) + (dy * dy)) <= r2)
                        {
                            nb++;
                            gris += BitmapHelper.ValeurCanal((int)x, (int)y, BitmapHelper.Canal.Luminosite);
                        }
                    }

                if(nb > 0)
                    GrisCercleInscrit = gris / nb;
            }

            private void InitGrisCellule()
            {
                var nb = 0f;
                var gris = 0;

                var enveloppe = Site.Polygon.Enveloppe;

                for (int x = 0; x < enveloppe.Width; x++)
                    for (int y = 0; y < enveloppe.Height; y++)
                    {
                        var pt = new PointF(enveloppe.X + x, enveloppe.Y + y);
                        if (Site.Polygon.InPolygon(pt))
                        {
                            nb++;
                            gris += BitmapHelper.ValeurCanal((int)x, (int)y, BitmapHelper.Canal.Luminosite);
                        }
                    }

                if (nb > 0)
                    GrisCellule = gris / nb;
            }
        }

        private static class Settings
        {
            public static Bitmap Bmp;
            public static Size Dimensions;
            public static VoronoiGraph Graph;
            //public static Dictionary<Canal, int[]> Histogram;
        }

        public static List<Cellule> Start(DraftSight.Interop.dsAutomation.ReferenceImage img, List<PointF> liste, int nbEquilibrage, out VoronoiGraph graph)
        {
            List<Cellule> listepoincon = null;

            try
            {
                int LgMM = (int)img.Width;
                int HtMM = (int)img.Height;
                int LgPx = LgMM + 1;
                int HtPx = HtMM + 1;
                var bmp = new Bitmap(img.GetPath());
                Settings.Bmp = bmp.Redimensionner(new Size(LgPx, HtPx));
                bmp.Dispose();
                Settings.Dimensions = new Size(LgMM, HtMM); ;
                //Settings.Histogram = BitmapHelper.Histogramme(Settings.Bmp);

                BitmapHelper.Verrouiller(Settings.Bmp);


                Settings.Graph = VoronoiGraph.ComputeVoronoiGraph(liste, LgMM, HtMM, false);

                for (int k = 0; k < nbEquilibrage; k++)
                {
                    liste = Equilibrer();
                    Settings.Graph = VoronoiGraph.ComputeVoronoiGraph(liste, LgMM, HtMM, false);
                }

                listepoincon = CalculerCellule();

                BitmapHelper.Liberer();

                Settings.Bmp.Dispose();
            }
            catch (Exception ex) { LogDebugging.Log.Message(ex); }

            graph = Settings.Graph;
            return listepoincon;
        }

        private static List<PointF> Equilibrer()
        {
            var liste = new List<PointF>();
            foreach (var site in Settings.Graph.Sites)
            {
                var enveloppe = site.Polygon.Enveloppe;
                double xSum = 0, ySum = 0, pSum = 0;

                for (int x = 0; x < enveloppe.Width; x++)
                    for (int y = 0; y < enveloppe.Height; y++)
                    {
                        var pt = new PointF(enveloppe.X + x, enveloppe.Y + y);
                        if (site.Polygon.InPolygon(pt))
                        {
                            var gris = BitmapHelper.ValeurCanal((int)pt.X, (int)pt.Y, Canal.Luminosite);

                            xSum += gris * x;
                            ySum += gris * y;
                            pSum += gris;
                        }
                    }

                if (pSum > 0)
                {
                    xSum /= pSum;
                    ySum /= pSum;
                }

                liste.Add(new PointF((float)(enveloppe.X + xSum), (float)(enveloppe.Y + ySum)));
            }

            return liste;
        }

        private static List<Cellule> CalculerCellule()
        {
            var liste = new List<Cellule>();

            foreach (var site in Settings.Graph.Sites)
            {
                var lgMin = site.MinDistToNearestEdge;

                if (!float.IsNaN(lgMin))
                    liste.Add(new Cellule(site, lgMin * 2));
            }

            return liste;
        }
    }
}
