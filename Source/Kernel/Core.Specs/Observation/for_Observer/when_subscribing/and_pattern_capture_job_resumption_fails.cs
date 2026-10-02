// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Schemas;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_pattern_capture_job_resumption_fails : given.an_observer
{
    PatternCapture _capture;
    Exception _failure;
    Exception? _firstError;
    bool _subscriptionWasAssigned;
    bool _subscribedAfterFailure;
    bool _subscribedAfterRetry;
    int _resumeAttempts;

    void Establish()
    {
        _failure = new Exception("Job resumption failed");
        _eventTypesStorage.GetLatestForAllEventTypes().Returns([
            new EventTypeSchema(EventType.Unknown, EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema())
        ]);
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_observer);
        var siloDetails = Substitute.For<ILocalSiloDetails>();
        siloDetails.SiloAddress.Returns(SiloAddress.Zero);
        _capture = new(_storage, siloDetails, grainFactory, NullLogger<PatternCapture>.Instance);
        _jobsManager.GetAllJobs().Returns(_ => GetJobsForSubscription());
    }

    async Task Because()
    {
        _firstError = await Catch.Exception(() => _capture.RecoverSubscription(_observerKey.EventStore, _observerKey.Namespace));
        _subscribedAfterFailure = (await _observer.GetSubscription()).IsSubscribed;

        // The event log's next timer attempt uses the same readiness check, without another append.
        await _capture.RecoverSubscription(_observerKey.EventStore, _observerKey.Namespace);
        _subscribedAfterRetry = (await _observer.GetSubscription()).IsSubscribed;
        await _capture.RecoverSubscription(_observerKey.EventStore, _observerKey.Namespace);
    }

    async Task<IImmutableList<JobState>> GetJobsForSubscription()
    {
        _resumeAttempts++;
        if (_resumeAttempts == 1)
        {
            _subscriptionWasAssigned = (await _observer.GetSubscription()).IsSubscribed;
            throw _failure;
        }
        return ImmutableList<JobState>.Empty;
    }

    [Fact] void should_fail_after_assigning_the_subscription() => _subscriptionWasAssigned.ShouldBeTrue();
    [Fact] void should_propagate_the_initial_failure() => _firstError.ShouldEqual(_failure);
    [Fact] void should_retain_the_subscription_for_recovery() => _subscribedAfterFailure.ShouldBeTrue();
    [Fact] void should_retry_without_another_append() => _resumeAttempts.ShouldEqual(2);
    [Fact] void should_finish_initializing_the_subscription() => _subscribedAfterRetry.ShouldBeTrue();
    [Fact] void should_resume_jobs_on_the_retry() => _resumeAttempts.ShouldEqual(2);
    [Fact] void should_not_initialize_a_completed_subscription_again() => _eventTypesStorage.Received(2).GetFor(Arg.Any<IEnumerable<EventType>>());
}
