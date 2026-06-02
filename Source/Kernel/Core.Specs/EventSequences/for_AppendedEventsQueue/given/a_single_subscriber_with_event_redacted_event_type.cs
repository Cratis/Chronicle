// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.given;

public class a_single_subscriber_with_event_redacted_event_type : a_single_subscriber
{
    protected EventType _eventRedactedEventType = new(GlobalEventTypes.Redaction, 1);
    protected EventType _subscribedEventType = new("SomeEvent", 1);

    protected override IEnumerable<EventType> EventTypes => [_subscribedEventType, _eventRedactedEventType];
}
