// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.EventSequences.for_AppendedEventsQueue.given;

public abstract class two_subscribed_subscribers : two_subscribers
{
    async Task Establish()
    {
        await _queue.Subscribe(_firstObserverKey, EventTypes);
        await _queue.Subscribe(_secondObserverKey, EventTypes);
    }

    protected abstract IEnumerable<EventType> EventTypes { get; }
}
