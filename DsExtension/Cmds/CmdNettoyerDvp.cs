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
                CmdLine.PrintLine("Création du claque 'LIGNES DE PLIAGE'");
                Layer LigneDePliage;
                LyMgr.CreateLayer("LIGNES DE PLIAGE", out LigneDePliage, out Erreur);
                c = LigneDePliage.Color;
                c.SetColorByIndex(252);
                LigneDePliage.Color = c;

                ///==============================================================================
                CmdLine.PrintLine("Création du claque 'NOTE DE PLIAGE'");
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
                CmdLine.PrintLine("Couleur des entité sur 'DuCalque'");
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
                CmdLine.PrintLine("Conversion des textes à graver en lignes");
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
                    foreach (var Spline in TabEntites)
                    {
                        if (ConvertirSplineLigne((Spline)Spline, 0.1) == null)
                        {
                            ConvertirSplineEnPolyligne((Spline)Spline, 0.1, 0.01);
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
                CmdLine.PrintLine(String.Format("Executé en {0}", GetSimplestTimeSpan(t)));

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

                // Si les points de control sont alignés dans la tolérance, on remplace par une ligne
                // On considère une spline droite dès q'un point dévie du 1000ème de la longueur
                bool EstDroite = true;
                {
                    // Calcul de la déviation
                    var deviation = Lg * tolerance / 100;

                    // Recupération des points de control
                    var LstControlPoint = spline.ListeControlPoint();

                    // On défini la ligne reliant le début et la fin de la spline
                    var s = LstControlPoint[0];
                    var e = LstControlPoint[LstControlPoint.Count - 1];
                    MathLine ligne = MathHelper.Mu.CreateLine(s.X, s.Y, s.Z, e.X, e.Y, e.Z, dsMathLineType_e.dsMathLineType_Bounded);

                    // On supprime les points de départ et d'arrivée de la liste
                    LstControlPoint.RemoveAt(0);
                    LstControlPoint.RemoveAt(LstControlPoint.Count - 1);

                    // On vérifie la déviation de chaque point par rapport à la ligne.
                    // Si elle est supérieur, on sort
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
                // Si la longueur de la spline est inferieure ou égale à 3 * le pas
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
                        // Reste à parcourir
                        var _diff = Lg - pos;

                        // Si la différence == 0 on ne renvoi rien
                        if (_diff == 0.0) return null;

                        // Si la différence est inferieur à 2*Pas on retourne l'arc
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

                // On crée les arc de cercle
                foreach (var arc in LstArc)
                    LstDsArc.Add(SkMgr.InsertArcBy3Points(arc.P1.Point.MathPoint(), arc.P2.Point.MathPoint(), arc.P3.Point.MathPoint()));

                //// On récupère le premier point
                //iPoint pt = LstArc[0].P1.Point;

                //double[] coords = new double[4];
                //coords[0] = pt.X;
                //coords[1] = pt.Y;
                //coords[2] = pt.X;
                //coords[3] = pt.Y-100;

                //// On crée une polyligne droite pour pouvoir y ajouter les arcs
                //Polyligne = SkMgr.InsertPolyline2D(coords, false);

                //// On ajoute les arcs
                //foreach (var arc in LstDsArc)
                //    Polyligne.JoinCircleArc(arc);

                //// On supprime le premier point
                //Polyligne.RemoveVertex(Polyligne.GetVerticesCount()-1);
            }
            catch (Exception e)
            { Log.Write(e); }

            return LstDsArc;
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
            ev.Point = new iPoint(X, Y, Z);
            ev.PointOnSpline = new iPointOnSpline(X, Y, D);
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
            ev.PointOnSpline = new iPointOnSpline(X, Y, dist);
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
            return new iPointOnSpline(X, Y, D);
        }

        public static iPointOnSpline PointOnSplineDistance(this Spline spl, Double dist)
        {
            double X, Y, Z, P; object fD, sD;
            spl.EvaluateAtDistance(dist, out X, out Y, out Z, out P, out fD, out sD);
            return new iPointOnSpline(X, Y, dist);
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
        private static void CercleFromPoints(iPointOnSpline p1, iPointOnSpline p2, iPointOnSpline p3, out double cX, out double cY, out double r)
        {
            double offset = Math.Pow(p2.X, 2) + Math.Pow(p2.Y, 2);
            double bc = (Math.Pow(p1.X, 2) + Math.Pow(p1.Y, 2) - offset) / 2.0;
            double cd = (offset - Math.Pow(p3.X, 2) - Math.Pow(p3.Y, 2)) / 2.0;
            double det = (p1.X - p2.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p2.Y);

            //if (Math.Abs(det) < 0.0000000001) { throw new Exception("Les points sont alignés"); }

            double idet = 1 / det;

            cX = (bc * (p2.Y - p3.Y) - cd * (p1.Y - p2.Y)) * idet;
            cY = (cd * (p1.X - p2.X) - bc * (p2.X - p3.X)) * idet;
            r = Math.Sqrt(Math.Pow(p2.X - cX, 2) + Math.Pow(p2.Y - cY, 2));
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
            public double Distance;

            public iPointOnSpline(double x, double y, double dist)
            {
                X = x; Y = y; Distance = dist;
            }

            public iPoint Point
            {
                get
                {
                    return new iPoint(X, Y);
                }
            }

            public double DistanceDe(iPoint pt)
            {
                return Math.Sqrt(Math.Pow(pt.X - X, 2) + Math.Pow(pt.Y - Y, 2));
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

            public iVecteur(double x, double y)
            {
                X = x; Y = y;
            }

            public iVecteur(double[] v)
            {
                X = v[0]; Y = v[1];
            }

            public override String ToString()
            {
                return "V(" + X + "," + Y + ")";
            }
        }
    }
}