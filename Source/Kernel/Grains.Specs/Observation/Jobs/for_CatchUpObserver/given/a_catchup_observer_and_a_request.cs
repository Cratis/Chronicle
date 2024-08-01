// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Grains.Observation.Jobs.for_CatchUpObserver.given;

public class a_catchup_observer_and_a_request : a_catchup_observer
{
    void Establish()
    {
        state_storage.State.Request = new CatchUpObserverRequest(
            new ObserverKey(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
            ObserverSubscription.Unsubscribed,
            42UL,
            [
                new EventType(Guid.NewGuid().ToString(), EventGeneration.First),
                new EventType(Guid.NewGuid().ToString(), EventGeneration.First)
            ]);
    }
}
