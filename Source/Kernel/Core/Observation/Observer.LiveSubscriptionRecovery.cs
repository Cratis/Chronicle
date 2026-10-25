// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    bool _rejoiningQueueAfterZeroWork;

    bool IsRejoiningQueueDuringBackoff => _rejoiningQueueAfterZeroWork && IsBackingOffCatchUp;

    async Task SettleCatchUpInObserving()
    {
        if (State.RunningState == ObserverRunningState.Replaying) return;
        await TransitionTo<Observing>();
        if (await _appendedEventsQueues.IsSubscribed(_observerKey)) return;

        // Observing ignores a transition to itself; routing re-enters it and restores the queue subscription.
        _rejoiningQueueAfterZeroWork = true;
        try
        {
            await TransitionTo<Routing>();
        }
        finally
        {
            _rejoiningQueueAfterZeroWork = false;
        }
    }
}
