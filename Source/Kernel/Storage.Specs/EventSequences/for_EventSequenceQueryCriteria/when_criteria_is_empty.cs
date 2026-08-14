// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.for_EventSequenceQueryCriteria.given;

namespace Cratis.Chronicle.Storage.EventSequences.for_EventSequenceQueryCriteria;

public class when_criteria_is_empty : Specification
{
    bool _result;

    void Because() => _result = EventSequenceQueryCriteria.Empty.Matches(
        an_event.With("some-source", "SomeEventType", ["a-tag"]));

    [Fact] void should_match_the_event() => _result.ShouldBeTrue();
    [Fact] void should_not_narrow_on_event_source() => EventSequenceQueryCriteria.Empty.HasEventSourceId.ShouldBeFalse();
    [Fact] void should_not_narrow_on_event_source_type() => EventSequenceQueryCriteria.Empty.HasEventSourceType.ShouldBeFalse();
    [Fact] void should_not_narrow_on_event_stream_type() => EventSequenceQueryCriteria.Empty.HasEventStreamType.ShouldBeFalse();
    [Fact] void should_not_narrow_on_correlation() => EventSequenceQueryCriteria.Empty.HasCorrelationId.ShouldBeFalse();
    [Fact] void should_not_narrow_on_event_types() => EventSequenceQueryCriteria.Empty.HasEventTypes.ShouldBeFalse();
    [Fact] void should_not_narrow_on_tags() => EventSequenceQueryCriteria.Empty.HasTags.ShouldBeFalse();
}
