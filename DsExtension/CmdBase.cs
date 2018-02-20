using DraftSight.Interop.dsAutomation;
using System;

namespace Cmds
{
    public abstract class CmdBase
    {
        protected DraftSight.Interop.dsAutomation.Application DsApp = null;
        protected  MathUtility dsMathUtility = null;
        protected Command m_command = null;
        protected UserCommand m_user_command = null;
        protected string m_user_command_id = "";
        protected string m_group_name = "";

        public CmdBase(DraftSight.Interop.dsAutomation.Application app, string groupName)
        {
            DsApp = app;
            MathHelper.Init(app.GetMathUtility());
            m_group_name = groupName;
        }

        public abstract void Execute();

        public abstract string globalName();
        public abstract string localName();
        public abstract string Description();
        public abstract string ItemName();
        public virtual string SmallIcon() { return ""; }
        public virtual string LargeIcon() { return ""; }
        public string UserCommandID() { return m_user_command_id; }

        // Registers the command, so it can be used from the
        // command line by typing the command's global or local name.
        public dsCreateCommandError_e registerCommand()
        {
            dsCreateCommandError_e error;
            m_command = DsApp.CreateCommand2(
                m_group_name,
                globalName(),
                localName(),
                out error);
            if (error == dsCreateCommandError_e.dsCreateCommand_Succeeded && m_command != null)
                m_command.ExecuteNotify += Execute;

            createUserCommand();

            return error;
        }

        private dsCreateCommandError_e createUserCommand(dsUIState_e uiState)
        {
            dsCreateCommandError_e error;
            m_user_command_id = "";
            m_user_command = DsApp.CreateUserCommand(
                m_group_name,
                localName(),
                "^C^C" + localName(),
                Description(),
                SmallIcon(),
                LargeIcon(),
                uiState,
                out error);
            if (error == dsCreateCommandError_e.dsCreateCommand_Succeeded)
                m_user_command_id = m_user_command.GetID();
            return error;
        }

        public dsCreateCommandError_e createUserCommand()
        {
            return createUserCommand(dsUIState_e.dsUIState_Document);
        }

        public ContextMenuItem getContextMenuItem(string name)
        {
            ContextMenuItem result = null;

            //Get all menus
            object[] menusArray = (object[])DsApp.GetContextMenuItems();
            if (null != menusArray)
            {
                foreach (object menu in menusArray)
                {
                    ContextMenuItem menuObj = menu as ContextMenuItem;
                    if (menuObj.Name == name)
                    {
                        result = menuObj;
                        break;
                    }
                }
            }

            return result;
        }

        protected string GetSimplestTimeSpan(TimeSpan timeSpan)
        {
            var result = string.Empty;
            if (timeSpan.Days > 0)
            {
                result += string.Format(
                    @"{0:ddd\d}", timeSpan).TrimStart('0');
            }
            if (timeSpan.Hours > 0)
            {
                result += string.Format(
                    @"{0:hh\h}", timeSpan).TrimStart('0');
            }
            if (timeSpan.Minutes > 0)
            {
                result += string.Format(
                    @"{0:mm\m}", timeSpan).TrimStart('0');
            }
            if (timeSpan.Seconds >= 1)
            {
                result += string.Format(@"{0:ss\s}", timeSpan).TrimStart('0');
            }
            if (timeSpan.TotalSeconds < 1)
            {
                result += "0s";
            }

            if (timeSpan.Milliseconds > 0 && (timeSpan.TotalSeconds <= 20))
            {
                result += string.Format(
                    @"{0:fff}", timeSpan).TrimStart('0');
            }
            return result;
        }
    }
}
