// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources.for_EventSourceDefinition;

public class when_getting_the_concurrency : Specification
{
    EventSourceDefinition _definition;
    ConcurrencyDimensions _forStreamWithOwnDimensions;
    ConcurrencyDimensions _forStreamWithoutDimensions;
    ConcurrencyDimensions _forNoStream;

    void Establish() => _definition = new(
        typeof(object),
        "Cart",
        string.Empty,
        ConcurrencyDimensions.EventSourceId,
        [
            new EventStream("Items", string.Empty, ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId),
            new EventStream("Payment", string.Empty, ConcurrencyDimensions.None)
        ]);

    void Because()
    {
        _forStreamWithOwnDimensions = _definition.ConcurrencyFor(_definition.FindStream("Items"));
        _forStreamWithoutDimensions = _definition.ConcurrencyFor(_definition.FindStream("Payment"));
        _forNoStream = _definition.ConcurrencyFor(null);
    }

    [Fact] void should_use_the_dimensions_of_the_stream_when_it_declares_any() => _forStreamWithOwnDimensions.ShouldEqual(ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId);
    [Fact] void should_fall_back_to_the_event_source_for_a_stream_without_any() => _forStreamWithoutDimensions.ShouldEqual(ConcurrencyDimensions.EventSourceId);
    [Fact] void should_use_the_event_source_when_there_is_no_stream() => _forNoStream.ShouldEqual(ConcurrencyDimensions.EventSourceId);
}
