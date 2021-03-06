using DraftSight.Interop.dsAutomation;
using LogDebugging;
using System;
using System.Collections.Generic;
using static Cmds.GeometrieHelper;

namespace Cmds
{
    public class CmdTest : CmdBase
    {
        public CmdTest(DraftSight.Interop.dsAutomation.Application app, string groupName) : base(app, groupName)
        { }

        public SketchManager Sm = null;

        public override string globalName() { return "_MTest"; }
        public override string localName() { return "MTest"; }

        public override string Description() { return "Tester"; }
        public override string ItemName() { return "Test"; }

        public override void Execute()
        {
            try
            {
                Sm = DsApp.GetActiveDocument().GetModel().GetSketchManager();

                DsApp.AbortRunningCommand();

                CommandMessage CmdLine = DsApp.GetCommandMessage();
                if (null == CmdLine) return;

                // Erreur avec le premier prompt, la commande est automatiquement annul�e
                // Pour contourner le pb, on lance une commande � vide.
                {
                    String sss = "";
                    CmdLine.PromptForString(true, "", "", out sss);
                }

                Document DsDoc = DsApp.GetActiveDocument();
                Model Mdl = DsDoc.GetModel();
                SketchManager SkMgr = Mdl.GetSketchManager();
                SelectionManager SlMgr = DsDoc.GetSelectionManager();
                SelectionFilter SlFilter;
                SlFilter = SlMgr.GetSelectionFilter();

                object[] TabEntites = null;

                SlFilter.Clear();
                SlFilter.AddEntityType(dsObjectType_e.dsSplineType);
                SlFilter.Active = true;

                if (CmdLine.PromptForSelection(true, "Selectionnez la spline", "Ce n'est pas une spline"))
                {
                    dsObjectType_e entityType;
                    var count = SlMgr.GetSelectedObjectCount(dsSelectionSetType_e.dsSelectionSetType_Previous);
                    TabEntites = new object[1];

                    object selectedEntity = SlMgr.GetSelectedObject(dsSelectionSetType_e.dsSelectionSetType_Previous, 0, out entityType);

                    if (dsObjectType_e.dsSplineType == entityType)
                    {
                        TabEntites[0] = selectedEntity;
                    }
                }

                CmdLine.PrintLine(TabEntites.Length + " spline(s) selectionn�e(s)");

                TimeSpan t; DateTime DateTimeStart;

                DateTimeStart = DateTime.Now;

                if (TabEntites != null && TabEntites.Length > 0)
                {
                    foreach (Spline Spline in TabEntites)
                    {
                        var slength = Spline.GetLength();

                        var Boucle = 100000;
                        var Pas = slength / Boucle;

                        DateTimeStart = DateTime.Now;

                        for (double i = 0; i <= slength; i += Pas)
                        {
                            //CmdLine.PrintLine(i.ToString());

                            var ev = Spline.EvalDistance(i);
                            
                            //Action<iPoint, iVecteur, double> Tracer = delegate (iPoint origine, iVecteur derive, double echelle)
                            //{
                            //    var s = origine;
                            //    var e = origine.DeplacerC(derive.MultiplierC(echelle));

                            //    SkMgr.InsertLine(s.X, s.Y, s.Z, e.X, e.Y, e.Z);
                            //};

                            ////Tracer(ev.Point, ev.Derivee1, 100);
                            ////Tracer(ev.Point, ev.Derivee2, 1000);
                        }

                        CmdLine.PrintLine(String.Format("1 Execut� en {0}", GetSimplestTimeSpan(DateTime.Now - DateTimeStart)));

                        double sP;
                        double eP;
                        Spline.GetEndParams(out sP, out eP);
                        Pas = (eP - sP) / Boucle;

                        DateTimeStart = DateTime.Now;

                        for (double i = sP; i <= eP; i += Pas)
                        {
                            //CmdLine.PrintLine(i.ToString());

                            var ev = Spline.EvalParam(i);

                            //Action<iPoint, iVecteur, double> Tracer = delegate (iPoint origine, iVecteur derive, double echelle)
                            //{
                            //    var s = origine;
                            //    var e = origine.DeplacerC(derive.MultiplierC(echelle));

                            //    SkMgr.InsertLine(s.X, s.Y, s.Z, e.X, e.Y, e.Z);
                            //};

                            ////Tracer(ev.Point, ev.Derivee1, 100);
                            ////Tracer(ev.Point, ev.Derivee2, 1000);
                        }

                        CmdLine.PrintLine(String.Format("2 Execut� en {0}", GetSimplestTimeSpan(DateTime.Now - DateTimeStart)));
                    }
                }

                t = DateTime.Now - DateTimeStart;
                CmdLine.PrintLine(String.Format("Execut� en {0}", GetSimplestTimeSpan(t)));

            }
            catch (Exception e)
            { Log.Write(e); }
        }
    }
}