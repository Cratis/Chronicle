// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.for_EventSequenceQueryCriteria;

public class when_criteria_is_empty : Specification
{
    bool _result;

    void Because() => _result = EventSequenceQueryCriteria.Empty.Matches(
        "some-source",
        "SomeEventType",
        ["a-tag"],
        DateTimeOffset.UtcNow);

    [Fact] void should_match_the_event() => _result.ShouldBeTrue();
    [Fact] void should_not_narrow_on_event_source() => EventSequenceQueryCriteria.Empty.HasEventSourceId.ShouldBeFalse();
    [Fact] void should_not_narrow_on_event_types() => EventSequenceQueryCriteria.Empty.HasEventTypes.ShouldBeFalse();
    [Fact] void should_not_narrow_on_tags() => EventSequenceQueryCriteria.Empty.HasTags.ShouldBeFalse();
}
