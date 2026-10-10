// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_ClosesStreamConstraintDefinition;

public class when_comparing : Specification
{
    ClosesStreamConstraintDefinition _first;
    ClosesStreamConstraintDefinition _second;

    void Establish() => _first = new("closing", ["Closed"], ClosedStreamDimensions.EventSourceId, ["Reopened"], "period")
    {
        EventSequences = [EventSequenceId.Log]
    };

    void Because() => _second = new("closing", ["Closed"], ClosedStreamDimensions.EventSourceId, ["Reopened"], "period")
    {
        EventSequences = [EventSequenceId.Log]
    };

    [Fact] void should_compare_collections_by_content() => _first.Equals(_second).ShouldBeTrue();
    [Fact] void should_hash_equal_content_equally() => _first.GetHashCode().ShouldEqual(_second.GetHashCode());
    [Fact] void should_detect_a_changed_property() => _first.Equals(_second with { EventStreamIdFrom = "other" }).ShouldBeFalse();
    [Fact] void should_not_request_automatic_reindexing() => _second.CompareWith(_first).ShouldEqual(ConstraintChange.None);
}
