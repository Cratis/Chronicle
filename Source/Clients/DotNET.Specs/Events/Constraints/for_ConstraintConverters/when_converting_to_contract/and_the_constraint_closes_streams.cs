// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintConverters.when_converting_to_contract;

public class and_the_constraint_closes_streams : Specification
{
    Contracts.Events.Constraints.Constraint _result;

    void Because() => _result = new ClosesStreamConstraintDefinition("closing", _ => string.Empty, ["Closed"], ClosedStreamDimensions.EventSourceId | ClosedStreamDimensions.EventStreamId, ["Reopened"], "period")
    {
        EventSequences = [EventSequenceId.Log]
    }.ToContract();

    [Fact] void should_use_the_additive_closing_type() => _result.Type.ShouldEqual(Contracts.Events.Constraints.ConstraintType.ClosesStream);
    [Fact] void should_keep_closing_event_types() => _result.ClosesStream!.EventTypeIds.ShouldContainOnly("Closed");
    [Fact] void should_keep_the_mask() => _result.ClosesStream!.Dimensions.ShouldEqual(9U);
    [Fact] void should_keep_reopening_event_types() => _result.ClosesStream!.ReopenedBy.ShouldContainOnly("Reopened");
    [Fact] void should_keep_the_property() => _result.ClosesStream!.EventStreamIdFrom.ShouldEqual("period");
    [Fact] void should_keep_sequence_restrictions() => _result.EventSequences.ShouldContainOnly(EventSequenceId.Log.Value);
}
