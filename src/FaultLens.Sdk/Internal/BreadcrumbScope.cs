using System;
using System.Collections.Generic;
using System.Linq;

namespace FaultLens.Sdk.Internal
{
    internal sealed class BreadcrumbScope
    {
        private readonly int _capacity;
        private readonly List<BreadcrumbEntry> _items;
        private int _nextSequence;
        private readonly object _sync = new object();

        public BreadcrumbScope(int capacity)
        {
            _capacity = capacity;
            _items = new List<BreadcrumbEntry>(capacity);
            _nextSequence = 0;
        }

        public void Add(BreadcrumbEntry entry)
        {
            lock (_sync)
            {
                if (_items.Count == _capacity)
                {
                    _items.RemoveAt(0);
                }

                _items.Add(entry);
            }
        }

        public int NextSequence()
        {
            lock (_sync)
            {
                return _nextSequence++;
            }
        }

        public IReadOnlyList<BreadcrumbEntry> SnapshotAndClear()
        {
            lock (_sync)
            {
                var copy = _items
                    .OrderBy(x => x.Sequence)
                    .ThenBy(x => x.Timestamp)
                    .ToList();

                _items.Clear();
                _nextSequence = 0;
                return copy;
            }
        }
    }

    internal sealed class BreadcrumbEntry
    {
        public DateTimeOffset Timestamp { get; set; }
        public int Sequence { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
        public IReadOnlyDictionary<string, object> Data { get; set; }
    }
}
