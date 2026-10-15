// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_explicitly_subscribed_during_quarantine_entry : an_observer_entering_quarantine
{
    protected bool _wasQuarantinedAfterSubscription;
    protected List<object?> _recoveryArguments = [];

    void Establish() => _jobsManager.Resume(Arg.Any<JobId>()).Returns(async callInfo =>
    {
        _resumedJobs.Add(callInfo.Arg<JobId>());
        _recoveryArguments.Add((await _observer.GetSubscription()).Arguments);
        return true;
    });

    protected async Task SubscribeDuringQuarantineEntry(bool allEvents, bool clearQuarantine)
    {
        try
        {
            if (clearQuarantine)
            {
                await _observer.ClearObserverQuarantine().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            }

            var subscribe = allEvents
                ? _observer.SubscribeToAllEvents<NullObserverSubscriber>(ObserverType.External, SiloAddress.Zero, "new-subscription")
                : _observer.Subscribe<NullObserverSubscriber>(ObserverType.External, [EventType.Unknown], SiloAddress.Zero, "new-subscription");
            await subscribe.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _wasQuarantinedAfterSubscription = await _observer.IsObserverQuarantined();
        }
        finally
        {
            _cleanupJobs.SetResult(_jobs);
        }
        await _quarantineEntry.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _silo.TimerRegistry.FireAllAsync();
    }
}
