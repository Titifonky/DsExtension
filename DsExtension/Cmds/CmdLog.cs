using DraftSight.Interop.dsAutomation;
using LogDebugging;
using System;
using System.IO;
using System.Reflection;

namespace Cmds
{
    public class CmdLog : CmdBase
    {
        public CmdLog(DraftSight.Interop.dsAutomation.Application app, string groupName) : base(app, groupName)
        { }
        public override string globalName() { return "_MAffLog"; }
        public override string localName() { return "AffLog"; }

        public override string Description() { return "Afficher le log de debug"; }
        public override string ItemName() { return "Afficher Log"; }

        public override void Execute()
        {
            try
            {
                String Dossier = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Log)).Location);
                String FichierLog = Path.Combine(Dossier, "LOGs.log");
                System.Diagnostics.Process.Start(FichierLog);
            }
            catch (Exception e)
            { Log.Write(e); }
        }
    }
}