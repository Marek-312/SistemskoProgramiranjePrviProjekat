using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
namespace PalindromeServer
{
    public class WebServer
    {
        private readonly HttpListener _listener;

        private readonly RequestQueue _queue;

        private readonly Cache _cache;
        private readonly string _rootFolder;
        private readonly int _workerCount;
        private HttpListenerContext _context;
        private bool _isRunning = false;
        public WebServer(string rootFolder, int workerCount = 4, int cacheSize = 10)
        {
            _rootFolder = rootFolder;
            _workerCount = workerCount;
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:5050/");
            _queue = new RequestQueue(100);
            _cache = new Cache(cacheSize);
        }


        public void Start()
        {
            _isRunning = true;
            try
            {
                _isRunning = true;
                _listener.Start();
                Logger.Log("Server pokrenut na http://localhost:5050/");

                // pokrecemo worker niti
                for (int i = 0; i < _workerCount; i++)
                {
                    ThreadPool.QueueUserWorkItem(_ => WorkerLoop());
                }

                // glavna nit prima zahteve
                AcceptLoop();
            }
            catch (HttpListenerException ex)
            {
                Logger.Log($"Error: {ex.Message}");
            }
            finally
            {
                _listener.Stop();
            }
        }
        public void AcceptLoop()
        {
            while (_isRunning)
            {
                try
                {
                    _context = _listener.GetContext();
                    string fileName = _context.Request.Url.AbsolutePath.TrimStart('/');
                    Logger.Log($"ZAHTEV PRIMLJEN: '{fileName}'");

                    _queue.EnqueueContext(_context);
                }
                catch (HttpListenerException ex)
                {
                    if (_isRunning)
                        Logger.Log($"Greska pri prijemu:{ex.Message}");

                }
                catch (Exception ex)
                {
                    Logger.Log($"Greska pri prijemu:{ex.Message}");

                }

            }
        }
        public void WorkerLoop()
        {
            Logger.Log("Worker nit pokrenuta");
            while (true)
            {
                HttpListenerContext context = _queue.Dequeue();
                if (context == null) break; // server se gasi
                ProcessRequest(context);
            }
            Logger.Log("Worker nit zavrsila");
        }
        private void ProcessRequest(HttpListenerContext context)
        {
            string fileName = context.Request.Url.AbsolutePath.TrimStart('/');
            Logger.Log($"OBRADA: '{fileName}'");
            try
            {
                string filePath = FindFile(fileName);
                if (filePath == null)
                {
                    SendResponse(context, $"Greska: Fajl '{fileName}' nije pronadjen!", 404);
                    return;
                }
                Logger.Log($"POZIVAM GetOrCompute...");
                int count = _cache.GetOrCompute(fileName, () => CountPalindromes(filePath));
                Logger.Log($"GetOrCompute ZAVRSIO: {count}");

                if (count == 0)
                    SendResponse(context, $"Fajl '{fileName}' ne sadrzi palindrome.", 200);
                else
                    SendResponse(context, $"Fajl '{fileName}' sadrzi {count} palindroma.", 200);
            }
            catch (Exception ex)
            {
                Logger.Log($"GRESKA pri obradi '{fileName}': {ex.Message}");
                SendResponse(context, $"Greska: {ex.Message}", 500);
            }
        }
        private void SendResponse(HttpListenerContext context, string _responseMessage, int errCode)
        {
            try
            {
                HttpListenerResponse response = context.Response;
                response.StatusCode = errCode;
                byte[] buffer = Encoding.UTF8.GetBytes(_responseMessage);
                response.ContentLength64 = buffer.Length;
                using (System.IO.Stream output = response.OutputStream)
                {
                    output.Write(buffer, 0, buffer.Length);
                    Logger.Log($"ODGOVOR POSLAT: {_responseMessage}");
                }
                response.Close();
            }
            catch (Exception ex)
            {
                Logger.Log($"GRESKA pri slanju odgovora: {ex.Message}");
            }
        }
        private string FindFile(string fileName)
        {
            try
            {
                string[] files = Directory.GetFiles(
                    _rootFolder,
                    fileName,
                    SearchOption.AllDirectories
                );

                if (files.Length > 0)
                {
                    Logger.Log($"FAJL PRONADJEN: {files[0]}");
                    return files[0];
                }

                Logger.Log($"FAJL NIJE PRONADJEN: '{fileName}'");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Log($"GRESKA pri pretrazivanju: {ex.Message}");
                return null;
            }
        }
        private int CountPalindromes(string filePath)
        {
            Logger.Log($"BROJANJE palindroma u: {filePath}");
            Thread.Sleep(2000);
            string content = File.ReadAllText(filePath);
            string[] words = content.Split(new char[]
            {
                ' ', '\n', '\r', '\t', '.', ',', '!', '?', ':', ';'
            }, StringSplitOptions.RemoveEmptyEntries);

            int count = 0;
            foreach (string word in words)
            {
                string clean = word.ToLower();
                if (clean.Length > 1 && IsPalindrome(clean))
                {
                    count++;
                    Logger.Log($"PALINDROM: '{clean}'");
                }
            }

            Logger.Log($"UKUPNO palindroma: {count}");
            return count;
        }

        private bool IsPalindrome(string word)
        {
            int left = 0;
            int right = word.Length - 1;
            while (left < right)
            {
                if (word[left] != word[right])
                    return false;
                left++;
                right--;
            }
            return true;
        }
        public void Stop()
        {
            _isRunning = false;
            _listener.Stop();
            _queue.Stop();
            Logger.Log("Server zaustavljen");
        }
    }
}
