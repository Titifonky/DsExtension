using LogDebugging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using VoronoiLib;
using VoronoiLib.Structures;
using static Cmds.Poinconner.BitmapHelper;

namespace Cmds.Poinconner
{
    public static class VoronoiSampler
    {
        public const int DefaultPointsPerIteration = 30;

        static readonly float SquareRootTwo = (float)Math.Sqrt(2);

        public struct Vecteur
        {
            public float X;
            public float Y;

            public Vecteur(float x, float y)
            {
                X = x;
                Y = y;
            }

            public Vecteur(double x, double y)
            {
                X = (float)x;
                Y = (float)y;
            }

            public static float Distance(Vecteur value1, Vecteur value2)
            {
                return (float)Math.Sqrt(DistanceSquared(value1, value2));
            }
            public static float DistanceSquared(Vecteur value1, Vecteur value2)
            {
                return ((value2.X - value1.X) * (value2.X - value1.X)) + ((value2.Y - value1.Y) * (value2.Y - value1.Y));
            }

            public static Vecteur operator +(Vecteur left, Vecteur right)
            {
                return new Vecteur(left.X + right.X, left.Y + right.Y);
            }
            public static Vecteur operator -(Vecteur left, Vecteur right)
            {
                return new Vecteur(left.X - right.X, left.Y - right.Y);
            }

            public static Vecteur operator /(Vecteur vector, float scale)
            {
                return new Vecteur(vector.X / scale, vector.Y / scale);
            }
        }

        public struct SitePoincon
        {
            public Vecteur V;
            public float Poincon;
            public float Radius;
            public float MaxRadius;
            public float Delta;
            public int Gris;

            public SitePoincon(Vecteur v)
            {
                V = v;
                Gris = BitmapHelper.ValeurCanal((int)Math.Floor(V.X + 0.5), (int)Math.Floor(V.Y + 0.5), Canal.Luminosite);
                Poincon = Settings.GetPoincon(Gris);
                Radius = (float)(Poincon * 0.5);
                MaxRadius = Radius + 4 * Poincon;
                Delta = MaxRadius - Radius;
            }

            public SitePoincon(Vecteur v, int gris)
            {
                V = v;
                Gris = gris;
                Poincon = Settings.GetPoincon(Gris);
                Radius = (float)(Poincon * 0.5);
                MaxRadius = Radius + 4 * Poincon;
                Delta = MaxRadius - Radius;
            }
        }

        private static class Settings
        {
            public static Bitmap Bmp;
            public static Vecteur Dimensions;
            public static float JeuPoincon = 3;
            public static List<int> ListePoincons;
            public static Dictionary<Canal, int[]> Histogram;

            public static void CalculerPlagePoincon()
            {
                plage = Math.Floor(255 / (Double)ListePoincons.Count);
            }

            private static double plage;
            public static int GetPoincon(int grey)
            {
                return ListePoincons[Math.Min((int)Math.Floor(grey / plage), ListePoincons.Count - 1)];
            }

            public static int GetPoincon(float x, float y)
            {
                return GetPoincon(BitmapHelper.ValeurCanal((int)Math.Floor(x + 0.5), (int)Math.Floor(y + 0.5), Canal.Luminosite));
            }
        }

        private static class State
        {
            public static List<FortuneSite> Site;
            public static LinkedList<VEdge> Maillage;
        }

        public static List<FortuneSite> SampleBitmapRejection(DraftSight.Interop.dsAutomation.ReferenceImage img, int nbPoint, List<int> listePoincons)
        {
            int LgMM = (int)img.Width;
            int HtMM = (int)img.Height;
            int LgPx = (int)(LgMM + 1);
            int HtPx = (int)(HtMM + 1);
            var bmp = new Bitmap(img.GetPath());
            Settings.Bmp = bmp.Redimensionner(new Size(LgPx, HtPx));
            bmp.Dispose();
            Settings.Dimensions = new Vecteur(LgMM, HtMM); ;
            Settings.Histogram = BitmapHelper.Histogramme(Settings.Bmp);
            Settings.ListePoincons = listePoincons;

            BitmapHelper.Verrouiller(Settings.Bmp);

            int i = 0;

            while (i < nbPoint)
            {
                
                float fx = RandomHelper.Random.Next(LgMM);
                float fy = RandomHelper.Random.Next(HtMM);

                int gris = BitmapHelper.ValeurCanal(MathHelper.Floor(fx), MathHelper.Floor(fy), Canal.Luminosite);

                if (RandomHelper.Random.Next(255) <= gris)
                {
                    State.Site.Add(new FortuneSite(fx, fy));
                    i++;
                }
            }

            State.Maillage = FortunesAlgorithm.Run(State.Site, 0, 0, LgMM, HtMM);

            BitmapHelper.Liberer();

            Settings.Bmp.Dispose();

            return State.Site;
        }

        public static void Equilibrer()
        {
            List<FortuneSite> ListeEquilibre = new List<FortuneSite>();
            Parallel.ForEach(State.Site, s =>
            {

            });
        }
    }
}
