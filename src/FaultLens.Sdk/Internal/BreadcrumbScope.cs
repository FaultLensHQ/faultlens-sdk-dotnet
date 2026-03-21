using System;
using System.Collections.Generic;
using System.Linq;

namespace FaultLens.Sdk.Internal
{
    internal sealed class BreadcrumbScope
    {
        private readonly int _capacity;
        private readonly BreadcrumbEntry[] _buffer;
        private int _nextSequence;
        private int _count;
        private int _head;
        private readonly object _sync = new object();

        public BreadcrumbScope(int capacity)
        {
            _capacity = capacity;
            _buffer = new BreadcrumbEntry[capacity];
            _nextSequence = 0;
        }

        public void Add(BreadcrumbEntry entry)
        {
            lock (_sync)
            {
                var index = (_head + _count) % _capacity;
                _buffer[index] = entry;

                if (_count == _capacity)
                {
                    _head = (_head + 1) % _capacity;
                    return;
                }

                _count++;
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
                var copy = new List<BreadcrumbEntry>(_count);
                for (var i = 0; i < _count; i++)
                {
                    var index = (_head + i) % _capacity;
                    copy.Add(_buffer[index]);
                    _buffer[index] = null;
                }

                _count = 0;
                _head = 0;
                _nextSequence = 0;
                return copy
                    .OrderBy(x => x.Sequence)
                    .ThenBy(x => x.Timestamp)
                    .ToList();
            }
        }
    }

    internal sealed class BreadcrumbEntry
    {
        public DateTimeOffset Timestamp { get; set; }
        public int Sequence { get; set; }
        public string Layer { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public IReadOnlyDictionary<string, object> Data { get; set; }
    }
}
