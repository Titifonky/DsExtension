using DraftSight.Interop.dsAutomation;
using LogDebugging;
using System;
using System.Collections.Generic;

namespace Cmds.Poinconner
{
    public class CmdPoinconner : CmdBase
    {
        public CmdPoinconner(DraftSight.Interop.dsAutomation.Application app, string groupName) : base(app, groupName)
        { }

        public Document DsDoc= null;
        public Model DsMdl = null;
        public LayerManager LyM = null;
        public SketchManager SkM = null;
        public SelectionManager SlM = null;

        public override string globalName() { return "_MTPoinconner"; }
        public override string localName() { return "MPoinconner"; }

        public override string Description() { return "Poinconner"; }
        public override string ItemName() { return "Poinconner"; }

        public override void Execute()
        {
            try
            {
                DsDoc = DsApp.GetActiveDocument();
                DsMdl = DsDoc.GetModel();
                LyM = DsDoc.GetLayerManager();
                SkM = DsMdl.GetSketchManager();
                SlM = DsDoc.GetSelectionManager();

                DsApp.AbortRunningCommand();

                CommandMessage CmdLine = DsApp.GetCommandMessage();
                if (null == CmdLine) return;

                // Erreur avec le premier prompt, la commande est automatiquement annulée
                // Pour contourner le pb, on lance une commande à vide.
                {
                    String sss = "";
                    CmdLine.PromptForString(true, "", "", out sss);
                }

                SelectionFilter SlFilter;
                SlFilter = SlM.GetSelectionFilter();

                ReferenceImage Image = null;

                SlFilter.Clear();
                SlFilter.AddEntityType(dsObjectType_e.dsReferenceImageType);
                SlFilter.Active = true;

                //if (CmdLine.PromptForSelection(true, "Selectionnez l'image", "Ce n'est pas une image"))
                //{
                //    dsObjectType_e entityType;
                //    var count = SlMgr.GetSelectedObjectCount(dsSelectionSetType_e.dsSelectionSetType_Previous);

                //    object selectedEntity = SlMgr.GetSelectedObject(dsSelectionSetType_e.dsSelectionSetType_Previous, 0, out entityType);

                //    if (dsObjectType_e.dsReferenceImageType == entityType)
                //        Image = (ReferenceImage)selectedEntity;
                //}

                object ObjType = null;
                object ObjEntites = null;
                string[] TabNomsCalques = GetTabNomsCalques(DsDoc);
                SkM.GetEntities(SlFilter, TabNomsCalques, out ObjType, out ObjEntites);
                object[] TabEntites = ObjEntites as object[];
                Image = (ReferenceImage)TabEntites[0];

                TimeSpan t; DateTime DateTimeStart;

                DateTimeStart = DateTime.Now;

                if (Image != null)
                {
                    CmdLine.PrintLine(String.Format("Image : {0}", Image.GetPath()));

                    //var listePoint = BitmapRejectionSampler.Run(Image, 6000);
                    var listePoint = BitmapPoissonSampler.Run(Image, 6000);

                    VoronoiMap.VoronoiGraph graph;
                    var listeSitePoincon = VoronoiEquilibreur.Start(Image, listePoint, 2, out graph);

                    CmdLine.PrintLine(String.Format("Nb de percages : {0}", listeSitePoincon.Count));

                    dsCreateObjectResult_e res;
                    var CalquePoincon = LyM.GetLayer("Poincon");
                    if (CalquePoincon == null)
                    {
                        LyM.CreateLayer("Poincon", out CalquePoincon, out res);
                        var c = CalquePoincon.Color;
                        c.SetColorByIndex(252);
                        CalquePoincon.Color = c;
                    }

                    var CalqueMaillage = LyM.GetLayer("Maillage");
                    if (CalqueMaillage == null)
                    {
                        LyM.CreateLayer("Maillage", out CalqueMaillage, out res);
                        var c = CalqueMaillage.Color;
                        c.SetColorByIndex(126);
                        CalqueMaillage.Color = c;
                    }

                    CalquePoincon.Activate();
                    foreach (var pc in listeSitePoincon)
                        SkM.InsertCircleByDiameter(pc.Site.X, Image.Height - pc.Site.Y, 0, pc.Poincon);

                    CalqueMaillage.Activate();
                    foreach (var s in graph.Segments)
                        SkM.InsertLine(s.P1.X, Image.Height - s.P1.Y, 0, s.P2.X, Image.Height - s.P2.Y, 0);

                }
                else
                    CmdLine.PrintLine("Pas d'image");

                t = DateTime.Now - DateTimeStart;
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
    }
}