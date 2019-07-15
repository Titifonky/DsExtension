using LogDebugging;
using System;
using System.Collections.Generic;
using System.Drawing;
using static Cmds.Poinconner.BitmapHelper;

namespace Cmds.Poinconner
{
    public static class BitmapPoissonSampler
    {
        private const int DefaultPointsPerIteration = 30;

        private static readonly float SquareRootTwo = (float)Math.Sqrt(2);

        private struct VecteurV
        {
            public float X;
            public float Y;
            public float Gris;
            public float FacteurGris;
            public float MinimumDistance;

            public VecteurV(float n)
            {
                X = n; Y = n;
                Gris = 0;
                FacteurGris = 0;
                MinimumDistance = Settings.MinimumDistance;
            }

            public VecteurV(float x, float y)
            {
                X = x; Y = y;
                Gris = 0;
                FacteurGris = 0;
                MinimumDistance = Settings.MinimumDistance;
            }

            public static float Distance(VecteurV v1, VecteurV v2)
            {
                return (float)Math.Sqrt(DistanceSquared(v1, v2));
            }

            public static float DistanceSquared(VecteurV v1, VecteurV v2)
            {
                return ((v2.X - v1.X) * (v2.X - v1.X)) + ((v2.Y - v1.Y) * (v2.Y - v1.Y));
            }

            public PointF GetPointF()
            {
                return new PointF(X, Y);
            }

            public void InitFacteurGris()
            {
                var nb = 0f;
                var gris = 0;
                var r2 = Settings.MinimumDistance * Settings.MinimumDistance;
                for (int i = 0; i <= (Settings.MinimumDistance * 2); i++)
                    for (int j = 0; j <= (Settings.MinimumDistance * 2); j++)
                    {
                        var x = X + i; var y = Y + j;
                        var dx = i - Settings.MinimumDistance; var dy = j - Settings.MinimumDistance;
                        if (x > 0 && x < Settings.Dimensions.Width && y > 0 && y < Settings.Dimensions.Height && ((dx * dx) + (dy * dy)) <= r2)
                        {
                            nb++;
                            gris += BitmapHelper.ValeurCanal((int)x, (int)y, BitmapHelper.Canal.Luminosite);
                        }
                    }

                if(nb > 0)
                    Gris = gris / nb;

                // On calcul l'inverse pour augment la taille dans les noirs et diminuer dans les blancs
                var f = (255 - Gris) / 255f;

                FacteurGris = f;
                MinimumDistance = Settings.MinimumDistance + Settings.MinimumDistance * f;
            }

            public static VecteurV operator +(VecteurV v1, VecteurV v2)
            {
                return new VecteurV(v1.X + v2.X, v1.Y + v2.Y);
            }

            public static VecteurV operator -(VecteurV v1, VecteurV v2)
            {
                return new VecteurV(v1.X - v2.X, v1.Y - v2.Y);
            }

            public static VecteurV operator /(VecteurV v1, float n)
            {
                return new VecteurV(v1.X / n, v1.Y / n);
            }
        }

        private static class Settings
        {
            public static Bitmap Bmp;
            public static PointF Center;
            public static SizeF Dimensions;
            public static float MinimumDistance;
            public static float CellSize;
            public static int GridWidth, GridHeight;
        }

        private static class State
        {
            public static VecteurV?[,] Grid;
            public static List<VecteurV> ActivePoints, Points;
        }

        public static List<PointF> Run(DraftSight.Interop.dsAutomation.ReferenceImage img, int nbPoint)
        {
            try
            {
                int LgMM = (int)img.Width;
                int HtMM = (int)img.Height;
                int LgPx = LgMM + 1;
                int HtPx = HtMM + 1;

                var bmp = new Bitmap(img.GetPath());
                Settings.Bmp = bmp.Redimensionner(new Size(LgPx, HtPx));
                bmp.Dispose();

                Settings.Dimensions = new SizeF(LgMM, HtMM);
                Settings.Center = new PointF(LgMM * 0.5f, HtMM * 0.5f);
                Settings.MinimumDistance = (float)(LgMM / Math.Sqrt(nbPoint / (HtMM / (double)LgMM))) * 0.7f;
                Log.Message("MinimumDistance : " + Settings.MinimumDistance);
                Settings.CellSize = Settings.MinimumDistance / SquareRootTwo;
                Settings.GridWidth = (int)(LgMM / Settings.CellSize) + 1;
                Settings.GridHeight = (int)(HtMM / Settings.CellSize) + 1;

                State.Grid = new VecteurV?[Settings.GridWidth, Settings.GridHeight];
                State.ActivePoints = new List<VecteurV>();
                State.Points = new List<VecteurV>();

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
            }
            catch (Exception ex) { Log.Message(ex); };

            var ListePoints = new List<PointF>();

            foreach (var pt in State.Points)
                ListePoints.Add(pt.GetPointF());

            return ListePoints;
        }

        private static void AddFirstPoint()
        {

            var d = RandomHelper.Random.NextDouble();
            var xr = Settings.Dimensions.Width * d;

            d = RandomHelper.Random.NextDouble();
            var yr = Settings.Dimensions.Height * d;

            var p = new VecteurV((float)xr, (float)yr);
            p.InitFacteurGris();

            var index = Denormalize(p);

            State.Grid[(int)index.X, (int)index.Y] = p;

            State.ActivePoints.Add(p);
            State.Points.Add(p);
        }

        private static bool AddNextPoint(VecteurV point)
        {
            var found = false;
            var q = GenerateRandomAround(point);

            if (q.X > 0 && q.X < Settings.Dimensions.Width && q.Y > 0 && q.Y < Settings.Dimensions.Height)
            {
                var qIndex = Denormalize(q);
                var tooClose = false;

                for (var i = (int)Math.Max(0, qIndex.X - 2); i < Math.Min(Settings.GridWidth, qIndex.X + 3) && !tooClose; i++)
                    for (var j = (int)Math.Max(0, qIndex.Y - 2); j < Math.Min(Settings.GridHeight, qIndex.Y + 3) && !tooClose; j++)
                        if (State.Grid[i, j].HasValue && VecteurV.Distance(State.Grid[i, j].Value, q) < State.Grid[i, j].Value.MinimumDistance)
                            tooClose = true;

                if (!tooClose)
                {
                    found = true;
                    q.InitFacteurGris();
                    State.ActivePoints.Add(q);
                    State.Points.Add(q);
                    State.Grid[(int)qIndex.X, (int)qIndex.Y] = q;
                }
            }
            return found;
        }

        private static VecteurV GenerateRandomAround(VecteurV center)
        {
            var d = RandomHelper.Random.NextDouble();
            
            var radius = center.MinimumDistance + (Settings.MinimumDistance * 2 * d);

            d = RandomHelper.Random.NextDouble();
            var angle = MathHelper.TwoPi * d;

            var newX = radius * Math.Sin(angle);
            var newY = radius * Math.Cos(angle);

            return new VecteurV((float)(center.X + newX), (float)(center.Y + newY));
        }

        private static VecteurV Denormalize(VecteurV point)
        {
            return new VecteurV((int)(point.X / Settings.CellSize), (int)(point.Y / Settings.CellSize));
        }

        private static class RandomHelper
        {
            public static readonly Random Random = new Random();
        }

        private static class MathHelper
        {
            public const float Pi = (float)Math.PI;
            public const float HalfPi = (float)(Math.PI / 2);
            public const float TwoPi = (float)(Math.PI * 2);
        }
    }
}
