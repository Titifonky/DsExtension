using DraftSight.Interop.dsAutomation;
using LogDebugging;
using System;

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

                //==============================================================================
                CmdLine.PrintLine("Calque '0' actif");
                Document DsDoc = DsApp.GetActiveDocument();
                LayerManager LyMgr = DsDoc.GetLayerManager();
                Layer L0 = LyMgr.GetLayer("0");
                L0.Activate();

                // Creer les calques de pliage
                Color c;
                dsCreateObjectResult_e Erreur;

                //==============================================================================
                CmdLine.PrintLine("Création du claque 'LIGNES DE PLIAGE'");
                Layer LigneDePliage;
                LyMgr.CreateLayer("LIGNES DE PLIAGE", out LigneDePliage, out Erreur);
                c = LigneDePliage.Color;
                c.SetColorByIndex(252);
                LigneDePliage.Color = c;

                //==============================================================================
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

                //==============================================================================
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

                //==============================================================================
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

                //==============================================================================
                CmdLine.PrintLine("Conversion des textes à graver en lignes");
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

                //==============================================================================
                CmdLine.PrintLine("Purger le dessin");
                DsApp.RunCommand("_-CLEAN\n_All\n*\n_No\n", true);

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
    }
}