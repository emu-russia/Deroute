using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DerouteSharp.Collab
{
    public class OfflineChangeQueue
    {
        private readonly ConcurrentQueue<OfflineChange> _queue = new ConcurrentQueue<OfflineChange>();
        private readonly object _lockObj = new object();
        public int Count => _queue.Count;

        public void Add(OfflineChange change)
        {
            _queue.Enqueue(change);
#if DEBUG && (!__MonoCS__)
            Console.WriteLine($"[OfflineQueue] Add: type={change.ChangeType}, primitiveId={change.PrimitiveId}, count={_queue.Count}");
#endif
        }

        public List<OfflineChange> Flush()
        {
            List<OfflineChange> result;
            lock (_lockObj)
            {
                result = _queue.ToList();
                while (_queue.TryDequeue(out _)) { }
            }
#if DEBUG && (!__MonoCS__)
            Console.WriteLine($"[OfflineQueue] Flush: {result.Count} changes, queue empty={result.Count == 0}");
#endif
            return result;
        }

        public void Clear()
        {
#if DEBUG && (!__MonoCS__)
            Console.WriteLine($"[OfflineQueue] Clear: {Count} changes removed");
#endif
            while (_queue.TryDequeue(out _)) { }
        }
    }

    public class OfflineChange
    {
        public string ChangeType { get; set; }
        public string PrimitiveId { get; set; }
        public string SessionId { get; set; }
        public string EntityType { get; set; }
        public string EntityLabel { get; set; }
        public List<float> Points { get; set; }
        public string StrokeColor { get; set; }
        public float StrokeWidth { get; set; }
        public string FillColor { get; set; }
        public float LambdaX { get; set; }
        public float LambdaY { get; set; }
        public float LambdaEndX { get; set; }
        public float LambdaEndY { get; set; }
        public List<float> PathPoints { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class CoordinateThrottler
    {
        private readonly System.Windows.Forms.Timer _timer;
        private readonly List<PositionUpdate> _pendingUpdates = new List<PositionUpdate>();
        private readonly object _lockObj = new object();
        private int _intervalMs;

        public event Action<List<PositionUpdate>> OnFlush;

        public CoordinateThrottler(Control parentControl, int intervalMs = 33)
        {
            _intervalMs = intervalMs;
            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = _intervalMs;
            _timer.Tick += Timer_Tick;
            _timer.Start();
#if DEBUG && (!__MonoCS__)
            Console.WriteLine($"[CoordinateThrottler] Created: interval={_intervalMs}ms");
#endif
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            List<PositionUpdate> updates;
            lock (_lockObj)
            {
                updates = _pendingUpdates.ToList();
                _pendingUpdates.Clear();
            }

            if (updates.Count > 0)
            {
#if DEBUG && (!__MonoCS__)
                Console.WriteLine($"[CoordinateThrottler] Flush: {updates.Count} updates");
#endif
                OnFlush?.Invoke(updates);
            }
        }

        public void AddUpdate(string primitiveId, List<float> points)
        {
            lock (_lockObj)
            {
                var existing = _pendingUpdates.FirstOrDefault(u => u.PrimitiveId == primitiveId);
                if (existing != null)
                {
#if DEBUG && (!__MonoCS__)
                    Console.WriteLine($"[CoordinateThrottler] Dedup: primitiveId={primitiveId}, replacing update");
#endif
                    _pendingUpdates.Remove(existing);
                }
                _pendingUpdates.Add(new PositionUpdate { PrimitiveId = primitiveId, Points = points, Timestamp = DateTime.UtcNow });
#if DEBUG && (!__MonoCS__)
                Console.WriteLine($"[CoordinateThrottler] AddUpdate: primitiveId={primitiveId}, pendingCount={_pendingUpdates.Count}");
#endif
            }
        }

        public void Stop()
        {
#if DEBUG && (!__MonoCS__)
            Console.WriteLine("[CoordinateThrottler] Stop");
#endif
            _timer?.Stop();
            _timer?.Dispose();
        }

        public class PositionUpdate
        {
            public string PrimitiveId { get; set; }
            public List<float> Points { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
