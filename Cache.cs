using System.Threading;
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
namespace PalindromeServer
{
    public class Cache
    {
        private readonly Dictionary<string, int> _store;

        private readonly Queue<string> _order;

        private readonly HashSet<string> _inProgress;
        private readonly int _maxSize;
        private readonly object _cacheLock = new object();
        public Cache(int maxSize = 10)
        {
            _maxSize = maxSize;
            _store = new Dictionary<string, int>();
            _order = new Queue<string>();
            _inProgress = new HashSet<string>();
        }
        public int GetOrCompute(string key, Func<int> compute)
        {
            var stopwatch = Stopwatch.StartNew();

            lock (_cacheLock)
            {

                if (_store.ContainsKey(key))
                {
                    stopwatch.Stop();
                    Logger.Log($"KES HIT: '{key}' - vreme: {stopwatch.ElapsedMilliseconds}ms");
                    return _store[key];
                }

                while (_inProgress.Contains(key))
                {
                    Logger.Log($"KES: Nit ceka na rezultat za '{key}' (druga nit racuna)");
                    Monitor.Wait(_cacheLock);


                    if (_store.ContainsKey(key))
                    {
                        Logger.Log($"KES: Nit dobila rezultat za '{key}' nakon cekanja");
                        return _store[key];
                    }
                }
                _inProgress.Add(key);
                Logger.Log($"KES MISS: '{key}' - ova nit ce racunati rezultat");
            }
            int value;
            try
            {
                value = compute();
            }
            catch (Exception ex)
            {

                lock (_cacheLock)
                {
                    _inProgress.Remove(key);
                    Monitor.PulseAll(_cacheLock);
                }
                Logger.Log($"KES GRESKA pri racunanju za '{key}': {ex.Message}");
                throw;
            }
            lock (_cacheLock)
            {

                if (_store.Count >= _maxSize)
                {
                    string oldest = _order.Dequeue();
                    _store.Remove(oldest);
                    Logger.Log($"KES PUN: Obrisan najstariji unos '{oldest}'");
                }


                if (!_store.ContainsKey(key))
                {
                    _store[key] = value;
                    _order.Enqueue(key);
                }


                _inProgress.Remove(key);
                Monitor.PulseAll(_cacheLock);
                stopwatch.Stop();
                Logger.Log($"KES: Upisan rezultat za '{key}' = {value} - vreme: {stopwatch.ElapsedMilliseconds}ms");
            }

            return value;
        }
        public int Count
        {
            get { lock (_cacheLock) { return _store.Count; } }
        }
    }
}