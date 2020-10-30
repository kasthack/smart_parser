using log4net;
using log4net.Appender;

using System;
using System.Collections.Generic;
using System.Reflection;

namespace TI.Declarator.ParserCommon
{
    public class Logger
    {
        private static readonly log4net.Repository.ILoggerRepository repo = LogManager.GetRepository(Assembly.GetEntryAssembly());

        private static void LoadConfig(string resourceName = "ParserCommon.Resources.log4net.config") // "Smart.Parser.Lib.Resources.log4net.config")
        {
            var debug = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            var currentAssembly = Assembly.GetExecutingAssembly();
            using var stream = currentAssembly.GetManifestResourceStream(resourceName);
            log4net.Config.XmlConfigurator.Configure(repo, stream);
        }

        public static void Setup(string logFileName = "", bool skipLogging = false)
        {
            Errors.Clear();
            if (skipLogging)
            {
                mainLog = null;
                secondLog = null;
            }
            else
            {
                if (string.IsNullOrEmpty(logFileName))
                {
                    logFileName = "smart_parser_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".log";
                }

                LoadConfig();
                SetLogFileName("Main", logFileName);
                mainLog = LogManager.GetLogger(repo.Name, "Main");
                secondLog = LogManager.GetLogger(repo.Name, "Second");
            }

            log = mainLog;
        }

        public static void SetupForTests(string mainLogName, string auxLogName)
        {
            mainLog = LogManager.GetLogger(repo.Name, mainLogName);
            secondLog = LogManager.GetLogger(repo.Name, "auxLogName");
            log = mainLog;
        }

        public static void SetSecondLogFileName(string logFileName)
        {
            if (secondLog != null)
            {
                SetLogFileName("Second", logFileName);
            }
        }

        public static void SetLogFileName(string logger, string logFileName)
        {
            var found = false;
            Errors.Clear();
            var appenders = repo.GetAppenders();
            foreach (var a in appenders)
            {
                if (a is FileAppender appender && a.Name == logger)
                {
                    appender.AppendToFile = false;
                    appender.File = logFileName;
                    appender.ActivateOptions();
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                throw new Exception("Cannot find Appender " + logger);
            }
        }

        public enum LogLevel
        {
            Debug = 0,
            Info,
            Error,
        }

        public static void SetLoggingLevel(LogLevel level)
        {
            log4net.Core.Level[] levels = { log4net.Core.Level.Debug, log4net.Core.Level.Info, log4net.Core.Level.Error };
            var hier = (log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository(Assembly.GetExecutingAssembly());
            hier.Root.Level = levels[(int)level];
            hier.RaiseConfigurationChanged(EventArgs.Empty);
            foreach (var logger in hier.GetCurrentLoggers())
            {
                ((log4net.Repository.Hierarchy.Logger)logger).Level = levels[(int)level];
            }
        }

        public static ILog Log => log;

        public static ILog SecondLog => secondLog;

        private static ILog log;
        private static ILog mainLog;
        private static ILog secondLog;

        public static void SetOutMain() => log = mainLog;

        public static void SetOutSecond() => log = secondLog;

        public static void Debug(string info, params object[] par)
        {
            if (log == null)
            {
                return;
            }

            log.Debug(string.Format(info.Replace("{", string.Empty).Replace("}", string.Empty), par));
        }

        public static void Info(string info, params object[] par)
        {
            if (log == null)
            {
                return;
            }

            log.Info(string.Format(info, par));
        }

        public static void Info(string info)
        {
            if (log == null)
            {
                return;
            }

            log.Info(string.Format(info));
        }

        public static void UnknownRealEstateType(string info) => UnknownRealEstate.Add(info);

        public static void Error(int row, string info, params object[] par)
        {
            if (log == null)
            {
                return;
            }

            var message = string.Format(info, par);
            log.Error($"row {row}: {message}");
            Errors.Add(message);
        }

        public static void Error(string info, params object[] par)
        {
            if (log == null)
            {
                return;
            }

            var message = string.Format(info, par);
            log.Error(message);
            Errors.Add(message);
        }

        public static void Info2(string info, params object[] par) => secondLog.Info(string.Format(info, par));

        public static void Info2(string info) => secondLog.Info(string.Format(info));

        public static void Error2(string info, params object[] par)
        {
            var message = string.Format(info, par);
            secondLog.Error(message);
            Errors.Add(message);
        }

        public static HashSet<string> Errors { get; } = new HashSet<string>();

        public static HashSet<string> UnknownRealEstate { get; } = new HashSet<string>();
    }
}
