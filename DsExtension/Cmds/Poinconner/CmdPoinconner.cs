using DraftSight.Interop.dsAutomation;
using LogDebugging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

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

                // Erreur avec le premier prompt, la commande est automatiquement annul�e
                // Pour contourner le pb, on lance une commande � vide.
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

                int NbPoint = 5000;
                Double Jeu = 5;
                Double DiamMin = 0;
                Double DiamMax = Double.PositiveInfinity;
                Double SeuilNoirs = 8;
                int TypeSampler = 1;
                int NbAffinage = 4;

                CmdLine.PromptForInteger("Nb de points maximum ", NbPoint, out NbPoint);
                CmdLine.PromptForDouble("Jeu entre les cercles ", Jeu, out Jeu);
                CmdLine.PromptForDouble("Supprimer les cercles de diam inf. � ", DiamMin, out DiamMin);
                CmdLine.PromptForDouble("R�duire les cercles de diam sup. � ", DiamMax, out DiamMax);
                CmdLine.PromptForInteger("Type de sampler : 1 -> Poisson / 2 -> Rejection ", TypeSampler, out TypeSampler);
                CmdLine.PromptForInteger("Nb d'iteration pour l'affinage ", NbAffinage, out NbAffinage);

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

                    Double ImgX, ImgY, ImgZ;
                    Image.GetPosition(out ImgX, out ImgY, out ImgZ);

                    CmdLine.PrintLine("Sampler");

                    List<PointF> listePoint;
                    if (TypeSampler == 1)
                        listePoint = BitmapPoissonSampler.Run(Image, NbPoint);
                    else
                        listePoint = BitmapRejectionSampler.Run(Image, NbPoint);

                    CmdLine.PrintLine("Sampler termin�");

                    VoronoiMap.VoronoiGraph graph;
                    var listeSitePoincon = VoronoiEquilibreur.Start(Image, listePoint, NbAffinage, out graph);

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

                    var CalqueHachures = LyM.GetLayer("Hachures");
                    if (CalqueHachures == null)
                    {
                        LyM.CreateLayer("Hachures", out CalqueHachures, out res);
                        var c = CalqueHachures.Color;
                        c.SetColorByIndex(100);
                        CalqueMaillage.Color = c;
                    }

                    var ListeCercles = new List<Circle>();
                    CalquePoincon.Activate();
                    foreach (var pc in listeSitePoincon)
                    {
                        if (pc.GrisCercleInscrit > SeuilNoirs && (pc.CercleInscrit - Jeu * 0.5) >= DiamMin)
                        {
                            var cercle = SkM.InsertCircleByDiameter(ImgX + pc.Site.X, ImgY + Image.Height - pc.Site.Y, 0, Math.Min(DiamMax, pc.CercleInscrit - Jeu * 0.5));
                            ListeCercles.Add(cercle);
                        }
                    }

                    CalqueHachures.Activate();
                    var selectedEntities = new DispatchWrapper[ListeCercles.Count];
                    for (int i = 0; i < ListeCercles.Count; i++)
                        selectedEntities[i] = new DispatchWrapper(ListeCercles[i]);

                    SkM.InsertHatchByEntities(selectedEntities, "SOLID", 1, 0);

                    CmdLine.PrintLine(String.Format("Nb de percages : {0}", ListeCercles.Count));

                    CalqueMaillage.Activate();
                    foreach (var s in graph.Segments)
                        SkM.InsertLine(ImgX + s.P1.X, ImgY + Image.Height - s.P1.Y, 0, ImgX + s.P2.X, ImgY + Image.Height - s.P2.Y, 0);

                    LyM.GetLayer("0").Activate();

                }
                else
                    CmdLine.PrintLine("Pas d'image");

                t = DateTime.Now - DateTimeStart;
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
}