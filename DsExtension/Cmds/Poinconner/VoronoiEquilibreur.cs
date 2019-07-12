using LogDebugging;
using System;
using System.Collections.Generic;
using System.Drawing;
using VoronoiMap;
using static Cmds.Poinconner.BitmapHelper;

namespace Cmds.Poinconner
{
    public class ParametrePoincon
    {
        public int NbPoints = 3000;
        public float JeuMiniEntrePoincons = 3;
        public float? JeuVariable = 0;
        public float? DiametrePoinconMax = null;
        public int NbEquilibrage = 3;

    }

    public static class VoronoiEquilibreur
    {
        public struct SitePoincon
        {
            public Site Site;
            public double Poincon;
            public SitePoincon(Site s, double diam)
            {
                Site = s;
                Poincon = diam;
            }
        }

        private static class Settings
        {
            public static Bitmap Bmp;
            public static Size Dimensions;
            public static float JeuPoincon = 3;
            public static VoronoiGraph Graph;
            //public static Dictionary<Canal, int[]> Histogram;
        }

        public static List<SitePoincon> Start(DraftSight.Interop.dsAutomation.ReferenceImage img, List<PointF> liste, int jeu, out VoronoiGraph graph)
        {
            List<SitePoincon> listepoincon = null;

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
                Settings.JeuPoincon = jeu;
                //Settings.Histogram = BitmapHelper.Histogramme(Settings.Bmp);

                BitmapHelper.Verrouiller(Settings.Bmp);


                Settings.Graph = VoronoiGraph.ComputeVoronoiGraph(liste, LgMM, HtMM, false);

                for (int k = 0; k < 4; k++)
                {
                    liste = Equilibrer();
                    Settings.Graph = VoronoiGraph.ComputeVoronoiGraph(liste, LgMM, HtMM, false);
                }

                listepoincon = CalculerPoincon();

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
                var l = site.Region;

                var enveloppe = site.Polygon.Enveloppe;
                double xSum = 0, ySum = 0, pSum = 0;

                for (int x = 0; x < enveloppe.Width; x++)
                    for (int y = 0; y < enveloppe.Height; y++)
                    {
                        var pt = new PointF(enveloppe.X + x, enveloppe.Y + y);
                        if (site.Polygon.InPolygon(pt))
                        {
                            var g = BitmapHelper.ValeurCanal((int)pt.X, (int)pt.Y, Canal.Luminosite);
                            xSum += g * x;
                            ySum += g * y;
                            pSum += g;
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

        private static List<SitePoincon> CalculerPoincon()
        {
            var liste = new List<SitePoincon>();

            foreach (var site in Settings.Graph.Sites)
            {
                var lgMin = site.MinDistToNearestEdge;

                if (!float.IsNaN(lgMin))
                    liste.Add(new SitePoincon(site, (lgMin - (Settings.JeuPoincon * 0.5)) * 2));
            }

            return liste;
        }
    }
}
