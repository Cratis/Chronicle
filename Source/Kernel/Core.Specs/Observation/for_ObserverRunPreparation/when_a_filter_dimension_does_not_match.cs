// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ObserverRunPreparation;

public class when_a_filter_dimension_does_not_match : given.a_partition_run
{
    PreparedObserverRun _source;
    PreparedObserverRun _stream;
    PreparedObserverRun _tags;

    void Because()
    {
        _source = ObserverRunPreparation.Prepare(_events, _context with { Filters = new([], "missing-source") });
        _stream = ObserverRunPreparation.Prepare(_events, _context with { Filters = new([], null, "missing-stream") });
        _tags = ObserverRunPreparation.Prepare(_events, _context with { Filters = new(["missing-tag"]) });
    }

    [Fact] void should_skip_when_source_type_is_missing() => _source.IsApplicable.ShouldBeFalse();
    [Fact] void should_skip_when_stream_type_is_missing() => _stream.IsApplicable.ShouldBeFalse();
    [Fact] void should_skip_when_tags_are_missing() => _tags.IsApplicable.ShouldBeFalse();
    [Fact] void should_preserve_the_tail_when_filters_exclude_the_run() => _tags.ObservedTail.ShouldEqual((EventSequenceNumber)12UL);
    [Fact] void should_retain_no_excluded_events() => _tags.EventsToHandle.ShouldBeEmpty();
}
