// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching.for_EventSequenceCache;

public class FakeEventCursor : IEventCursor
{
    readonly EventSequenceNumber _start;
    readonly EventSequenceNumber _end;
    readonly int _cursorSize;
    EventSequenceNumber _currentSequenceNumber;

    public IEnumerable<AppendedEvent> Current { get; private set; }

    public FakeEventCursor(EventSequenceNumber start, EventSequenceNumber end, int cursorSize)
    {
        _start = start;
        _end = end;
        _cursorSize = cursorSize;
        _currentSequenceNumber = start;
    }

    public Task<bool> MoveNext()
    {
        if (_start == _end)
        {
            return Task.FromResult(false);
        }

        if (_currentSequenceNumber <= _end)
        {
            var actualEnd = _currentSequenceNumber + _cursorSize;
            var numberOfEvents = actualEnd - _currentSequenceNumber;
            if (actualEnd > _end)
            {
                numberOfEvents = _end - _currentSequenceNumber + 1;
            }

            Current = Enumerable.Range(0, (int)numberOfEvents.Value).GenerateEvents(_currentSequenceNumber);
            _currentSequenceNumber += Current.Count();
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
