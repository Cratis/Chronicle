// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.EventSequences;
using static Cratis.Chronicle.Services.TypeScriptSequenceNumberCompatibility;

namespace Cratis.Chronicle.Services.EventSequences.for_EventSequences.when_getting_tail_sequence_number;

public class and_tail_sequence_number_is_unavailable : given.all_dependencies
{
    GetTailSequenceNumberResponse _result;

    void Establish() =>
        _eventSequenceStorage
            .GetTailSequenceNumber(
                Arg.Any<IEnumerable<EventType>>(),
                Arg.Any<EventSourceId>(),
                Arg.Any<EventSourceType>(),
                Arg.Any<EventStreamId>(),
                Arg.Any<EventStreamType>())
            .Returns(EventSequenceNumber.Unavailable);

    async Task Because() => _result = await _service.GetTailSequenceNumber(new()
    {
        EventStore = "event-store",
        Namespace = "event-store-namespace",
        EventSequenceId = "event-sequence"
    });

    [Fact] void should_sanitize_tail_sequence_number() => _result.SequenceNumber.ShouldEqual(MaxSafeInteger);
}
