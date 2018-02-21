using DraftSight.Interop.dsAddin;
using DraftSight.Interop.dsAutomation;
using LogDebugging;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DsExtension
{
    [Guid("AB105651-116F-4CCD-AA7D-405A6EC771A3"), ComVisible(true)]
    public class dsAddin : DsAddin
    {
        private DraftSight.Interop.dsAutomation.Application AppDs = null;
        private string AddinGUID = "";
        private List<Cmds.CmdBase> ListeCmds = new List<Cmds.CmdBase>();

        public dsAddin()
        {
            AddinGUID = this.GetType().GUID.ToString();
        }

        public string GUID
        {
            get { return AddinGUID; }
        }

        public DraftSight.Interop.dsAutomation.Application dsApplication
        {
            get { return AppDs; }
        }

        private void CreateUserInterfaceAndCommands()
        {
            try
            {
                WorkSpace dsWorkSpace = AppDs.GetWorkspace("Drafting and Annotation");

                dsWorkSpace.Activate();

                object[] objects = (object[])dsWorkSpace.GetRibbonTabs();
                int length = objects.Length + 1;

                ////Add a New Tab
                RibbonTab ribbonTab = dsWorkSpace.AddRibbonTab(AddinGUID, length, "DsExtension", "DsExtension");

                if (ribbonTab != null)
                {
                    RibbonPanel Panneau; RibbonRow LigneBase; RibbonRowPanel Colonne; RibbonRow Ligne; RibbonCommandButton Btn;

                    Panneau = ribbonTab.InsertRibbonPanel(AddinGUID, 1, "Sw", "Sw");
                    LigneBase = Panneau.InsertRibbonRow(AddinGUID, "A");
                    Colonne = LigneBase.InsertRibbonRowPanel(AddinGUID, "AA");

                    // Première colonne
                    Ligne = Colonne.InsertRibbonRow(AddinGUID, "AAA");
                    var CmdNettoyerDvp = new Cmds.CmdNettoyerDvp(AppDs, AddinGUID);
                    CmdNettoyerDvp.registerCommand();
                    ListeCmds.Add(CmdNettoyerDvp);
                    Btn = Ligne.InsertRibbonCommandButton(AddinGUID, dsRibbonButtonStyle_e.dsRibbonButtonStyle_SmallWithText, CmdNettoyerDvp.ItemName(), CmdNettoyerDvp.UserCommandID());

                    Ligne = Colonne.InsertRibbonRow(AddinGUID, "AAB");
                    var CmdSimplifierSpline = new Cmds.CmdSimplifierSpline(AppDs, AddinGUID);
                    CmdSimplifierSpline.registerCommand();
                    ListeCmds.Add(CmdSimplifierSpline);
                    Btn = Ligne.InsertRibbonCommandButton(AddinGUID, dsRibbonButtonStyle_e.dsRibbonButtonStyle_SmallWithText, CmdSimplifierSpline.ItemName(), CmdSimplifierSpline.UserCommandID());

                    Ligne = Colonne.InsertRibbonRow(AddinGUID, "AAC");
                    var CmdEffacerGravure = new Cmds.CmdEffacerGravure(AppDs, AddinGUID);
                    CmdEffacerGravure.registerCommand();
                    ListeCmds.Add(CmdEffacerGravure);
                    Btn = Ligne.InsertRibbonCommandButton(AddinGUID, dsRibbonButtonStyle_e.dsRibbonButtonStyle_SmallWithText, CmdEffacerGravure.ItemName(), CmdEffacerGravure.UserCommandID());

                    // Deuxième colonne

                    Panneau = ribbonTab.InsertRibbonPanel(AddinGUID, 2, "Dev", "Dev");
                    LigneBase = Panneau.InsertRibbonRow(AddinGUID, "R");
                    Colonne = LigneBase.InsertRibbonRowPanel(AddinGUID, "RR");

                    Ligne = Colonne.InsertRibbonRow(AddinGUID, "RRR");
                    var Cmdlog = new Cmds.CmdLog(AppDs, AddinGUID);
                    Cmdlog.registerCommand();
                    ListeCmds.Add(Cmdlog);
                    Btn = LigneBase.InsertRibbonCommandButton(AddinGUID, dsRibbonButtonStyle_e.dsRibbonButtonStyle_SmallWithText, Cmdlog.ItemName(), Cmdlog.UserCommandID());

                    Ligne = Colonne.InsertRibbonRow(AddinGUID, "RRS");
                    var CmdTest = new Cmds.CmdTest(AppDs, AddinGUID);
                    CmdTest.registerCommand();
                    ListeCmds.Add(CmdTest);
                    Btn = LigneBase.InsertRibbonCommandButton(AddinGUID, dsRibbonButtonStyle_e.dsRibbonButtonStyle_SmallWithText, CmdTest.ItemName(), CmdTest.UserCommandID());
                }
            }
            catch (Exception e)
            { Log.Write(e); }
        }

        private void RemoveUserInterface()
        {
            //object dsWSObj = application.GetWorkspaces( dsUIState_e.dsUIState_Document );
            //object[] dsWSArr = (object[])dsWSObj;
            //for( int i = 0; i < dsWSArr.Length; ++i )
            //{
            //    WorkSpace dsWorkspace = (WorkSpace)dsWSArr[i];
            //    if ( dsWorkspace != null )
            //    {
            //        string nameWS = dsWorkspace.Name;
            //        if ( nameWS.CompareTo( "Drafting and Annotation" ) == 0)
            //        {
            //            object dsOb = dsWorkspace.GetRibbonTabs( );
            //            object[] dsObArr = (object[])dsOb;
            //            for( int ip = 0; ip < dsObArr.Length; ++ip )
            //            {
            //                RibbonTab ribbonsTab = (RibbonTab)dsObArr[ip];
            //                if( ribbonsTab != null )
            //                {
            //                    string name = ribbonsTab.Name;
            //                    string displayText = ribbonsTab.DisplayText;
            //                    string apiID = ribbonsTab.GetApiID();
            //                    if ( name.CompareTo( "RibbonSampleTab") == 0 && 
            //                        displayText.CompareTo( "RibbonSampleTab") == 0 && 
            //                        apiID.CompareTo( m_AddinGUID ) == 0 )
            //                    {
            //                        object dsObjRbPanels = ribbonsTab.GetRibbonPanels( );
            //                        object[] dsRbPanelsArr = (object[])dsObjRbPanels;

            //                        for( int j = 0; j < dsRbPanelsArr.Length; ++j )
            //                        {
            //                            RibbonPanel dsRibbonPanel = (RibbonPanel)dsRbPanelsArr[j];
            //                            if( dsRibbonPanel != null )
            //                            {
            //                                dsRibbonItemType_e ItemType = dsRibbonPanel.GetType( );
            //                                if( ItemType == dsRibbonItemType_e.dsRibbonItemType_Panel )
            //                                {
            //                                    name = dsRibbonPanel.Name;
            //                                    string displayTxt = dsRibbonPanel.DisplayText;
            //                                    if (name.CompareTo("SamplePannel") == 0 && displayTxt.CompareTo("SamplePannel") == 0)
            //                                    {
            //                                        RibbonRow dsRibbonRow = dsRibbonPanel.GetRibbonRow( );
            //                                        if ( dsRibbonRow != null )
            //                                        {
            //                                            dsRibbonRow.Remove();
            //                                            dsRibbonPanel.RemoveFromTab("Manage");
            //                                            dsRibbonPanel.Remove();
            //                                            ribbonsTab.Remove();
            //                                        }
            //                                    }
            //                                }
            //                            }
            //                        }
            //                    }
            //                    else if ( name.CompareTo( "Manage") == 0 )
            //                    {
            //                        object dsObjRibbonPanelsArr = ribbonsTab.GetRibbonPanels();
            //                        object[] dsRibbonPanelsArr = (object[])dsObjRibbonPanelsArr;

            //                        for( int it = 0; it < dsRibbonPanelsArr.Length; ++it )
            //                        {
            //                            RibbonPanel dsRibbonPanel = (RibbonPanel)dsRibbonPanelsArr[it];
            //                            if ( dsRibbonPanel != null )
            //                            {
            //                                string panelName = dsRibbonPanel.GetApiID();
            //                                if( panelName.CompareTo("ID_PanelMacro") == 0 )
            //                                {
            //                                    RibbonRow dsRibbonRow = dsRibbonPanel.GetRibbonRow();
            //                                    object dsObjRibbonRow = dsRibbonRow.GetRibbonRowItems();
            //                                    object[] dsRibbonRowArr = (object[])dsObjRibbonRow;

            //                                    for( int ig = 0; ig < dsRibbonRowArr.Length; ++ig )
            //                                    {
            //                                        RibbonRowItem dsRibbonRowItem = (RibbonRowItem)dsRibbonRowArr[ig];
            //                                        dsRibbonItemType_e ItemType = dsRibbonRowItem.GetType();
            //                                        if( ItemType == dsRibbonItemType_e.dsRibbonItemType_CommandButton )
            //                                        {
            //                                            string nameRow = dsRibbonRowItem.Name;
            //                                            if (nameRow.CompareTo("Hello") == 0)
            //                                            {
            //                                                dsRibbonRowItem.Remove();
            //                                            }
            //                                        }
            //                                    }                         
            //                                }
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}
        }

        public bool ConnectToDraftSight(object DsApp, int Cookie)
        {
            Log.Demarrer();

            AppDs = DsApp as DraftSight.Interop.dsAutomation.Application;
            if (AppDs == null)
                return false;
            CreateUserInterfaceAndCommands();
            return true;
        }

        public bool DisconnectFromDraftSight()
        {
            RemoveUserInterface();
            AppDs.RemoveUserInterface(AddinGUID);
            Log.Stopper();
            AppDs = null;
            return true;
        }
    }
}
