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
                Double DiamMin = 5;
                Double DiamMax = 45; // Double.PositiveInfinity;
                int SeuilNoirs = 8;
                int BlancPctDiam = 65;
                int TypeSampler = 2;
                int NbAffinage = 6;
                bool MaillageEtPoint = false;

                CmdLine.PromptForInteger("Nb de points maximum ", NbPoint, out NbPoint);
                CmdLine.PromptForDouble("Jeu entre les cercles ", Jeu, out Jeu);
                CmdLine.PromptForDouble("Supprimer les cercles de diam inf. � ", DiamMin, out DiamMin);
                CmdLine.PromptForDouble("R�duire les cercles de diam sup. � ", DiamMax, out DiamMax);
                CmdLine.PromptForInteger("Seuil mini pour les noirs (0 � 255) ", SeuilNoirs, out SeuilNoirs);
                CmdLine.PromptForInteger("Blanc, % du diam mini (0 � 100)", BlancPctDiam, out BlancPctDiam);
                CmdLine.PromptForInteger("Type de sampler : 1 -> Poisson / 2 -> Rejection ", TypeSampler, out TypeSampler);
                CmdLine.PromptForInteger("Nb d'iteration pour l'affinage ", NbAffinage, out NbAffinage);
                CmdLine.PromptForBool("Dessiner le maillage et les points d'origine ", "Oui", "Non", MaillageEtPoint, out MaillageEtPoint);

                if (CmdLine.PromptForSelection(true, "Selectionnez l'image", "Ce n'est pas une image"))
                {
                    dsObjectType_e entityType;
                    var count = SlM.GetSelectedObjectCount(dsSelectionSetType_e.dsSelectionSetType_Previous);

                    object selectedEntity = SlM.GetSelectedObject(dsSelectionSetType_e.dsSelectionSetType_Previous, 0, out entityType);

                    if (dsObjectType_e.dsReferenceImageType == entityType)
                        Image = (ReferenceImage)selectedEntity;
                }

                //object ObjType = null;
                //object ObjEntites = null;
                //string[] TabNomsCalques = GetTabNomsCalques(DsDoc);
                //SkM.GetEntities(SlFilter, TabNomsCalques, out ObjType, out ObjEntites);
                //object[] TabEntites = ObjEntites as object[];
                //Image = (ReferenceImage)TabEntites[0];

                TimeSpan t; DateTime DateTimeStart;

                DateTimeStart = DateTime.Now;

                if (Image != null)
                {
                    CmdLine.PrintLine(String.Format("Image : {0}", Image.GetPath()));

                    Double ImgX, ImgY, ImgZ;
                    Image.GetPosition(out ImgX, out ImgY, out ImgZ);

                    CmdLine.PrintLine("Sampler");
                    Log.Message("Sampler");

                    List<PointF> listePoint;
                    if (TypeSampler == 1)
                    {
                        Double fact1 = 2;
                        Double fact2 = 0.7;
                        CmdLine.PromptForDouble("Facteur multip. du rayon de rejection ", fact1, out fact1);
                        CmdLine.PromptForDouble("Facteur multip. du rayon minimum � l'initialisation ", fact2, out fact2);
                        listePoint = BitmapPoissonSampler.Run(Image, NbPoint, fact1, fact2);
                    }
                    else
                        listePoint = BitmapRejectionSampler.Run(Image, NbPoint);

                    dsCreateObjectResult_e res;
                    var CalquePoint = LyM.GetLayer("Point");
                    if (CalquePoint == null)
                    {
                        LyM.CreateLayer("Point", out CalquePoint, out res);
                        var c = CalquePoint.Color;
                        c.SetColorByIndex(230);
                        CalquePoint.Color = c;
                    }

                    if (MaillageEtPoint)
                    {
                        CalquePoint.Activate();
                        foreach (var pc in listePoint)
                            SkM.InsertCircleByDiameter(ImgX + pc.X, ImgY + Image.Height - pc.Y, 0, 2);
                    }
                    

                    CmdLine.PrintLine("Sampler termin�");
                    Log.Message("Sampler termin�");

                    Log.Message("Equilibrage des points");

                    VoronoiMap.VoronoiGraph graph;
                    var listeSitePoincon = VoronoiEquilibreur.Start(Image, listePoint, NbAffinage, out graph);

                    Log.Message("Equilibrage termin�");

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

                    var facteurGris = (100 - BlancPctDiam) / (255 - SeuilNoirs);
                    var DiamMiniDessin = Double.PositiveInfinity;
                    var DiamMaxiDessin = 0.0;

                    foreach (var pc in listeSitePoincon)
                    {
                        var diam = pc.CercleInscrit - (Jeu * 0.5);
                        var reduce = (BlancPctDiam + (255 - pc.GrisCercleInscrit) * facteurGris) / 100;
                        var diamReduce = Math.Min(DiamMax, diam * reduce);

                        if (pc.GrisCercleInscrit > SeuilNoirs && diamReduce >= DiamMin)
                        {
                            //Log.Message("� : " + diam + " / f : " + reduce + " / �red : " + diamReduce);
                            DiamMiniDessin = Math.Min(DiamMiniDessin, diamReduce);
                            DiamMaxiDessin = Math.Max(DiamMaxiDessin, diamReduce);
                            var cercle = SkM.InsertCircleByDiameter(ImgX + pc.Site.X, ImgY + Image.Height - pc.Site.Y, 0, diamReduce);
                            ListeCercles.Add(cercle);
                        }
                    }
                    var format = String.Format("Nb de percages : {0} / DiamMaxi : {1:0.0} / DiamMini : {2:0.0}", ListeCercles.Count, DiamMaxiDessin, DiamMiniDessin);
                    CmdLine.PrintLine(format);
                    Log.Message(format);

                    CalqueHachures.Activate();

                    foreach (var item in ListeCercles)
                    {
                        var ent = new DispatchWrapper[1] { new DispatchWrapper(item) };
                        SkM.InsertHatchByEntities(ent, "SOLID", 1, 0);
                    }

                    if (MaillageEtPoint)
                    {
                        CalqueMaillage.Activate();
                        foreach (var s in graph.Segments)
                            SkM.InsertLine(ImgX + s.P1.X, ImgY + Image.Height - s.P1.Y, 0, ImgX + s.P2.X, ImgY + Image.Height - s.P2.Y, 0);
                    }

                    LyM.GetLayer("0").Activate();

                    CalqueMaillage.Shown = false;
                    CalquePoint.Shown = false;
                    CalquePoincon.Shown = false;
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