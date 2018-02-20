using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Repository;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LogDebugging
{
    [Flags]
    internal enum LogLevelL4N
    {
        DEBUG = 1,
        ERROR = 2,
        FATAL = 4,
        INFO = 8,
        WARN = 16
    }

    internal static class Log
    {
        private static readonly ILog _Logger = LogManager.GetLogger("DLL");

        private static Boolean _EstInitialise = false;

        private static Boolean _Actif = true;

        static Log()
        {
            String Dossier = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Log)).Location);
            String Chemin = Dossier + @"\" + "log4net.config";
            XmlConfigurator.Configure(_Logger.Logger.Repository, new FileInfo(Chemin));

            IAppender[] appenders = _Logger.Logger.Repository.GetAppenders();
            foreach (IAppender appender in appenders)
            {
                FileAppender fileAppender = appender as FileAppender;

                String CheminFichier = Path.Combine(Dossier, Path.GetFileName(fileAppender.File));
                if (File.Exists(CheminFichier))
                    File.Delete(CheminFichier);

                fileAppender.File = Path.Combine(Dossier, Path.GetFileName(fileAppender.File));
                fileAppender.ActivateOptions();
            }
        }

        internal static void Demarrer()
        {
            Activer = true;
            Entete();
        }

        internal static void Stopper()
        {
            IAppender[] appenders = _Logger.Logger.Repository.GetAppenders();
            foreach (IAppender appender in appenders)
            {
                appender.Close();
            }
            _Logger.Logger.Repository.Shutdown();
        }

        internal static void Entete()
        {
            if (_EstInitialise)
                return;

            Write("\n ");
            Write("====================================================================================================");
            Write("|                                                                                                  |");
            Write("|                                          DEBUG                                                   |");
            Write("|                                                                                                  |");
            Write("====================================================================================================");
            Write("\n ");

            _EstInitialise = true;
        }

        internal static Boolean Activer
        {
            get
            {
                return _Actif;
            }
            set
            {
                _Actif = value;

                log4net.Core.Level pLevel = log4net.Core.Level.Debug;
                if (value)
                    pLevel = log4net.Core.Level.All;

                ILoggerRepository repository = _Logger.Logger.Repository;
                repository.Threshold = pLevel;

                ((log4net.Repository.Hierarchy.Logger)_Logger.Logger).Level = pLevel;

                log4net.Repository.Hierarchy.Hierarchy h = (log4net.Repository.Hierarchy.Hierarchy)repository;
                log4net.Repository.Hierarchy.Logger rootLogger = h.Root;
                rootLogger.Level = pLevel;

            }
        }

        internal static void Write(Object Message, LogLevelL4N Level = LogLevelL4N.DEBUG)
        {
            try
            {
                if (Level.Equals(LogLevelL4N.DEBUG))
                    _Logger.Debug(Message.ToString());
                else if (Level.Equals(LogLevelL4N.ERROR))
                    _Logger.Error(Message.ToString());
                else if (Level.Equals(LogLevelL4N.FATAL))
                    _Logger.Fatal(Message.ToString());
                else if (Level.Equals(LogLevelL4N.INFO))
                    _Logger.Info(Message.ToString());
                else if (Level.Equals(LogLevelL4N.WARN))
                    _Logger.Warn(Message.ToString());
            }
            catch { }
        }

        internal static void Message(Object message)
        {
            if (!_Actif)
                return;

            Write("\t\t\t\t-> " + message.ToString());
        }

        internal static void LogMethode(this Object O, Object[] Message, [CallerMemberName] String methode = "")
        {
            if (!_Actif)
                return;

            Write("\t\t\t" + O.GetType().Name + "." + methode + "  ->  " + String.Join(" ", Message));
        }

        internal static void LogMethode(this Object O, [CallerMemberName] String methode = "")
        {
            Methode(O.GetType().Name, methode);
        }

        internal static void LogResultat(this Object O, String Text, [CallerMemberName] String methode = "")
        {
            if (!_Actif)
                return;

            Write("\t\t\t Resultat dans " + methode + "  " + Text + " : " + O.ToString());
        }

        internal static void Methode(String nomClasse, [CallerMemberName] String methode = "")
        {
            if (!_Actif)
                return;

            Write("\t\t\t" + nomClasse + "." + methode);
        }

        internal static void Methode(String nomClasse, Object message, [CallerMemberName] String methode = "")
        {
            if (!_Actif)
                return;

            Write("\t\t\t" + nomClasse + "." + methode);
            if (message != null)
                Write("\t\t\t\t-> " + message.ToString());
        }

        internal static void Methode<T>([CallerMemberName] String methode = "")
        {
            Methode(typeof(T).ToString(), methode);
        }

        internal static void Methode<T>(Object message, [CallerMemberName] String methode = "")
        {
            Methode(typeof(T).ToString(), message, methode);
        }
    }
}