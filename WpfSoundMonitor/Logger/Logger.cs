using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace WpfSoundMonitor.Logs
{
    public static class Logger
    {
        private const bool DELETE_OLD_LOGS = true;
        private const string LOG_DIR = @"Logs";

        private const string LOGS_LOG_PATH = $@"{LOG_DIR}\logs.log";
        private const string DEBUG_LOG_PATH = @$"{LOG_DIR}\debug.log";
        private const string INFO_LOG_PATH = @$"{LOG_DIR}\info.log";
        private const string WARN_LOG_PATH = @$"{LOG_DIR}\warn.log";
        private const string ERROR_LOG_PATH = @$"{LOG_DIR}\error.log";

        private const bool APPEND = true;

        private static readonly Encoding ENCODING = Encoding.Default;

        private const string LVL_DEBUG  = "[ DEBUG ]";
        private const string LVL_INFO   = "[ INFO  ]";
        private const string LVL_WARN   = "[WARNING]";
        private const string LVL_ERROR  = "[ ERROR ]";

        private static StreamWriter _swLogs;
        private static StreamWriter _swDebug;
        private static StreamWriter _swInfo;
        private static StreamWriter _swWarn;
        private static StreamWriter _swError;

        static Logger()
        {
            if (!Directory.Exists(LOG_DIR))
                Directory.CreateDirectory(LOG_DIR);

            if (DELETE_OLD_LOGS)
            {
                DirectoryInfo dirInfos = new DirectoryInfo(LOG_DIR);

                foreach (FileInfo file in dirInfos.GetFiles())
                    file.Delete();

                foreach (DirectoryInfo dir in dirInfos.GetDirectories())
                    dir.Delete();
            }

            _swLogs = new StreamWriter(LOGS_LOG_PATH, APPEND, ENCODING);
            _swDebug = new StreamWriter(DEBUG_LOG_PATH, APPEND, ENCODING);
            _swInfo = new StreamWriter(INFO_LOG_PATH, APPEND, ENCODING);
            _swWarn = new StreamWriter(WARN_LOG_PATH, APPEND, ENCODING);
            _swError = new StreamWriter(ERROR_LOG_PATH, APPEND, ENCODING);
        }

        public static void Close()
        {
            _swDebug.Close();
            _swInfo.Close();
            _swWarn.Close();
            _swError.Close();
        }

        private static string CreateLogText(string level, string location, string message, Exception? ex = null)
        {
            string outLog = "";

            outLog += level;
            outLog += " ";
            outLog += DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss:FFFFFFF");
            outLog += " | ";
            outLog += location;
            outLog += " | ";
            outLog += message;
            
            if(ex != null)
            {
                outLog += " | ";
                outLog += ex.ToString();
            }

            return outLog;
        }

        public static void LogDebug(string location, string message, Exception? ex = null)
        {
            string text = CreateLogText(LVL_DEBUG, location, message, ex);

            _swLogs.WriteLine(text);
            _swLogs.Flush();

            _swDebug.WriteLine(text);
            _swDebug.Flush();
        }

        public static void LogInfo(string location, string message, Exception? ex = null)
        {
            string text = CreateLogText(LVL_INFO, location, message, ex);

            _swLogs.WriteLine(text);
            _swLogs.Flush();

            _swInfo.WriteLine(text);
            _swInfo.Flush();
        }

        public static void LogWarn(string location, string message, Exception? ex = null)
        {
            string text = CreateLogText(LVL_WARN, location, message, ex);

            _swLogs.WriteLine(text);
            _swLogs.Flush();

            _swWarn.WriteLine(text);
            _swWarn.Flush();
        }

        public static void LogError(string location, string message, Exception? ex = null)
        {
            string text = CreateLogText(LVL_ERROR, location, message, ex);

            _swLogs.WriteLine(text);
            _swLogs.Flush();

            _swError.WriteLine(text);
            _swError.Flush();
        }
    }
}
