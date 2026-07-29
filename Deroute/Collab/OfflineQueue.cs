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
        }

        public List<OfflineChange> Flush()
        {
            List<OfflineChange> result;
            lock (_lockObj)
            {
                result = _queue.ToList();
                while (_queue.TryDequeue(out _)) { }
            }
            return result;
        }

        public void Clear()
        {
            while (_queue.TryDequeue(out _)) { }
        }
    }

    public class OfflineChange
    {
        public string Type { get; set; }
        public string PrimitiveId { get; set; }
        public string SessionId { get; set; }
        public List<float> Points { get; set; }
        public string StrokeColor { get; set; }
        public float StrokeWidth { get; set; }
        public string FillColor { get; set; }
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
                    _pendingUpdates.Remove(existing);
                }
                _pendingUpdates.Add(new PositionUpdate { PrimitiveId = primitiveId, Points = points, Timestamp = DateTime.UtcNow });
            }
        }

        public void Stop()
        {
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
