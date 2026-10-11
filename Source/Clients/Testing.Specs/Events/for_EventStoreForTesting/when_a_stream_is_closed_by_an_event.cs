// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.EventSequences;

namespace Cratis.Chronicle.Testing.Events.for_EventStoreForTesting;

public class when_a_stream_is_closed_by_an_event : Specification
{
    EventStoreForTesting _store;
    IAppendResult _result;

    async Task Establish()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(StreamClosed), typeof(EntryRecorded)]);
        artifacts.ClosesStreamEventTypes.Returns([typeof(StreamClosed)]);
        _store = new(null, artifacts);
        await _store.EventLog.Append("source", new StreamClosed());
    }

    async Task Because() => _result = await _store.EventLog.Append("source", new EntryRecorded());
    void Destroy() => _store.Dispose();

    [Fact] void should_enforce_the_closed_stream_constraint() => _result.ShouldHaveConstraintViolationFor("closed-stream");

    [EventType]
    [ClosesStream(Name = "test-stream-closed", Dimensions = ClosedStreamDimensions.EventSourceId)]
    record StreamClosed;

    [EventType]
    record EntryRecorded;
}
