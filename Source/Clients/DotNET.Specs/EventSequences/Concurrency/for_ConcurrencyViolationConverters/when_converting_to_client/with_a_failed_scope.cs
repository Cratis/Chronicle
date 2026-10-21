// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyViolationConverters.when_converting_to_client;

public class with_a_failed_scope : Specification
{
    ConcurrencyViolation _result;

    void Because() => _result = new Contracts.EventSequences.Concurrency.ConcurrencyViolation
    {
        EventSourceId = "report",
        ScopeIndex = 1,
        Scope = new()
        {
            EventSourceId = true,
            EventStreamType = "Corrections",
            EventStreamId = "open",
            ExpectsNoMatchingEvent = true,
            SequenceNumber = EventSequenceNumber.Unavailable.Value
        }
    }.ToClient();

    [Fact] void should_map_the_request_position() => _result.ScopeIndex.ShouldEqual(1u);
    [Fact] void should_restore_the_source_narrowing() => _result.Scope!.EventSourceId.ShouldEqual((EventSourceId)"report");
    [Fact] void should_restore_the_stream() => _result.Scope!.EventStreamType.ShouldEqual((EventStreamType)"Corrections");
    [Fact] void should_restore_the_negative_expectation() => _result.Scope!.SequenceNumber.ShouldEqual(EventSequenceNumber.BeforeFirst);
}
