// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.for_ObserverFilters;

public class when_checking_none_sentinel : Specification
{
    [Fact] void should_have_empty_tags() => ObserverFilters.None.Tags.ShouldBeEmpty();
    [Fact] void should_have_null_event_source_type() => ObserverFilters.None.EventSourceType.ShouldBeNull();
    [Fact] void should_have_null_event_stream_type() => ObserverFilters.None.EventStreamType.ShouldBeNull();
}
