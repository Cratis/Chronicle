// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using ProtoBuf;

namespace Cratis.Chronicle.Services.Events.Constraints.for_ConstraintConverters.when_converting_to_chronicle;

public class and_a_closing_declaration_crosses_the_wire : Specification
{
    IConstraintDefinition _result;

    void Because()
    {
        var contract = new Contracts.Events.Constraints.Constraint
        {
            Name = "closing",
            Type = Contracts.Events.Constraints.ConstraintType.ClosesStream,
            ClosesStream = new()
            {
                EventTypeIds = ["Closed"],
                Dimensions = 9,
                ReopenedBy = ["Reopened"],
                EventStreamIdFrom = "period"
            },
            EventSequences = [EventSequenceId.Log.Value]
        };
        _result = Serializer.DeepClone(contract).ToChronicle();
    }

    [Fact] void should_round_trip_the_additive_field_without_using_the_old_union() => _result.ShouldEqual(new ClosesStreamConstraintDefinition("closing", ["Closed"], ClosedStreamDimensions.EventSourceId | ClosedStreamDimensions.EventStreamId, ["Reopened"], "period") { EventSequences = [EventSequenceId.Log] });
}
