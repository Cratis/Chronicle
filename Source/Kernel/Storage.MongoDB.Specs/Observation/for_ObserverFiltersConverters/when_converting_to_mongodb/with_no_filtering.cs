// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using KernelObserverFilters = Cratis.Chronicle.Concepts.Observation.ObserverFilters;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverFiltersConverters.when_converting_to_mongodb;

public class with_no_filtering : Specification
{
    ObserverFilters _result;

    void Because() => _result = KernelObserverFilters.None.ToMongoDB();

    [Fact] void should_have_empty_filter_tags() => _result.FilterTags.ShouldBeEmpty();
    [Fact] void should_have_empty_event_source_type() => _result.EventSourceType.ShouldEqual(string.Empty);
    [Fact] void should_have_all_event_stream_type() => _result.EventStreamType.ShouldEqual(EventStreamType.All.Value);
}
