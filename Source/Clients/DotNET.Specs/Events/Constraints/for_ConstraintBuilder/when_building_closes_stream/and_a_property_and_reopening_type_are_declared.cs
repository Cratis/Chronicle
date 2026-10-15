// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder.when_building_closes_stream;

public class and_a_property_and_reopening_type_are_declared : given.a_constraint_builder_with_owner
{
    ClosesStreamConstraintDefinition _result;

    void Establish()
    {
        var closing = new EventType(nameof(Closed), EventTypeGeneration.First);
        var reopening = new EventType(nameof(Reopened), EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(Closed)).Returns(closing);
        _eventTypes.GetEventTypeFor(typeof(Reopened)).Returns(reopening);
        _eventTypes.GetSchemaFor(closing.Id).Returns(_generator.Generate(typeof(Closed)));
        _eventTypes.GetSchemaFor(reopening.Id).Returns(_generator.Generate(typeof(Reopened)));
    }

    void Because() => _result = (ClosesStreamConstraintDefinition)_constraintBuilder
        .ClosesStreamOn<Closed>(scope => scope.PerEventSourceId().EventStreamIdFrom(@event => @event.Period).ReopenedBy<Reopened>(), "period-close")
        .ForEventLog().Build().Single();

    [Fact] void should_use_the_explicit_source_and_implied_stream_identifier_dimensions() => _result.Dimensions.ShouldEqual(ClosedStreamDimensions.EventSourceId | ClosedStreamDimensions.EventStreamId);
    [Fact] void should_keep_the_property() => _result.EventStreamIdFrom.ShouldEqual("Period");
    [Fact] void should_keep_the_reopening_type() => _result.ReopenedBy.Single().Value.ShouldEqual(nameof(Reopened));
    [Fact] void should_apply_the_event_sequence_restriction() => _result.EventSequences.ShouldContainOnly(EventSequenceId.Log);

    record Closed(string Period);
    record Reopened(string Period);
}
