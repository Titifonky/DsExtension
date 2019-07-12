using Cmds.Poinconner;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace AwesomeNamespace
{
    // Adapated from java source by Herman Tulleken
    // http://www.luma.co.za/labs/2008/02/27/poisson-disk-sampling/

    // The algorithm is from the "Fast Poisson Disk Sampling in Arbitrary Dimensions" paper by Robert Bridson
    // http://www.cs.ubc.ca/~rbridson/docs/bridson-siggraph07-poissondisk.pdf

    public static class UniformPoissonDiskSampler
    {
        private const int DefaultPointsPerIteration = 30;

        private static readonly float SquareRootTwo = (float)Math.Sqrt(2);

        private struct VecteurV
        {
            public float X;
            public float Y;

            public VecteurV(float n)
            {
                X = n; Y = n;
            }

            public VecteurV(float x, float y)
            {
                X = x; Y = y;
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

            public static VecteurV operator +(VecteurV v1, VecteurV v2)
            {
                return new VecteurV(v2.X + v1.X, v2.Y + v1.Y);
            }

            public static VecteurV operator -(VecteurV v1, VecteurV v2)
            {
                return new VecteurV(v2.X - v1.X, v2.Y - v1.Y);
            }

            public static VecteurV operator /(VecteurV v1, float n)
            {
                return new VecteurV(v1.X / n, v1.Y / n);
            }
        }

        private static class Settings
        {
            public static Bitmap Bmp;
            public static VecteurV TopLeft, LowerRight, Center;
            public static VecteurV Dimensions;
            public static float? RejectionSqDistance;
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
            int LgMM = (int)img.Width;
            int HtMM = (int)img.Height;
            int LgPx = LgMM + 1;
            int HtPx = HtMM + 1;

            var bmp = new Bitmap(img.GetPath());
            Settings.Bmp = bmp.Redimensionner(new Size(LgPx, HtPx));
            bmp.Dispose();

            Settings.TopLeft = new VecteurV(0, HtMM);
            Settings.LowerRight = new VecteurV(LgMM, 0);
            Settings.Dimensions = Settings.LowerRight - Settings.TopLeft;
            Settings.Center = (Settings.TopLeft + Settings.LowerRight) / 2;
            Settings.MinimumDistance = (LgMM * nbPoint / (LgMM * HtMM)) * 0.3f;
            Settings.CellSize = Settings.MinimumDistance / SquareRootTwo;
            Settings.GridWidth = (int)(Settings.Dimensions.X / Settings.CellSize) + 1;
            Settings.GridHeight = (int)(Settings.Dimensions.Y / Settings.CellSize) + 1;

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

            var ListePoints = new List<PointF>();

            foreach (var pt in State.Points)
                ListePoints.Add(pt.GetPointF());

            return ListePoints;
        }

        private static void AddFirstPoint()
        {
            var added = false;
            while (!added)
            {
                var d = RandomHelper.Random.NextDouble();
                var xr = Settings.TopLeft.X + Settings.Dimensions.X * d;

                d = RandomHelper.Random.NextDouble();
                var yr = Settings.TopLeft.Y + Settings.Dimensions.Y * d;

                var p = new VecteurV((float)xr, (float)yr);
                if (Settings.RejectionSqDistance != null && VecteurV.DistanceSquared(Settings.Center, p) > Settings.RejectionSqDistance)
                    continue;
                added = true;

                var index = Denormalize(p);

                State.Grid[(int)index.X, (int)index.Y] = p;

                State.ActivePoints.Add(p);
                State.Points.Add(p);
            }
        }

        private static bool AddNextPoint(VecteurV point)
        {
            var found = false;
            var q = GenerateRandomAround(point, BitmapHelper.ValeurCanal((int)point.X, (int)point.Y, BitmapHelper.Canal.Luminosite));

            if (q.X >= Settings.TopLeft.X && q.X < Settings.LowerRight.X &&
                q.Y > Settings.TopLeft.Y && q.Y < Settings.LowerRight.Y)
            {
                var qIndex = Denormalize(q);
                var tooClose = false;

                for (var i = (int)Math.Max(0, qIndex.X - 2); i < Math.Min(Settings.GridWidth, qIndex.X + 3) && !tooClose; i++)
                    for (var j = (int)Math.Max(0, qIndex.Y - 2); j < Math.Min(Settings.GridHeight, qIndex.Y + 3) && !tooClose; j++)
                        if (State.Grid[i, j].HasValue && VecteurV.Distance(State.Grid[i, j].Value, q) < Settings.MinimumDistance)
                            tooClose = true;

                if (!tooClose)
                {
                    found = true;
                    State.ActivePoints.Add(q);
                    State.Points.Add(q);
                    State.Grid[(int)qIndex.X, (int)qIndex.Y] = q;
                }
            }
            return found;
        }

        private static VecteurV GenerateRandomAround(VecteurV center, int gris)
        {
            var d = RandomHelper.Random.NextDouble();
            var radius = Settings.MinimumDistance + Settings.MinimumDistance / gris;

            d = RandomHelper.Random.NextDouble();
            var angle = MathHelper.TwoPi * d;

            var newX = radius * Math.Sin(angle);
            var newY = radius * Math.Cos(angle);

            return new VecteurV((float)(center.X + newX), (float)(center.Y + newY));
        }

        private static VecteurV Denormalize(VecteurV point)
        {
            return new VecteurV((int)((point.X - Settings.TopLeft.X) / Settings.CellSize), (int)((point.Y - Settings.TopLeft.Y) / Settings.CellSize));
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
