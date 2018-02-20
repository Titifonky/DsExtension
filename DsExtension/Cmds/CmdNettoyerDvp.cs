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
                    CmdLine.PrintLine(TabEntites.Length + " spline(s) convertis");
                    foreach (var Spline in TabEntites)
                    {
                        ConvertirSplineEnPolyligne((Spline)Spline, 0.1, 0.01);
                    }
                }
                ///commandline.PrintLine("Command Executed! You need to write code to create entity.");

            }
            catch (Exception e)
            { Log.Write(e); }
        }

        private string[] GetTabNomsCalques(Document dsDoc)
        {
            //Get Layer Manager and Layer names
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

        private void ConvertirSplineEnPolyligne(Spline spline, Double tolerance, Double angle)
        {
            try
            {
                double Pas = 0.1;

                Document DsDoc = DsApp.GetActiveDocument();
                Model Mdl = DsDoc.GetModel();
                CommandMessage CmdLine = DsApp.GetCommandMessage();
                SketchManager SkMgr = Mdl.GetSketchManager();

                DateTime DateTimeStart = DateTime.Now;

                {
                    Double S, E;
                    spline.GetEndParams(out S, out E);
                    Double Lg = spline.GetLength();

                    CmdLine.PrintLine("Param : " + S.ToString() + " -> " + E.ToString());

                    List<iPointOnSpline> LstPoint = new List<iPointOnSpline>();
                    List<iArc> LstArc = new List<iArc>();

                    // TODO :
                    // Si la spline à une longueur inferieure à 0.3mm, traiter le cas

                    double i = 0;

                    var p1 = spline.PointOnSplineDistance(0);
                    iArc? LastArc = null;
                    iPointOnSpline? LastPoint = null;

                    while (i <= Lg)
                    {
                        // Recherche du prochain point d'arc p3
                        var p3 = spline.PointOnSplineDistance(i);
                        LstPoint.Add(p3);

                        // Bricolage pour initialiser p2
                        iPointOnSpline p2 = p3;                

                        var nbPt = LstPoint.Count;
                        Boolean Exit = false;
                        iArc arc;
                        if (nbPt > 2)
                        {
                            if (nbPt % 2 != 0)
                                p2 = LstPoint[nbPt / 2];
                            else
                                p2 = spline.PointOnSplineDistance((p3.Distance + p1.Distance) * 0.5);

                            arc = ArcFromPoints(p1, p2, p3);


                            foreach (var pt in LstPoint)
                            {
                                if (arc.DistanceDe(pt.Point) > tolerance)
                                    Exit = true;
                            }

                            if (Exit)
                            {
                                if (LstPoint.Count > 3)
                                {
                                    var a = (iArc)LastArc;
                                    LstArc.Add(a);
                                    p1 = (iPointOnSpline)LastPoint;
                                }
                                else
                                {
                                    LstArc.Add(arc);
                                    p1 = p3;
                                }

                                i = p1.Distance;
                                LstPoint.Clear();
                                LstPoint.Add(p1);
                            }

                            LastPoint = p3;
                            LastArc = arc;
                        }

                        // Prochaine itération
                        if (i == Lg)
                        {
                            if (!Exit)
                                LstArc.Add(ArcFromPoints(p1, p2, p3));
                            break;
                        }
                        else if ((Lg - i) < Pas)
                            i = Lg;
                        else
                            i = i + Pas;
                    }

                    Color c;
                    dsCreateObjectResult_e Erreur;

                    LayerManager LyMgr = DsDoc.GetLayerManager();
                    Layer Convert;
                    LyMgr.CreateLayer("CONVERT", out Convert, out Erreur);
                    c = Convert.Color;
                    c.SetColorByIndex(10);
                    Convert.Color = c;

                    foreach (var arc in LstArc)
                    {
                        var a = SkMgr.InsertArcBy3Points(arc.P1.MathPoint(), arc.P2.MathPoint(), arc.P3.MathPoint());
                        a.Layer = "CONVERT";
                    }
                }

                TimeSpan t = DateTime.Now - DateTimeStart;
                CmdLine.PrintLine(String.Format("Executé en {0}", GetSimplestTimeSpan(t)));
            }
            catch (Exception e)
            { Log.Write(e); }
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
            return Mu.CreatePoint(pt.X , pt.Y, pt.Z);
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
        private static void CercleFromPoints(iPoint p1, iPoint p2, iPoint p3, out double cX, out double cY, out double r)
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

        public static iCercle CercleFromPoints(iPoint p1, iPoint p2, iPoint p3)
        {
            double cX, cY, r;

            CercleFromPoints(p1, p2, p3, out cX, out cY, out r);

            return new iCercle(new iPoint(cX, cY), r);
        }

        public static iArc ArcFromPoints(iPoint p1, iPoint p2, iPoint p3)
        {
            double cX, cY, r;

            CercleFromPoints(p1, p2, p3, out cX, out cY, out r);

            return new iArc(new iPoint(cX, cY), r, p1, p2, p3);
        }

        public static iArc ArcFromPoints(iPointOnSpline p1, iPointOnSpline p2, iPointOnSpline p3)
        {
            double cX, cY, r;
            var P1 = p1.Point;
            var P2 = p2.Point;
            var P3 = p3.Point;

            CercleFromPoints(P1, p2.Point, P3, out cX, out cY, out r);

            return new iArc(new iPoint(cX, cY), r, P1, P2, P3);
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
            public iPoint P1;
            public iPoint P2;
            public iPoint P3;
            public iPoint Centre;
            public double Rayon;

            public iArc(iPoint centre, double rayon, iPoint p1, iPoint p2, iPoint p3)
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