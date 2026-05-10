using System;
using System.IO;
using System.Threading;
namespace PalindromeServer
{
    public class Logger
    {
        private static readonly object logLock = new object();
        private static readonly string logFile = "server.log";
        public static void Log(string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}][Nit-{Thread.CurrentThread.ManagedThreadId}] {message}";
            //Console.WriteLine(line);
            lock (logLock)
            {
                File.AppendAllText(logFile, line + Environment.NewLine);
            }


        }
    }
}