// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.given;

public class a_single_subscriber_with_filter_tags : a_single_subscriber
{
    protected EventType _eventType = new("Some event", 1);
    protected string _requiredTag = "important";

    protected override IEnumerable<EventType> EventTypes => [_eventType];

    protected override ObserverFilters? Filters =>
        new([_requiredTag]);
}
