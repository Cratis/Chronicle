// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder.when_building_closes_stream;

public class and_all_dimensions_are_chosen : given.a_constraint_builder_with_owner
{
    ClosesStreamConstraintDefinition _result;

    void Establish() => _eventTypes.GetEventTypeFor(typeof(Closed)).Returns(new EventType(nameof(Closed), EventTypeGeneration.First));

    void Because() => _result = (ClosesStreamConstraintDefinition)_constraintBuilder.ClosesStreamOn<Closed>(scope => scope.PerEventSourceId().PerEventSourceType().PerEventStreamType().PerEventStreamId()).Build().Single();

    [Fact] void should_include_every_requested_dimension() => _result.Dimensions.ShouldEqual(ClosedStreamDimensions.EventSourceId | ClosedStreamDimensions.EventSourceType | ClosedStreamDimensions.EventStreamType | ClosedStreamDimensions.EventStreamId);

    record Closed();
}
