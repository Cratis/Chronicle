// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ReplayScopedEventSequenceStorage.given;

public class a_bounded_parent_page : a_replay_scoped_storage
{
    protected readonly EventSourceId _missing = "missing";
    protected AppendedEvent _predecessor;
    protected AppendedEvent _changed;
    protected AppendedEvent _last;
    IProjectionPageCache _page;
    protected IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>> _history;

    void Establish()
    {
        _page = (IProjectionPageCache)_session;
        _predecessor = EventAt(2);
        _changed = EventAt(12);
        _last = EventAt(20);
        _history = new Dictionary<EventSourceId, IReadOnlyList<AppendedEvent>> { [_eventSourceId] = [_predecessor, _changed, _last] };
        _inner.GetInstancesOfAnyInRange(Arg.Any<IEnumerable<EventSourceId>>(), Arg.Any<IEnumerable<EventTypeId>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<CancellationToken>())
            .Returns(_history);
    }

    protected void BeginPreparedPage() => _page.BeginPreparedPage();

    protected void ClearPreparedPage() => _page.ClearPreparedPage();

    protected async Task Prepare()
    {
        _session.BeginReplaySession();
        _page.BeginPreparedPage();
        await _storage.GetInstancesOfAnyInRange([_eventSourceId, _missing], _eventTypeIds, 10, 20);
    }

    protected AppendedEvent EventAt(EventSequenceNumber number) => new(
        EventContext.Empty with { EventSourceId = _eventSourceId, EventType = _eventTypes[0], SequenceNumber = number },
        new ExpandoObject());
}
