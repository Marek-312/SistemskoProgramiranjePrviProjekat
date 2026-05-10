using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace PalindromeServer
{
    public class RequestQueue
    {
        private readonly Queue<HttpListenerContext> _queue;
        private readonly object _queueLock = new object();
        private readonly int _maxSize;
        private bool _isStopped = false;

        public RequestQueue(int maxSize = 100)
        {
            _maxSize = maxSize;
            _queue = new Queue<HttpListenerContext>();
        }

        public bool EnqueueContext(HttpListenerContext context)
        {
            lock (_queueLock)
            {
                if (_isStopped) return false;

                while (_queue.Count >= _maxSize && !_isStopped)
                {
                    Logger.Log("RED PUN: Cekam mesto...");
                    Monitor.Wait(_queueLock);
                }

                if (_isStopped) return false;

                _queue.Enqueue(context);
                Logger.Log($"RED: Dodat zahtev, u redu: {_queue.Count}");
                Monitor.Pulse(_queueLock);
                return true;
            }
        }

        public HttpListenerContext Dequeue()
        {
            lock (_queueLock)
            {
                while (_queue.Count == 0 && !_isStopped)
                {
                    Logger.Log("RED PRAZAN: Worker ceka...");
                    Monitor.Wait(_queueLock);
                }

                if (_queue.Count == 0) return null;

                HttpListenerContext context = _queue.Dequeue();
                Logger.Log($"RED: Uzet zahtev, ostalo: {_queue.Count}");
                Monitor.Pulse(_queueLock);
                return context;
            }
        }

        public void Stop()
        {
            lock (_queueLock)
            {
                _isStopped = true;
                Monitor.PulseAll(_queueLock);
                Logger.Log("RED: Zaustavljanje");
            }
        }

        public int Count
        {
            get { lock (_queueLock) { return _queue.Count; } }
        }
    }
}