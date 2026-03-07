// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.given;

public class a_single_subscriber_with_an_event_type : a_single_subscriber
{
    protected EventType _eventType = new("Some event", 1);

    protected override IEnumerable<EventType> EventTypes => [_eventType];
}
