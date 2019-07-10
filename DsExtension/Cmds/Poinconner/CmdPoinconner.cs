using DraftSight.Interop.dsAutomation;
using LogDebugging;
using System;
using System.Collections.Generic;
using System.Drawing;
using static Cmds.GeometrieHelper;

namespace Cmds.Poinconner
{
    public class CmdPoinconner : CmdBase
    {
        public CmdPoinconner(DraftSight.Interop.dsAutomation.Application app, string groupName) : base(app, groupName)
        { }

        public SketchManager Sm = null;

        public override string globalName() { return "_MTPoinconner"; }
        public override string localName() { return "MPoinconner"; }

        public override string Description() { return "Poinconner"; }
        public override string ItemName() { return "Poinconner"; }

        public override void Execute()
        {
            try
            {
                Sm = DsApp.GetActiveDocument().GetModel().GetSketchManager();

                DsApp.AbortRunningCommand();

                CommandMessage CmdLine = DsApp.GetCommandMessage();
                if (null == CmdLine) return;

                // Erreur avec le premier prompt, la commande est automatiquement annulée
                // Pour contourner le pb, on lance une commande à vide.
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

                ReferenceImage Image = null;

                SlFilter.Clear();
                SlFilter.AddEntityType(dsObjectType_e.dsReferenceImageType);
                SlFilter.Active = true;

                if (CmdLine.PromptForSelection(true, "Selectionnez l'image", "Ce n'est pas une image"))
                {
                    dsObjectType_e entityType;
                    var count = SlMgr.GetSelectedObjectCount(dsSelectionSetType_e.dsSelectionSetType_Previous);

                    object selectedEntity = SlMgr.GetSelectedObject(dsSelectionSetType_e.dsSelectionSetType_Previous, 0, out entityType);

                    if (dsObjectType_e.dsReferenceImageType == entityType)
                        Image = (ReferenceImage)selectedEntity;
                }

                TimeSpan t; DateTime DateTimeStart;

                DateTimeStart = DateTime.Now;

                if (Image != null)
                {
                    //var liste = PoissonDiskSampler.SampleBitmapPoisson(Image, new List<int>() { 10, 5, 2 });
                    var liste = VoronoiSampler.SampleBitmapRejection(Image, 2000, new List<int>() { 1, 2, 3 });
                    

                    CmdLine.PrintLine(String.Format("Nb de percages : {0}", liste.Count));

                    foreach (var pc in liste)
                    {
                        //Sm.InsertCircle(pc.V.X, Image.Height - pc.V.Y, 0, pc.Radius);
                    }

                }

                t = DateTime.Now - DateTimeStart;
                CmdLine.PrintLine(String.Format("Executé en {0}", GetSimplestTimeSpan(t)));

            }
            catch (Exception e)
            { Log.Write(e); }
        }
    }
}