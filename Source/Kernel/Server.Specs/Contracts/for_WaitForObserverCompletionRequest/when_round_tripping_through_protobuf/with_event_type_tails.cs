// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.Observation;
using ProtoBuf;

namespace Cratis.Chronicle.Server.Contracts.for_WaitForObserverCompletionRequest.when_round_tripping_through_protobuf;

public class with_event_type_tails : Specification
{
    WaitForObserverCompletionRequest _request;
    WaitForObserverCompletionRequest _result;

    void Establish() => _request = new()
    {
        EventStore = "store",
        Namespace = "namespace",
        EventSequenceId = "sequence",
        TailEventSequenceNumber = 13,
        EventTypeTails = new List<AppendedEventTypeTail>
        {
            new() { EventType = new EventType { Id = "first", Generation = 1 }, SequenceNumber = 11 },
            new() { EventType = new EventType { Id = "second", Generation = 2 }, SequenceNumber = 13 },
        },
    };

    void Because() => _result = Serializer.DeepClone(_request);

    [Fact] void should_preserve_both_event_type_tails() => _result.EventTypeTails.Select(tail => (tail.EventType.Id, tail.EventType.Generation, tail.SequenceNumber)).ShouldContainOnly(("first", 1u, 11ul), ("second", 2u, 13ul));
}
