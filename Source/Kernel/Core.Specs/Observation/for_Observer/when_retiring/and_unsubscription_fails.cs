// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_retiring;

public class and_unsubscription_fails : given.an_observer_with_subscription
{
    Exception _error;
    EventSequenceNumber _progress;

    async Task Establish()
    {
        await _observer.PartitionFailed("failed", 12UL, ["Failed"], "Stack");
        _progress = _stateStorage.State.NextEventSequenceNumber;
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromException<IImmutableList<JobState>>(new Exception("Jobs unavailable")));
        _subscriber.ClearReceivedCalls();
    }

    async Task Because()
    {
        _error = await Catch.Exception(_observer.Retire);
        await _observer.Handle("healthy", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new("event", 1), 20UL)]);
        _observerAlerts.ClearReceivedCalls();
        await ReportAlerts();
    }

    [Fact] void should_fail_retirement() => _error.ShouldNotBeNull();
    [Fact] async Task should_detach_the_subscription() => (await _observer.IsSubscribed()).ShouldBeFalse();
    [Fact] void should_not_deliver_events() => _subscriber.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_advance_progress() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(_progress);
    [Fact] void should_not_commit_retirement_before_job_cleanup() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Active);
    [Fact] async Task should_keep_existing_failures_reportable() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(snapshot => snapshot.Disposition == AlertDisposition.Active && snapshot.FailedPartitions.Count == 1));
}
