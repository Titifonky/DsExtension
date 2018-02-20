using DraftSight.Interop.dsAutomation;
using LogDebugging;
using System;

namespace Cmds
{
    public class CmdEffacerGravure : CmdBase
    {
        public CmdEffacerGravure(DraftSight.Interop.dsAutomation.Application app, string groupName) : base(app, groupName)
        { }

        public override string globalName() { return "_MEffacerGravure"; }
        public override string localName() { return "MEffacerGravure"; }

        public override string Description() { return "Effacer les gravures"; }
        public override string ItemName() { return "Effacer Gravure"; }

        public override void Execute()
        {
            try
            {
                var DateTimeStart = DateTime.Now;

                DsApp.AbortRunningCommand();

                CommandMessage CmdLine = DsApp.GetCommandMessage();
                if (null == CmdLine) return;

                Document DsDoc = DsApp.GetActiveDocument();

                Model Mdl = DsDoc.GetModel();
                SketchManager SkMgr = Mdl.GetSketchManager();
                SelectionManager SlMgr = DsDoc.GetSelectionManager();

                object ObjType = null;
                object ObjEntites = null;
                Int32[] TabTypes = null;
                object[] TabEntites = null;
                string[] TabNomsCalques = null;


                EntityHelper dsEntityHelper = DsApp.GetEntityHelper();

                ///==============================================================================
                CmdLine.PrintLine("Suppression des gravures");
                TabNomsCalques = new string[] { "GRAVURE" , "Gravure" ,"gravure" };
                SkMgr.GetEntities(null, TabNomsCalques, out ObjType, out ObjEntites);

                TabTypes = (Int32[])ObjType;
                TabEntites = (object[])ObjEntites;

                foreach (var ent in TabEntites)
                    dsEntityHelper.SetErased(ent, true);

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