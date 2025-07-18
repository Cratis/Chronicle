// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyScopeConverters.when_converting_to_contract;

public class and_all_optional_properties_are_null : Specification
{
    ConcurrencyScope _scope;
    Contracts.EventSequences.Concurrency.ConcurrencyScope _result;

    void Establish()
    {
        _scope = new ConcurrencyScope(
            new EventSequenceNumber(42),
            EventSourceId: null,
            EventStreamType: null,
            EventStreamId: null,
            EventSourceType: null,
            EventTypes: null);
    }

    void Because() => _result = _scope.ToContract();

    [Fact] void should_set_sequence_number() => _result.SequenceNumber.ShouldEqual(42ul);
    [Fact] void should_set_event_source_id_to_false() => _result.EventSourceId.ShouldBeFalse();
    [Fact] void should_set_event_stream_type_to_null() => _result.EventStreamType.ShouldBeNull();
    [Fact] void should_set_event_stream_id_to_null() => _result.EventStreamId.ShouldBeNull();
    [Fact] void should_set_event_source_type_to_null() => _result.EventSourceType.ShouldBeNull();
    [Fact] void should_set_event_types_to_null() => _result.EventTypes.ShouldBeNull();
}
