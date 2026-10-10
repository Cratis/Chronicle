// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder.when_building_closes_stream;

public class and_declarations_share_a_name : given.a_constraint_builder_with_owner
{
    ClosesStreamConstraintDefinition _result;

    void Establish()
    {
        _eventTypes.GetEventTypeFor(typeof(Closed)).Returns(new EventType(nameof(Closed), EventTypeGeneration.First));
        _eventTypes.GetEventTypeFor(typeof(Cancelled)).Returns(new EventType(nameof(Cancelled), EventTypeGeneration.First));
    }

    void Because() => _result = (ClosesStreamConstraintDefinition)_constraintBuilder.ClosesStreamOn<Closed>(name: "shared").ClosesStreamOn<Cancelled>(name: "shared").Build().Single();

    [Fact] void should_merge_both_closing_types() => _result.EventTypeIds.Select(type => type.Value).ShouldContainOnly(nameof(Closed), nameof(Cancelled));

    record Closed();
    record Cancelled();
}
