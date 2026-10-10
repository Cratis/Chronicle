// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder.when_building_closes_stream;

public class and_no_dimensions_are_chosen : given.a_constraint_builder_with_owner
{
    ClosesStreamConstraintDefinition _result;

    void Establish() => _eventTypes.GetEventTypeFor(typeof(Closed)).Returns(new EventType(nameof(Closed), EventTypeGeneration.First));

    void Because() => _result = (ClosesStreamConstraintDefinition)_constraintBuilder.ClosesStreamOn<Closed>().Build().Single();

    [Fact] void should_use_source_and_stream_dimensions() => _result.Dimensions.ShouldEqual(ClosedStreamDimensions.EventSourceId | ClosedStreamDimensions.EventStreamType | ClosedStreamDimensions.EventStreamId);
    [Fact] void should_default_the_owner_to_the_event_type_identifier() => _result.Name.Value.ShouldEqual(nameof(Closed));

    record Closed();
}
