// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ReplayScopedEventSequenceStorage;

public class when_forwarding_history_reads : given.a_replay_scoped_storage
{
    IEventCursor _expected;
    IEventCursor _range;
    IEventCursor _from;
    readonly CancellationTokenSource _cancellation = new();

    void Establish()
    {
        _expected = Substitute.For<IEventCursor>();
        _inner.GetRangeWithHistory(10, 20, _eventSourceId, _eventTypes, [(Tag)"tag"], _cancellation.Token).ReturnsForAnyArgs(_expected);
        _inner.GetFromSequenceNumberWithHistory(10, _eventSourceId, "source-type", "stream-type", "stream", _eventTypes, [(Tag)"tag"], _cancellation.Token).ReturnsForAnyArgs(_expected);
        _session.BeginReplaySession();
    }

    async Task Because()
    {
        _range = await _storage.GetRangeWithHistory(10, 20, _eventSourceId, _eventTypes, ["tag"], _cancellation.Token);
        _from = await _storage.GetFromSequenceNumberWithHistory(10, _eventSourceId, "source-type", "stream-type", "stream", _eventTypes, ["tag"], _cancellation.Token);
    }

    [Fact] void should_return_the_inner_range_cursor() => ReferenceEquals(_range, _expected).ShouldBeTrue();
    [Fact] void should_return_the_inner_from_cursor() => ReferenceEquals(_from, _expected).ShouldBeTrue();
    [Fact] void should_forward_every_range_argument() => _inner.Received(1).GetRangeWithHistory(10, 20, _eventSourceId, _eventTypes, Arg.Is<IEnumerable<Tag>>(tags => tags.Single().Value == "tag"), _cancellation.Token);
    [Fact] void should_forward_every_from_argument() => _inner.Received(1).GetFromSequenceNumberWithHistory(10, _eventSourceId, "source-type", "stream-type", "stream", _eventTypes, Arg.Is<IEnumerable<Tag>>(tags => tags.Single().Value == "tag"), _cancellation.Token);

    void Destroy() => _cancellation.Dispose();
}
