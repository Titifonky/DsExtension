using DraftSight.Interop.dsAutomation;
using LogDebugging;
using System;
using System.Collections.Generic;
using static Cmds.GeometrieHelper;

namespace Cmds
{
    public class CmdSimplifierSpline : CmdBase
    {
        public CmdSimplifierSpline(DraftSight.Interop.dsAutomation.Application app, string groupName) : base(app, groupName)
        { }

        public override string globalName() { return "_MSimplifierSpline"; }
        public override string localName() { return "MSimplifierSpline"; }

        public override string Description() { return "Simplifier les splines en arc pour le laser"; }
        public override string ItemName() { return "Simplifier Spline"; }

        public override void Execute()
        {
            try
            {
                MathHelper.Sm = DsApp.GetActiveDocument().GetModel().GetSketchManager();

                DsApp.AbortRunningCommand();

                CommandMessage CmdLine = DsApp.GetCommandMessage();
                if (null == CmdLine) return;

                //Entree utilisateur
                double toleranceEcart = 0.1, toleranceAngle = 5, toleranceEcartMax = 0.1;
                bool methodeCalcul = true, toutesLesSplines = true, supprimerOriginal = true;

                // Erreur avec le premier prompt, la commande est automatiquement annul�e
                // Pour contourner le pb, on lance une commande � vide.
                {
                    String sss = "";
                    CmdLine.PromptForString(true, "", "", out sss);
                }

                string[] KeyWord = new string[] { "Oui", "Non", "Distance", "Max", "Angle", "Calcul", "SupprimerOriginal"};

                Action<string> ModifierOptions = delegate (string s)
                {
                    if(s == "Distance")
                        CmdLine.PromptForDouble("Deviation de distance par rapport � la spline ", toleranceEcart, out toleranceEcart);
                    else if (s == "Max")
                        CmdLine.PromptForDouble("Deviation de distance maximum absolue ", toleranceEcartMax, out toleranceEcartMax);
                    else if (s == "Angle")
                        CmdLine.PromptForDouble("Deviation d'angle par rapport � la spline ", toleranceAngle, out toleranceAngle);
                    else if (s == "Calcul")
                        CmdLine.PromptForBool("Deviation en pourcent de la longueur ", "Oui", "Non", methodeCalcul, out methodeCalcul);
                    else if (s == "SupprimerOriginal")
                        CmdLine.PromptForBool("Supprimer l'original ", "Oui", "Non", supprimerOriginal, out supprimerOriginal);
                };

                string Option;
                dsPromptResultType_e result;
                CmdLine.PromptExplanation = "Oui";
                do
                {
                    result = CmdLine.PromptForBoolOrKeyword("Convertir toutes les splines", "Erreur", KeyWord, KeyWord, (int)dsPromptInit_e.dsPromptInit_NoDefault, "Oui", "Non", true, out Option, out toutesLesSplines);
                    if (result == dsPromptResultType_e.dsPromptResultType_Keyword)
                        ModifierOptions(Option);

                } while (!((result == dsPromptResultType_e.dsPromptResultType_Value) || (result == dsPromptResultType_e.dsPromptResultType_Cancel)));

                if (result == dsPromptResultType_e.dsPromptResultType_Cancel)
                    return;

                Document DsDoc = DsApp.GetActiveDocument();
                Model Mdl = DsDoc.GetModel();
                SketchManager SkMgr = Mdl.GetSketchManager();
                SelectionManager SlMgr = DsDoc.GetSelectionManager();
                SelectionFilter SlFilter;
                SlFilter = SlMgr.GetSelectionFilter();

                object[] TabEntites = null;

                if (toutesLesSplines)
                {
                    SlFilter.Clear();
                    SlFilter.AddEntityType(dsObjectType_e.dsSplineType);
                    SlFilter.Active = true;

                    object ObjType = null;
                    object ObjEntites = null;
                    string[] TabNomsCalques = new string[] { "0" };
                    SkMgr.GetEntities(SlFilter, TabNomsCalques, out ObjType, out ObjEntites);
                    TabEntites = ObjEntites as object[];
                }
                else
                {
                    SlFilter.Clear();
                    SlFilter.AddEntityType(dsObjectType_e.dsSplineType);
                    SlFilter.Active = true;

                    if(CmdLine.PromptForSelection(false, "Selectionnez la spline", "Ce n'est pas une spline"))
                    {
                        dsObjectType_e entityType;
                        var count = SlMgr.GetSelectedObjectCount(dsSelectionSetType_e.dsSelectionSetType_Previous);
                        TabEntites = new object[count];

                        for (int i = 0; i < count; i++)
                        {
                            object selectedEntity = SlMgr.GetSelectedObject(dsSelectionSetType_e.dsSelectionSetType_Previous, i, out entityType);

                            if (dsObjectType_e.dsSplineType == entityType)
                            {
                                TabEntites[i] = selectedEntity;
                            }
                        }
                    }
                }

                var DateTimeStart = DateTime.Now;

                if (TabEntites != null && TabEntites.Length > 0)
                {
                    foreach (Spline Spline in TabEntites)
                    {
                        SplineConverter SplConverter = new SplineConverter(
                            Spline,
                            toleranceEcart,
                            toleranceAngle,
                            toleranceEcartMax,
                            methodeCalcul ? SplineConverter.eCalculTolerance.Pourcent : SplineConverter.eCalculTolerance.Absolu
                            );
                        iLine? L = SplConverter.EnLigne();
                        if (L != null)
                        {
                            var l = (iLine)L;
                            SkMgr.InsertLine(l.P1.X, l.P1.Y, l.P1.Z, l.P2.X, l.P2.Y, l.P2.Z);
                        }
                        else
                        {
                            var List = SplConverter.EnPolyligne();
                            foreach (var arc in List)
                                SkMgr.InsertArcBy3Points(arc.P1.MathPoint(), arc.P2.MathPoint(), arc.P3.MathPoint());
                        }
                    }

                    CmdLine.PrintLine(TabEntites.Length + " spline(s) convertis");

                    if (supprimerOriginal)
                    {
                        for (int i = 0; i < TabEntites.Length; i++)
                        {
                            Spline s = (Spline)TabEntites[i];
                            s.Erased = true;
                        }
                    }
                }

                TimeSpan t = DateTime.Now - DateTimeStart;
                CmdLine.PrintLine(String.Format("Execut� en {0}", GetSimplestTimeSpan(t)));

            }
            catch (Exception e)
            { Log.Write(e); }
        }

        private string[] GetTabNomsCalques(Document dsDoc)
        {
            LayerManager dsLayerManager = dsDoc.GetLayerManager();

            object[] dsLayers = (object[])dsLayerManager.GetLayers();
            string[] layerNames = new string[dsLayers.Length];

            for (int index = 0; index < dsLayers.Length; ++index)
            {
                Layer dsLayer = dsLayers[index] as Layer;
                layerNames[index] = dsLayer.Name;
            }

            return layerNames;
        }
    }

    class SplineConverter
    {
        public enum eCalculTolerance
        {
            Absolu = 1,
            Pourcent = 2
        }

        public Spline Spline { get; set; }

        public Double Lg { get; private set; }
        public iPointOnSpline StartPoint { get; private set; }
        public iPointOnSpline EndPoint { get; private set; }
        public List<iPoint> ListePointControl
        {
            get
            {
                if (_ListePointControl == null)
                    _ListePointControl = Spline.ListeControlPoint();

                return _ListePointControl;
            }
        }
        private List<iPoint> _ListePointControl;

        public Double ToleranceEcart { get; private set; }
        public Double ToleranceAngle { get; private set; }
        public Double ToleranceEcartMax { get; private set; }
        public eCalculTolerance CalculTolerance { get; private set; }
        public Double Pas { get; private set; }

        public SplineConverter(Spline spline, Double toleranceEcart, Double toleranceAngle, Double toleranceEcartMax, eCalculTolerance calcul = eCalculTolerance.Absolu, Double pas = 0.1)
        {
            Spline = spline;
            ToleranceEcart = toleranceEcart; ToleranceAngle = toleranceAngle; ToleranceEcartMax = toleranceEcartMax;
            CalculTolerance = calcul; Pas = pas;

            Lg = spline.GetLength();

            Double sP, eP;
            Spline.GetEndParams(out sP, out eP);
            StartPoint = Spline.PointOnSplineParam(sP);
            EndPoint = Spline.PointOnSplineParam(eP);
            CalculerDeviation();
        }

        private Double deviation;

        private void CalculerDeviation()
        {
            deviation = ToleranceEcart;
            if (CalculTolerance == eCalculTolerance.Pourcent)
                if (ToleranceEcartMax == 0)
                    deviation = Lg * ToleranceEcart / 100;
                else
                    deviation = Math.Min(Lg * ToleranceEcart / 100, ToleranceEcartMax);
        }

        /// <summary>
        /// Converti la spline en ligne si elle rentre dans la tol�rance
        /// </summary>
        /// <param name="Absolu">En absolu ou pourcentage de la longueur de la spline</param>
        /// <returns></returns>
        public iLine? EnLigne()
        {
            try
            {
                // Si les points de control sont align�s dans la tol�rance, on remplace par une ligne
                bool EstDroite = true;
                {
                    // Recup�ration des points de control
                    var LstControlPoint = Spline.ListeControlPoint();

                    // On d�fini la ligne reliant le d�but et la fin de la spline
                    var s = LstControlPoint[0];
                    var e = LstControlPoint[LstControlPoint.Count - 1];
                    MathLine ligne = MathHelper.Mu.CreateLine(s.X, s.Y, s.Z, e.X, e.Y, e.Z, dsMathLineType_e.dsMathLineType_Bounded);

                    // On supprime les points de d�part et d'arriv�e de la liste
                    LstControlPoint.RemoveAt(0);
                    LstControlPoint.RemoveAt(LstControlPoint.Count - 1);

                    // On v�rifie la d�viation de chaque point par rapport � la ligne.
                    // Si elle est sup�rieur, on sort
                    foreach (var iPt in LstControlPoint)
                    {
                        MathPoint dsResultPoint1, dsResultPoint2;
                        var d = MathHelper.Mu.Distance(iPt.MathPoint(), ligne, out dsResultPoint1, out dsResultPoint2);
                        if (d > deviation)
                        {
                            EstDroite = false;
                            break;
                        }
                    }
                }

                // Si la spline est droite, on la remplace par une ligne.
                if (EstDroite)
                {
                    return new iLine(StartPoint, EndPoint);
                }
            }
            catch (Exception e)
            { Log.Write(e); }

            return null;
        }

        public List<iArc> EnPolyligne()
        {
            List<iArc> LstArc = new List<iArc>();
            try
            {
                // Si la longueur de la spline est inferieure ou �gale � 3 * le pas
                if (Lg <= (Pas * 3))
                    LstArc.Add(
                        ArcFromPoints(
                        Spline.PointOnSplineDistance(0),
                        Spline.PointOnSplineDistance(Lg * 0.5),
                        Spline.PointOnSplineDistance(Lg)
                        )
                        );
                // Sinon on parcourt la spline
                else
                {
                    var arc = ChercherArc(0.0);
                    while (arc != null)
                    {
                        var a = (iArc)arc;
                        LstArc.Add(a);
                        arc = ChercherArc(a.P3.Distance);
                    }
                }
            }
            catch (Exception e)
            { Log.Write(e); }

            return LstArc;
        }

        private iArc? ChercherArc(double pos)
        {
            iPointOnSpline _p1, _p2, _p3;
            List<iPointOnSpline> _LstPoint = new List<iPointOnSpline>();
            // Reste � parcourir
            var _diff = Lg - pos;

            // Si la diff�rence == 0 on ne renvoi rien
            if (_diff == 0.0) return null;

            // Si la diff�rence est inferieur � 2*Pas on retourne l'arc
            if (_diff <= (Pas * 2.0))
            {
                return ArcFromPoints(
                    Spline.PointOnSplineDistance(pos),
                    Spline.PointOnSplineDistance(pos + _diff * 0.5),
                    Spline.PointOnSplineDistance(pos + _diff));
            }
            else
            {
                _p1 = Spline.PointOnSplineDistance(pos);
                _p2 = Spline.PointOnSplineDistance(pos + Pas);
                _p3 = Spline.PointOnSplineDistance(pos + (2 * Pas));
                pos += 2 * Pas;
            }

            iArc _arc = ArcFromPoints(_p1, _p2, _p3);
            _LstPoint.Add(_p1); _LstPoint.Add(_p2); _LstPoint.Add(_p3);

            iArc _LastArc = _arc;

            Func<bool> TestDeviationAngle = delegate ()
            {
                if (ToleranceAngle != 0)
                {
                    if ((_arc.DeviationP1() > ToleranceAngle) || (_arc.DeviationP3() > ToleranceAngle))
                        return true;
                }

                return false;
            };

            Func<bool> TestDeviationDistance = delegate ()
            {
                for (int _p = 1; _p < _LstPoint.Count - 1; _p++)
                {
                    if (_arc.DistanceDe(_LstPoint[_p].Point) > deviation)
                        return true;
                }

                return false;
            };

            do
            {
                Boolean _Exit = false;

                if (!(_Exit = TestDeviationAngle()))
                    _Exit = TestDeviationDistance();

                if (_Exit)
                {
                    //Action<iPointOnSpline> TracerDerive1 = delegate (iPointOnSpline pt)
                    //{
                    //    MathHelper.Sm.InsertLine(pt.X, pt.Y, pt.Z, pt.X + pt.Derivee1.X * 10, pt.Y + pt.Derivee1.Y * 10, pt.Z + pt.Derivee1.Z * 10);
                    //};

                    //Action<iPointOnSpline> TracerDerive2 = delegate (iPointOnSpline pt)
                    //{
                    //    MathHelper.Sm.InsertLine(pt.X, pt.Y, pt.Z, pt.X + pt.Derivee2.X * 10, pt.Y + pt.Derivee2.Y * 10, pt.Z + pt.Derivee2.Z * 10);
                    //};

                    //TracerDerive1(_LastArc.P1);
                    //TracerDerive1(_LastArc.P3);
                    //TracerDerive2(_LastArc.P1);
                    //TracerDerive2(_LastArc.P3);
                    return _LastArc;
                }
                else
                {
                    if (pos == Lg)
                        break;
                    else if ((Lg - pos) < Pas)
                        pos = Lg;
                    else
                        pos += Pas;

                    _p3 = Spline.PointOnSplineDistance(pos);
                    _LstPoint.Add(_p3);
                    _p2 = _LstPoint[_LstPoint.Count / 2];

                    if (_LstPoint.Count % 2 == 0)
                        _p2 = Spline.PointOnSplineDistance((_p3.Distance + _p1.Distance) * 0.5);

                    _LastArc = _arc;
                    _arc = ArcFromPoints(_p1, _p2, _p3);
                }


            } while (pos <= Lg);

            return _arc;
        }
    }

    static class MathHelper
    {
        public static MathUtility Mu = null;

        public static SketchManager Sm = null;

        public static void Init(MathUtility mu)
        {
            Mu = mu;
        }

        public static MathPoint MathPoint(this iPoint pt)
        {
            return Mu.CreatePoint(pt.X, pt.Y, pt.Z);
        }

        public static MathPoint MathPoint(this iPointOnSpline pt)
        {
            return Mu.CreatePoint(pt.X, pt.Y, pt.Z);
        }

        public static MathVector MathVector(this iVecteur v)
        {
            return Mu.CreateVector(v.X, v.Y, v.Z);
        }
    }

    static class SplineHelper
    {
        public static List<iPoint> ListeControlPoint(this Spline spl)
        {
            var lst = new List<iPoint>();
            for (int i = 0; i < spl.GetControlPointsCount(); i++)
            {
                double X, Y, Z;
                spl.GetControlPointCoordinate(i, out X, out Y, out Z);
                lst.Add(new iPoint(X, Y, Z));
            }

            return lst;
        }

        public static Eval EvalParam(this Spline spl, Double param)
        {
            double X, Y, Z, D; object fD, sD;
            spl.EvaluateAtParameter(param, out X, out Y, out Z, out D, out fD, out sD);
            var ev = new Eval();
            ev.Distance = D;
            ev.PointOnSpline = new iPointOnSpline(X, Y, D, new iVecteur((Double[])fD), new iVecteur((Double[])sD));
            ev.Point = new iPoint(X, Y, Z);
            ev.Param = param;
            ev.Derivee1 = new iVecteur((Double[])fD);
            ev.Derivee2 = new iVecteur((Double[])sD);
            return ev;
        }

        public static Eval EvalDistance(this Spline spl, Double dist)
        {
            double X, Y, Z, P; object fD, sD;
            spl.EvaluateAtDistance(dist, out X, out Y, out Z, out P, out fD, out sD);
            var ev = new Eval();
            ev.Distance = dist;
            ev.PointOnSpline = new iPointOnSpline(X, Y, dist, new iVecteur((Double[])fD), new iVecteur((Double[])sD));
            ev.Point = new iPoint(X, Y, Z);
            ev.Param = P;
            ev.Derivee1 = new iVecteur((Double[])fD);
            ev.Derivee2 = new iVecteur((Double[])sD);
            return ev;
        }

        public static iPointOnSpline PointOnSplineParam(this Spline spl, Double param)
        {
            double X, Y, Z, D; object fD, sD;
            spl.EvaluateAtParameter(param, out X, out Y, out Z, out D, out fD, out sD);
            return new iPointOnSpline(X, Y, D, new iVecteur((Double[])fD), new iVecteur((Double[])sD));
        }

        public static iPointOnSpline PointOnSplineDistance(this Spline spl, Double dist)
        {
            double X, Y, Z, P; object fD, sD;
            spl.EvaluateAtDistance(dist, out X, out Y, out Z, out P, out fD, out sD);
            return new iPointOnSpline(X, Y, dist, new iVecteur((Double[])fD), new iVecteur((Double[])sD));
        }

        public struct Eval
        {
            public Double Distance;
            public iPoint Point;
            public iPointOnSpline PointOnSpline;
            public Double Param;
            public iVecteur Derivee1;
            public iVecteur Derivee2;
        }
    }

    static class GeometrieHelper
    {
        public static void CercleFromPoints(iPointOnSpline p1, iPointOnSpline p2, iPointOnSpline p3, out double cX, out double cY, out double r)
        {
            double offset = Math.Pow(p2.X, 2) + Math.Pow(p2.Y, 2);
            double bc = (Math.Pow(p1.X, 2) + Math.Pow(p1.Y, 2) - offset) / 2.0;
            double cd = (offset - Math.Pow(p3.X, 2) - Math.Pow(p3.Y, 2)) / 2.0;
            double det = (p1.X - p2.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p2.Y);

            //if (Math.Abs(det) < 0.0000000001) { throw new Exception("Les points sont align�s"); }

            double idet = 1 / det;

            cX = (bc * (p2.Y - p3.Y) - cd * (p1.Y - p2.Y)) * idet;
            cY = (cd * (p1.X - p2.X) - bc * (p2.X - p3.X)) * idet;
            r = Math.Sqrt(Math.Pow(p2.X - cX, 2) + Math.Pow(p2.Y - cY, 2));
        }

        public static void CercleFromPoints2(iPointOnSpline p1, iPointOnSpline p2, iPointOnSpline p3, out double cX, out double cY, out double cZ, out double r)
        {
            try
            {
                MathCircArc arc = MathHelper.Mu.CreateCircArcBy3Points(p1.MathPoint(), p2.MathPoint(), p2.MathPoint());

                arc.Center.GetPosition(out cX, out cY, out cZ);
                r = arc.Radius;
            }
            catch (Exception e)
            {
                Log.Write(e);
                cX = 0; cY = 0; cZ = 0; r = 0;
            }
        }

        public static iCercle CercleFromPoints(iPointOnSpline p1, iPointOnSpline p2, iPointOnSpline p3)
        {
            double cX, cY, r;

            CercleFromPoints(p1, p2, p3, out cX, out cY, out r);

            return new iCercle(new iPoint(cX, cY), r);
        }

        public static iArc ArcFromPoints(iPointOnSpline p1, iPointOnSpline p2, iPointOnSpline p3)
        {
            double cX, cY, r;

            CercleFromPoints(p1, p2, p3, out cX, out cY, out r);

            return new iArc(new iPoint(cX, cY), r, p1, p2, p3);
        }

        public struct iCercle
        {
            public iPoint Centre;
            public double Rayon;

            public iCercle(iPoint centre, double rayon)
            {
                Centre = centre; Rayon = rayon;
            }

            public override String ToString()
            {
                return Centre + " " + Rayon;
            }
        }

        public struct iLine
        {
            public iPointOnSpline P1;
            public iPointOnSpline P2;

            public iLine(iPointOnSpline p1, iPointOnSpline p2)
            {
                P1 = p1; P2 = p2;
            }
        }

        public struct iArc
        {
            public iPointOnSpline P1;
            public iPointOnSpline P2;
            public iPointOnSpline P3;
            public iPoint Centre;
            public double Rayon;

            public iArc(iPoint centre, double rayon, iPointOnSpline p1, iPointOnSpline p2, iPointOnSpline p3)
            {
                Centre = centre; Rayon = rayon; P1 = p1; P2 = p2; P3 = p3;
            }

            public double DistanceDe(iPoint pt)
            {
                return Math.Abs(pt.DistanceDe(Centre) - Rayon);
            }

            public double DeviationP1()
            {
                iVecteur vP1 = new iVecteur(Centre.X - P1.X, Centre.Y - P1.Y, Centre.Z - P1.Z);
                return Math.Abs(vP1.Angle(P1.Derivee1) - 90);
            }

            public double DeviationP3()
            {
                iVecteur vP3 = new iVecteur(Centre.X - P3.X, Centre.Y - P3.Y, Centre.Z - P3.Z);
                return Math.Abs(vP3.Angle(P3.Derivee1) - 90);
            }

            public override String ToString()
            {
                return Centre + " " + Rayon + " " + P1 + " " + P3;
            }
        }

        public struct iPoint
        {
            public Double X;
            public Double Y;
            public Double Z;

            public iPoint(double x, double y)
            {
                X = x; Y = y; Z = 0;
            }

            public iPoint(double x, double y, double z)
            {
                X = x; Y = y; Z = z;
            }

            public double DistanceDe(iPoint pt)
            {
                return Math.Sqrt(Math.Pow(pt.X - X, 2) + Math.Pow(pt.Y - Y, 2) + Math.Pow(pt.Z - Z, 2));
            }

            public override String ToString()
            {
                return "P(" + X + "," + Y + ")";
            }
        }

        public struct iPointOnSpline
        {
            public Double X;
            public Double Y;
            public Double Z;
            public double Distance;
            public iVecteur Derivee1;
            public iVecteur Derivee2;

            public iPointOnSpline(double x, double y, double dist, iVecteur derivee1, iVecteur derivee2)
            {
                X = x; Y = y; Z = 0; Distance = dist; Derivee1 = derivee1; Derivee2 = derivee2;
            }

            public iPointOnSpline(double x, double y, double z, double dist, iVecteur derivee1, iVecteur derivee2)
            {
                X = x; Y = y; Z = z; Distance = dist; Derivee1 = derivee1; Derivee2 = derivee2;
            }

            public iPoint Point
            {
                get
                {
                    return new iPoint(X, Y, Z);
                }
            }

            public double DistanceDe(iPoint pt)
            {
                return Math.Sqrt(Math.Pow(pt.X - X, 2) + Math.Pow(pt.Y - Y, 2) + Math.Pow(pt.Z - Z, 2));
            }

            public override String ToString()
            {
                return "P(" + X + "," + Y + ")";
            }
        }

        public struct iVecteur
        {
            public Double X;
            public Double Y;
            public Double Z;

            public iVecteur(double x, double y)
            {
                X = x; Y = y; Z = 0;
            }

            public iVecteur(double x, double y, double z)
            {
                X = x; Y = y; Z = z;
            }

            public iVecteur(double[] v)
            {
                X = 0; Y = 0; Z = 0;

                if (v.Length == 2)
                { X = v[0]; Y = v[1]; Z = 0.0; }
                else if (v.Length == 3)
                { X = v[0]; Y = v[1]; Z = v[2]; }
            }

            public double Angle(iVecteur vec)
            {
                return Math.Acos(ProdScalaire(vec) / (vec.Longueur * Longueur)) * 180.0 / Math.PI;
            }

            public double ProdScalaire(iVecteur vec)
            {
                return (X * vec.X) + (Y * vec.Y) + (Z * vec.Z);
            }

            public double Longueur
            {
                get
                {
                    return Math.Sqrt(Math.Pow(X, 2) + Math.Pow(Y, 2) + Math.Pow(Z, 2));
                }
            }

            public override String ToString()
            {
                return "V(" + X + "," + Y + ")";
            }
        }
    }
}