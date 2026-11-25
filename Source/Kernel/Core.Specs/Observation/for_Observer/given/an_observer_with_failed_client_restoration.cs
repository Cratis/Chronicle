// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_with_failed_client_restoration : an_observer_that_lost_its_client
{
    protected readonly TimeoutException _failure = new("Job lookup failed");
    protected bool _failLookup = true;
    protected bool _installedAfterFailure;
    protected bool _establishedAfterFailure;
    protected Exception? _firstError;

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 42, LastHandledEventSequenceNumber = 41 };
        await Crash();
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(_ => _failLookup
            ? Task.FromException<IImmutableList<JobState>>(_failure)
            : Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        _firstError = await Catch.Exception(Restore);
        _installedAfterFailure = await _observer.IsSubscribed();
        _establishedAfterFailure = await _observer.IsSubscriptionEstablished();
        _failLookup = false;
        _jobsManager.ClearReceivedCalls();
    }

    protected Task Restore() => _observer.RestoreClientSubscription<NullObserverSubscriber>(ObserverType.Reactor, [event_type], SiloAddress.Zero, _client);
}
