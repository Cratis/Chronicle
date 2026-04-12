// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using KernelObserverFilters = Cratis.Chronicle.Concepts.Observation.ObserverFilters;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverFiltersConverters.when_converting_to_mongodb;

public class with_all_filters_set : Specification
{
    ObserverFilters _result;

    void Because() =>
        _result = new KernelObserverFilters(
            ["important", "priority"],
            new EventSourceType("order"),
            new EventStreamType("invoices")).ToMongoDB();

    [Fact] void should_map_filter_tags() => _result.FilterTags.ShouldContainOnly(["important", "priority"]);
    [Fact] void should_map_event_source_type() => _result.EventSourceType.ShouldEqual("order");
    [Fact] void should_map_event_stream_type() => _result.EventStreamType.ShouldEqual("invoices");
}
