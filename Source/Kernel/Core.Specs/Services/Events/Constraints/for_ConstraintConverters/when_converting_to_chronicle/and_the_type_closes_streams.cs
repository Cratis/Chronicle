// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Services.Events.Constraints.for_ConstraintConverters.when_converting_to_chronicle;

public class and_the_type_closes_streams : Specification
{
    IConstraintDefinition _result;

    void Because() => _result = new Contracts.Events.Constraints.Constraint
    {
        Name = "closing",
        Type = Contracts.Events.Constraints.ConstraintType.ClosesStream,
        ClosesStream = new()
        {
            EventTypeIds = ["Closed"],
            Dimensions = (uint)(ClosedStreamDimensions.EventSourceId | ClosedStreamDimensions.EventStreamId),
            ReopenedBy = ["Reopened"],
            EventStreamIdFrom = "period"
        },
        EventSequences = [EventSequenceId.Log.Value]
    }.ToChronicle();

    [Fact] void should_convert_the_complete_definition() => _result.ShouldEqual(new ClosesStreamConstraintDefinition("closing", ["Closed"], ClosedStreamDimensions.EventSourceId | ClosedStreamDimensions.EventStreamId, ["Reopened"], "period") { EventSequences = [EventSequenceId.Log] });
}
