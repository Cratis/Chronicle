// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_with_failing_setup_jobs : an_observer
{
    protected readonly TimeoutException _failure = new("Job lookup failed");
    protected bool _failLookup = true;
    protected bool _installed;
    protected bool _established;
    protected bool _needsRecovery;
    protected Exception? _error;

    void Establish() => _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(_ => _failLookup
        ? Task.FromException<IImmutableList<JobState>>(_failure)
        : Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));

    protected async Task FailSetup(Func<Task> subscribe)
    {
        _error = await Catch.Exception(subscribe);
        _installed = (await _observer.GetSubscription()).IsSubscribed;
        _established = await _observer.IsSubscriptionEstablished();
        _needsRecovery = await _observer.NeedsSubscriptionRecovery([EventType.Unknown]);
        _failLookup = false;
    }
}
