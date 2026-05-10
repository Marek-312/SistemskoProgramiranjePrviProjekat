using System;
using System.IO;

namespace PalindromeServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string s = "";
            string rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "Files");

            // napravimo folder ako ne postoji
            if (!Directory.Exists(rootFolder))
            {
                Directory.CreateDirectory(rootFolder);
                Logger.Log($"Napravljen root folder: {rootFolder}");
            }

            Logger.Log($"Root folder: {rootFolder}");

            // pravimo i pokrecemo server
            WebServer server = new WebServer(rootFolder, workerCount: 4, cacheSize: 10);

            // pokrecemo server na zasebnoj niti
            // jer AcceptLoop blokira glavnu nit
            Thread serverThread = new Thread(() => server.Start());
            serverThread.IsBackground = true;
            serverThread.Start();

            Console.WriteLine("=================================");
            Console.WriteLine("  Palindrome Server pokrenut!   ");
            Console.WriteLine("  http://localhost:5050/fajl.txt");
            Console.WriteLine("  Pritisni Enter za gasenje...  ");
            Console.WriteLine("=================================");

            // cekamo Enter da ugasimo server

            while (s != "Q")
            {
                s = (string)Console.ReadLine();
            }
            server.Stop();
            Logger.Log("Server ugasen");
            Console.WriteLine("Crtl + c");
        }
    }
}