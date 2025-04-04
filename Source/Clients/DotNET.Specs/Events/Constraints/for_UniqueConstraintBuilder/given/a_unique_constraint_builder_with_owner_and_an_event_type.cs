// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.given;

public class a_unique_constraint_builder_with_owner_and_an_event_type : a_unique_constraint_builder_with_owner
{
    protected EventType _eventType;
    protected EventTypeId _eventTypeId;

    void Establish()
    {
        _eventTypeId = nameof(EventWithStringProperty);
        _eventType = new EventType(nameof(EventWithStringProperty), EventTypeGeneration.First);
        _eventTypes.GetSchemaFor(_eventTypeId).Returns(_generator.Generate(typeof(EventWithStringProperty)));
    }
}
