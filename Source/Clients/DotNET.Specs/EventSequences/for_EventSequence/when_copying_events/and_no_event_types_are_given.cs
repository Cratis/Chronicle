// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_copying_events;

public class and_no_event_types_are_given : given.an_event_sequence
{
    Contracts.Sequences.CopyEventsForEventSourceRequest _request;

    void Establish() =>
        _sequences.CopyEventsForEventSource(Arg.Do<Contracts.Sequences.CopyEventsForEventSourceRequest>(_ => _request = _), Arg.Any<CallContext>())
            .Returns(CommandResult<Guid>.Success(Guid.NewGuid(), Guid.NewGuid()));

    async Task Because() => await _eventSequence.Copy("source", "target");

    [Fact] void should_send_no_event_types() => _request.EventTypes.ShouldBeEmpty();
}
