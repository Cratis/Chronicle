// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Alerts;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_retiring;

public class and_unsubscription_fails : given.an_observer_with_subscription
{
    Exception _error;

    void Establish() => _jobsManager.GetAllJobs().Returns(Task.FromException<IImmutableList<JobState>>(new Exception("Jobs unavailable")));

    async Task Because()
    {
        _error = await Catch.Exception(_observer.Retire);
        _observerAlerts.ClearReceivedCalls();
        await _observer.PartitionFailed("partition", 12UL, ["Failed"], "Stack");
    }

    [Fact] void should_fail_retirement() => _error.ShouldNotBeNull();
    [Fact] async Task should_keep_reporting_failures() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(snapshot => snapshot.FailedPartitions.Count == 1));
}
