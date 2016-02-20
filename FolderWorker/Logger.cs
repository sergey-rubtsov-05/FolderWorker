using System;
using System.Diagnostics;

namespace FolderWorker
{
    public static class Logger
    {
        private const string LogSource = "FolderWorker";
        public static void Info(string message)
        {
            Console.WriteLine(message);
            EventLog.WriteEntry(LogSource, message, EventLogEntryType.Information);
        }

        public static void Warning(string message, bool silentMode)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.WriteLine("Работа будет прекращена");
            Console.ForegroundColor = ConsoleColor.White;
            EventLog.WriteEntry(LogSource, message, EventLogEntryType.Warning);
            if (!silentMode)
                Console.Read();

            Environment.Exit(0);
        }

        public static void Error(string message, bool silentMode)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.WriteLine("Работа будет прекращена");
            Console.ForegroundColor = ConsoleColor.White;
            EventLog.WriteEntry(LogSource, message, EventLogEntryType.Error);
            if (!silentMode)
                Console.Read();

            Environment.Exit(0);
        }
    }
}
