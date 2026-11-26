// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_fluently;

public class and_using_default_metadata : given.an_observed_reactor_stream
{
    async Task Because() => await _reactors.Register("orders", reactor => reactor.On<OrderPlaced>(_ => { }));

    [Fact] void should_allow_replay() => _definitions.Single().IsReplayable.ShouldBeTrue();
    [Fact] void should_not_disable_replay() => _definitions.Single().IsNotReplayable.ShouldBeFalse();
    [Fact] void should_have_no_tags() => _definitions.Single().Tags.ShouldBeEmpty();
    [Fact] void should_have_no_filter_tags() => _definitions.Single().Filters.FilterTags.ShouldBeEmpty();
    [Fact] void should_not_filter_event_source_types() => _definitions.Single().Filters.EventSourceType.ShouldEqual(EventSourceType.Unspecified.Value);
    [Fact] void should_include_all_event_stream_types() => _definitions.Single().Filters.EventStreamType.ShouldEqual(EventStreamType.All.Value);
    [Fact] void should_start_from_the_beginning() => _definitions.Single().StartAtTail.ShouldBeFalse();
}
