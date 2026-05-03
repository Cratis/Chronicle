// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using KernelObserverFilters = Cratis.Chronicle.Concepts.Observation.ObserverFilters;

namespace Cratis.Chronicle.Services.Observation.for_ObserverFiltersConverters.when_converting_to_chronicle;

public class with_no_filtering : Specification
{
    KernelObserverFilters _result;

    void Because() =>
        _result = new Contracts.Observation.ObserverFilters
        {
            FilterTags = [],
            EventSourceType = string.Empty,
            EventStreamType = EventStreamType.All.Value
        }.ToChronicle();

    [Fact] void should_have_empty_tags() => _result.Tags.ShouldBeEmpty();
    [Fact] void should_have_unspecified_event_source_type() => _result.EventSourceType.ShouldEqual(EventSourceType.Unspecified);
    [Fact] void should_have_all_event_stream_type() => _result.EventStreamType.ShouldEqual(EventStreamType.All);
}
