// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_copying_events;

public class and_event_types_are_given : given.an_event_sequence
{
    Guid _jobId;
    Guid _returned;
    Contracts.Sequences.CopyEventsForEventSourceRequest _request;

    void Establish()
    {
        _jobId = Guid.NewGuid();
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(new EventType("a-type", 1));
        _sequences.CopyEventsForEventSource(Arg.Do<Contracts.Sequences.CopyEventsForEventSourceRequest>(_ => _request = _), Arg.Any<CallContext>())
            .Returns(CommandResult<Guid>.Success(Guid.NewGuid(), _jobId));
    }

    async Task Because() => _returned = await _eventSequence.Copy("source", "target", typeof(string));

    [Fact] void should_return_the_job_id() => _returned.ShouldEqual(_jobId);
    [Fact] void should_send_the_source() => _request.SourceId.ShouldEqual("source");
    [Fact] void should_send_the_target() => _request.TargetId.ShouldEqual("target");
    [Fact] void should_send_the_event_types() => _request.EventTypes.ShouldContainOnly("a-type");
}
