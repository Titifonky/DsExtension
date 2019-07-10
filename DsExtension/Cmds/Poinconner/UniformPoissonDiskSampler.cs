using LogDebugging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using VoronoiLib;
using VoronoiLib.Structures;
using static Cmds.Poinconner.BitmapHelper;

namespace Cmds.Poinconner
{
    // Adapated from java source by Herman Tulleken
    // http://www.luma.co.za/labs/2008/02/27/poisson-disk-sampling/

    // The algorithm is from the "Fast Poisson Disk Sampling in Arbitrary Dimensions" paper by Robert Bridson
    // http://www.cs.ubc.ca/~rbridson/docs/bridson-siggraph07-poissondisk.pdf

    public static class PoissonDiskSampler
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
            public static Vecteur Center;
            public static Vecteur Dimensions;
            public static float JeuPoincon = 3;
            public static List<int> ListePoincons;
            public static float CellSize;
            public static int GridWidth, GridHeight;
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
            public static SitePoincon?[,] Grid;
            public static List<SitePoincon> ActivePoints, Poincons;
        }

        public static List<SitePoincon> SampleBitmapPoisson(DraftSight.Interop.dsAutomation.ReferenceImage img, List<int> listePoincons)
        {
            int LgMM = (int)img.Width;
            int HtMM = (int)img.Height;
            int LgPx = (int)(LgMM + 1);
            int HtPx = (int)(HtMM + 1);
            var bmp = new Bitmap(img.GetPath());
            Settings.Bmp = bmp.Redimensionner(new Size(LgPx, HtPx));
            bmp.Dispose();
            Settings.Dimensions = new Vecteur(LgMM, HtMM); ;
            Settings.Center = Settings.Dimensions / 2;
            Settings.CellSize = listePoincons.Max() / SquareRootTwo;
            Settings.GridWidth = (int)(Settings.Dimensions.X / Settings.CellSize) + 1;
            Settings.GridHeight = (int)(Settings.Dimensions.Y / Settings.CellSize) + 1;
            Settings.Histogram = BitmapHelper.Histogramme(Settings.Bmp);
            Settings.ListePoincons = listePoincons;
            Settings.CalculerPlagePoincon();

            State.Grid = new SitePoincon?[Settings.GridWidth, Settings.GridHeight];
            State.ActivePoints = new List<SitePoincon>();
            State.Poincons = new List<SitePoincon>();

            BitmapHelper.Verrouiller(Settings.Bmp);

            AddFirstPoint();

            while (State.ActivePoints.Count != 0)
            {
                var listIndex = RandomHelper.Random.Next(State.ActivePoints.Count);

                var point = State.ActivePoints[listIndex];
                var found = false;

                for (var k = 0; k < DefaultPointsPerIteration; k++)
                    found |= AddNextPoint(point);

                if (!found)
                    State.ActivePoints.RemoveAt(listIndex);
            }

            BitmapHelper.Liberer();

            Settings.Bmp.Dispose();

            return State.Poincons;
        }

        static void AddFirstPoint()
        {
            var added = false;
            while (!added)
            {
                var d = RandomHelper.Random.NextDouble();
                var xr = Settings.ListePoincons.Max() * 0.5 + (Settings.Dimensions.X - Settings.ListePoincons.Max()) * d;

                d = RandomHelper.Random.NextDouble();
                var yr = Settings.ListePoincons.Max() * 0.5 + (Settings.Dimensions.Y - Settings.ListePoincons.Max()) * d;

                var v = new Vecteur(xr, yr);
                added = true;

                var index = Denormalize(v);

                var p = new SitePoincon(v);

                State.Grid[(int)index.X, (int)index.Y] = p;

                State.ActivePoints.Add(p);
                State.Poincons.Add(p);
            }
        }

        static bool AddNextPoint(SitePoincon point)
        {
            var found = false;
            var q = GenerateRandomAround(point);

            if (q.X >= 0 && q.X < Settings.Dimensions.X &&
                q.Y > 0 && q.Y < Settings.Dimensions.Y)
            {
                var p = new SitePoincon(q);

                var tooClose = false;

                if (Vecteur.Distance(point.V, p.V) >= (point.Radius + p.Radius + Settings.JeuPoincon))
                {
                    var qIndex = Denormalize(q);

                    for (var i = (int)Math.Max(0, qIndex.X - 3); i < Math.Min(Settings.GridWidth, qIndex.X + 4) && !tooClose; i++)
                        for (var j = (int)Math.Max(0, qIndex.Y - 3); j < Math.Min(Settings.GridHeight, qIndex.Y + 4) && !tooClose; j++)
                            if (State.Grid[i, j].HasValue && Vecteur.Distance(State.Grid[i, j].Value.V, p.V) < (State.Grid[i, j].Value.Radius + p.Radius + Settings.JeuPoincon))
                                tooClose = true;

                    if (!tooClose)
                    {
                        Log.Message("Trouvé " + p.Poincon);
                        found = true;
                        State.ActivePoints.Add(p);
                        State.Poincons.Add(p);
                        State.Grid[(int)qIndex.X, (int)qIndex.Y] = p;
                    }
                }
            }

            return found;
        }

        static Vecteur GenerateRandomAround(SitePoincon center)
        {
            var d = RandomHelper.Random.NextDouble();
            var radius = center.Radius + Settings.ListePoincons.Min() * 0.5 + center.Delta * d * (center.Gris / (double)255);

            d = RandomHelper.Random.NextDouble();
            var angle = MathHelper.TwoPi * d;

            var newX = radius * Math.Sin(angle);
            var newY = radius * Math.Cos(angle);

            return new Vecteur(center.V.X + newX, center.V.Y + newY);
        }

        static Vecteur Denormalize(Vecteur point)
        {
            return new Vecteur((int)(point.X / Settings.CellSize), (int)(point.Y / Settings.CellSize));
        }
    }
}
