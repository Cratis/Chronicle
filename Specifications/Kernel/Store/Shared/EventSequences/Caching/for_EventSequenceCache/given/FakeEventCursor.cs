// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Events.Store.EventSequences.Caching.for_EventSequenceCache.given;

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
        if (_currentSequenceNumber < _end)
        {
            var actualEnd = _currentSequenceNumber + _cursorSize;
            if (actualEnd > _end)
            {
                actualEnd = _end;
            }

            var numberOfEvents = actualEnd - _currentSequenceNumber;
            Current = Enumerable.Range((int)_start.Value, (int)numberOfEvents.Value).Select(_ =>
                new AppendedEvent(
                    new(_currentSequenceNumber + (ulong)_, new(Guid.Empty, EventGeneration.First)),
                    new EventContext(
                        EventSourceId.Unspecified,
                        DateTimeOffset.Now,
                        DateTimeOffset.MinValue,
                        TenantId.Development,
                        CorrelationId.New(),
                        CausationId.System,
                        CausedBy.System), new JsonObject())).ToArray();

            _currentSequenceNumber += _cursorSize;
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
