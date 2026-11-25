// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintEventTypes;

public class when_resolving_types : Specification
{
    [Fact]
    void should_include_claims_and_removals_but_not_redaction_for_unique_constraints() =>
        ConstraintEventTypes.For([new UniqueConstraintDefinition("unique", [new("Claimed", ["value"])], ["Removed"])])
            .ShouldEqual([new EventType("Claimed", EventTypeGeneration.First), new EventType("Removed", EventTypeGeneration.First)]);

    [Fact]
    void should_include_closing_reopening_and_redaction() =>
        ConstraintEventTypes.For([new ClosesStreamConstraintDefinition("closing", ["Closed"], ClosedStreamDimensions.EventSourceId, ["Reopened"])])
            .ShouldEqual([new EventType("Closed", EventTypeGeneration.First), new EventType("Reopened", EventTypeGeneration.First), new EventType(GlobalEventTypes.Redaction, EventTypeGeneration.First)]);

    [Fact]
    void should_union_and_deduplicate_definitions() =>
        ConstraintEventTypes.For([new UniqueConstraintDefinition("first", [new("Claimed", ["value"])], ["Removed"]), new UniqueConstraintDefinition("second", [new("Claimed", ["other"])])])
            .ShouldEqual([new EventType("Claimed", EventTypeGeneration.First), new EventType("Removed", EventTypeGeneration.First)]);

    [Fact] void should_return_no_types_for_no_definitions() => ConstraintEventTypes.For([]).ShouldBeEmpty();
}
