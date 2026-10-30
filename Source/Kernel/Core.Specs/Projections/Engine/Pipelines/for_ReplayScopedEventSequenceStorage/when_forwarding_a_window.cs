// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ReplayScopedEventSequenceStorage;

public class when_forwarding_a_window : given.a_replay_scoped_storage
{
    IEventCursor _expected;
    IEventCursor _result;
    readonly CancellationTokenSource _cancellation = new();

    void Establish()
    {
        _expected = Substitute.For<IEventCursor>();
        _inner.GetWindow(10, 20, 3, true, _eventSourceId, "source-type", "stream-type", "stream", _eventTypes, [(Tag)"tag"], _cancellation.Token)
            .ReturnsForAnyArgs(_expected);
        _session.BeginReplaySession();
    }

    async Task Because() => _result = await _storage.GetWindow(10, 20, 3, true, _eventSourceId, "source-type", "stream-type", "stream", _eventTypes, ["tag"], _cancellation.Token);

    [Fact] void should_return_the_inner_cursor() => ReferenceEquals(_result, _expected).ShouldBeTrue();
    [Fact] void should_forward_every_window_argument() => _inner.Received(1).GetWindow(10, 20, 3, true, _eventSourceId, "source-type", "stream-type", "stream", _eventTypes, Arg.Is<IEnumerable<Tag>>(tags => tags.Single().Value == "tag"), _cancellation.Token);

    void Destroy() => _cancellation.Dispose();
}
