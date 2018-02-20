using DraftSight.Interop.dsAutomation;
using LogDebugging;
using System;
using System.Collections.Generic;
using static Cmds.GeometrieHelper;

namespace Cmds
{
    public class CmdNettoyerDvp : CmdBase
    {
        public CmdNettoyerDvp(DraftSight.Interop.dsAutomation.Application app, string groupName) : base(app, groupName)
        { }

        public override string globalName() { return "_MNettoyerDvp"; }
        public override string localName() { return "MNettoyerDvp"; }

        public override string Description() { return "Nettoyer les dvp SW pour le laser"; }
        public override string ItemName() { return "Nettoyer Dvp"; }

        public override void Execute()
        {
            try
            {
                var DateTimeStart = DateTime.Now;

                DsApp.AbortRunningCommand();

                CommandMessage CmdLine = DsApp.GetCommandMessage();
                if (null == CmdLine) return;

                ///==============================================================================
                CmdLine.PrintLine("Calque '0' actif");
                Document DsDoc = DsApp.GetActiveDocument();
                LayerManager LyMgr = DsDoc.GetLayerManager();
                Layer L0 = LyMgr.GetLayer("0");
                L0.Activate();

                // Creer les calques de pliage
                Color c;
                dsCreateObjectResult_e Erreur;

                ///==============================================================================
                CmdLine.PrintLine("Cr�ation du claque 'LIGNES DE PLIAGE'");
                Layer LigneDePliage;
                LyMgr.CreateLayer("LIGNES DE PLIAGE", out LigneDePliage, out Erreur);
                c = LigneDePliage.Color;
                c.SetColorByIndex(252);
                LigneDePliage.Color = c;

                ///==============================================================================
                CmdLine.PrintLine("Cr�ation du claque 'NOTE DE PLIAGE'");
                Layer NoteDePliage = null;
                LyMgr.CreateLayer("NOTE DE PLIAGE", out NoteDePliage, out Erreur);
                c = NoteDePliage.Color;
                c.SetColorByIndex(126);
                NoteDePliage.Color = c;


                Model Mdl = DsDoc.GetModel();
                SketchManager SkMgr = Mdl.GetSketchManager();
                SelectionManager SlMgr = DsDoc.GetSelectionManager();
                SelectionFilter SlFilter;

                object ObjType = null;
                object ObjEntites = null;
                Int32[] TabTypes = null;
                object[] TabEntites = null;
                string[] TabNomsCalques = null;

                ///==============================================================================
                CmdLine.PrintLine("Couleur des entit� sur 'DuCalque'");
                TabNomsCalques = GetTabNomsCalques(DsDoc);
                SkMgr.GetEntities(null, TabNomsCalques, out ObjType, out ObjEntites);
                TabEntites = (object[])ObjEntites;

                EntityHelper dsEntityHelper = DsApp.GetEntityHelper();

                foreach (object entity in TabEntites)
                {
                    Color ce = dsEntityHelper.GetColor(entity);
                    ce.SetNamedColor(dsNamedColor_e.dsNamedColor_ByLayer);
                    dsEntityHelper.SetColor(entity, ce);
                }

                ///==============================================================================
                CmdLine.PrintLine("Transfert des lignes et notes de pliage sur les calques correspondants");
                TabNomsCalques = new string[] { "0" };
                SkMgr.GetEntities(null, TabNomsCalques, out ObjType, out ObjEntites);

                TabTypes = (Int32[])ObjType;
                TabEntites = (object[])ObjEntites;

                for (int i = 0; i < TabEntites.GetLength(0); i++)
                {
                    dsObjectType_e tpe = (dsObjectType_e)TabTypes[i];

                    if (tpe == dsObjectType_e.dsLineType)
                    {
                        Line L = (Line)TabEntites[i];
                        if (L.LineStyle == "SLD-Center")
                        {
                            L.Layer = LigneDePliage.Name;
                        }
                    }
                    else if (tpe == dsObjectType_e.dsNoteType)
                    {
                        Note N = (Note)TabEntites[i];
                        N.Layer = NoteDePliage.Name;
                    }
                }

                ///==============================================================================
                CmdLine.PrintLine("Conversion des textes � graver en lignes");
                //Get selection filter
                SlFilter = SlMgr.GetSelectionFilter();
                SlFilter.Clear();
                SlFilter.AddEntityType(dsObjectType_e.dsNoteType);
                SlFilter.Active = true;

                TabNomsCalques = new string[] { "GRAVURE" };
                SkMgr.GetEntities(SlFilter, TabNomsCalques, out ObjType, out ObjEntites);
                TabTypes = (Int32[])ObjType;
                TabEntites = ObjEntites as object[];

                if (TabEntites != null && TabEntites.Length > 0)
                {
                    CmdLine.PrintLine(TabEntites.Length + " texte(s) convertis");
                    foreach (var Texte in TabEntites)
                    {
                        SlMgr.ClearSelections(dsSelectionSetType_e.dsSelectionSetType_Current);
                        dsEntityHelper.Select(Texte, true);
                        DsApp.RunCommand("ECLATERTEXTE\n", true);
                    }
                }

                ///==============================================================================
                CmdLine.PrintLine("Conversion des splines en polylignes");
                //Get selection filter
                SlFilter = SlMgr.GetSelectionFilter();
                SlFilter.Clear();
                SlFilter.AddEntityType(dsObjectType_e.dsSplineType);
                SlFilter.Active = true;

                TabNomsCalques = new string[] { "0" };
                SkMgr.GetEntities(SlFilter, TabNomsCalques, out ObjType, out ObjEntites);
                TabTypes = (Int32[])ObjType;
                TabEntites = ObjEntites as object[];

                if (TabEntites != null && TabEntites.Length > 0)
                {
                    foreach (Spline Spline in TabEntites)
                    {
                        SplineConverter SplConverter = new SplineConverter(Spline, 0.1, 0.1);
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

                    for (int i = 0; i < TabEntites.Length; i++)
                    {
                        Spline s = (Spline)TabEntites[i];
                        s.Erased = true;
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

        private Line ConvertirSplineLigne(Spline spline, Double tolerance)
        {
            Line Ligne = null;
            try
            {
                Document DsDoc = DsApp.GetActiveDocument();
                Model Mdl = DsDoc.GetModel();
                CommandMessage CmdLine = DsApp.GetCommandMessage();
                SketchManager SkMgr = Mdl.GetSketchManager();

                Double Lg = spline.GetLength();

                // Si les points de control sont align�s dans la tol�rance, on remplace par une ligne
                // On consid�re une spline droite d�s q'un point d�vie du 1000�me de la longueur
                bool EstDroite = true;
                {
                    // Calcul de la d�viation
                    var deviation = Lg * tolerance / 100;

                    // Recup�ration des points de control
                    var LstControlPoint = spline.ListeControlPoint();

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
                    var LstControlPoint = spline.ListeControlPoint();
                    var s = LstControlPoint[0];
                    var e = LstControlPoint[LstControlPoint.Count - 1];
                    Ligne = SkMgr.InsertLine(s.X, s.Y, s.Z, e.X, e.Y, e.Z);
                }
            }
            catch (Exception e)
            { Log.Write(e); }

            return Ligne;
        }

        private List<CircleArc> ConvertirSplineEnPolyligne(Spline spline, Double tolerance, Double angle)
        {
            List<CircleArc> LstDsArc = new List<CircleArc>();
            try
            {
                double Pas = 0.1;

                Document DsDoc = DsApp.GetActiveDocument();
                Model Mdl = DsDoc.GetModel();
                CommandMessage CmdLine = DsApp.GetCommandMessage();
                SketchManager SkMgr = Mdl.GetSketchManager();

                Double Lg = spline.GetLength();
                // On calcul la deviation admissible en pourcent de la longueur
                var deviation = Math.Min(Lg * tolerance / 100, 0.1);

                List<iArc> LstArc = new List<iArc>();
                // Si la longueur de la spline est inferieure ou �gale � 3 * le pas
                if (Lg <= (Pas * 3))
                {
                    LstArc.Add(ArcFromPoints(spline.PointOnSplineDistance(0), spline.PointOnSplineDistance(Lg * 0.5), spline.PointOnSplineDistance(Lg)));
                }
                // Sinon on parcourt la spline
                else
                {
                    Func<double, iArc?> ChercherArc = delegate (double pos)
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
                                spline.PointOnSplineDistance(pos),
                                spline.PointOnSplineDistance(pos + _diff * 0.5),
                                spline.PointOnSplineDistance(pos + _diff));
                        }
                        else
                        {
                            _p1 = spline.PointOnSplineDistance(pos);
                            _p2 = spline.PointOnSplineDistance(pos + Pas);
                            _p3 = spline.PointOnSplineDistance(pos + (2 * Pas));
                            pos += 2 * Pas;
                        }

                        iArc _arc = ArcFromPoints(_p1, _p2, _p3);
                        _LstPoint.Add(_p1); _LstPoint.Add(_p2); _LstPoint.Add(_p3);

                        iArc _LastArc = _arc;

                        do
                        {
                            Boolean _Exit = false;
                            for (int _p = 1; _p < _LstPoint.Count - 1; _p++)
                            {
                                if (_arc.DistanceDe(_LstPoint[_p].Point) > deviation)
                                {
                                    _Exit = true;
                                    break;
                                }
                            }

                            if (_Exit)
                                return _LastArc;
                            else
                            {
                                if (pos == Lg)
                                    break;
                                else if ((Lg - pos) < Pas)
                                    pos = Lg;
                                else
                                    pos += Pas;

                                _p3 = spline.PointOnSplineDistance(pos);
                                _LstPoint.Add(_p3);
                                _p2 = _LstPoint[_LstPoint.Count / 2];

                                if (_LstPoint.Count % 2 == 0)
                                    _p2 = spline.PointOnSplineDistance((_p3.Distance + _p1.Distance) * 0.5);

                                _LastArc = _arc;
                                _arc = ArcFromPoints(_p1, _p2, _p3);
                            }


                        } while (pos <= Lg);

                        return _arc;
                    };

                    var arc = ChercherArc(0.0);
                    while (arc != null)
                    {
                        var a = (iArc)arc;
                        LstArc.Add(a);
                        arc = ChercherArc(a.P3.Distance);
                    }
                }

                // On cr�e les arc de cercle
                foreach (var arc in LstArc)
                    LstDsArc.Add(SkMgr.InsertArcBy3Points(arc.P1.MathPoint(), arc.P2.MathPoint(), arc.P3.MathPoint()));

            }
            catch (Exception e)
            { Log.Write(e); }

            return LstDsArc;
        }

    }

    class SplineConverter
    {
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

        public Double Tolerance { get; set; }
        public Double Angle { get; set; }
        public Double Pas { get; set; }

        public SplineConverter(Spline spline, Double tolerance, Double angle, Double pas = 0.1)
        {
            Spline = spline; Tolerance = tolerance; Angle = angle;
            Lg = spline.GetLength();
            Pas = pas;

            Double sP, eP;
            Spline.GetEndParams(out sP, out eP);
            StartPoint = Spline.PointOnSplineParam(sP);
            EndPoint = Spline.PointOnSplineParam(eP);


        }

        private Double deviation;
        /// <summary>
        /// Converti la spline en ligne si elle rentre dans la tol�rance
        /// </summary>
        /// <param name="Absolu">En absolu ou pourcentage de la longueur de la spline</param>
        /// <returns></returns>
        public iLine? EnLigne(bool Absolu = false)
        {
            try
            {
                // Si les points de control sont align�s dans la tol�rance, on remplace par une ligne
                bool EstDroite = true;
                {
                    // Calcul de la d�viation
                    deviation = Tolerance;
                    if (!Absolu)
                        deviation = Lg * Tolerance / 100;

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

        public List<iArc> EnPolyligne(bool Absolu = false)
        {
            List<iArc> LstArc = new List<iArc>();
            try
            {
                double Pas = 0.1;

                Double Lg = Spline.GetLength();
                // On calcul la deviation admissible en pourcent de la longueur

                deviation = Tolerance;
                if (!Absolu)
                    deviation = Math.Min(Lg * Tolerance / 100, 0.1);

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
                if (Angle != 0)
                {
                    Log.Write(_arc.DeviationP1() + " " + _arc.DeviationP3());

                    if ((_arc.DeviationP1() > Angle) || (_arc.DeviationP3() > Angle))
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
                    return _LastArc;
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
                return Math.Abs(vP1.Angle(P1.Derivee2));
            }

            public double DeviationP3()
            {
                iVecteur vP3 = new iVecteur(Centre.X - P3.X, Centre.Y - P3.Y, Centre.Z - P3.Z);
                return Math.Abs(vP3.Angle(P3.Derivee2));
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