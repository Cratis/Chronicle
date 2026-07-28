// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.given;

/// <summary>
/// Prepares a step against a chosen subscriber interface, which is how the step learns whether the observer it
/// feeds is a projection that collapses several event sources onto one read model document.
/// </summary>
public abstract class a_prepared_job_step_for_subscriber_type : Specification
{
    readonly TestKitSilo _silo = new();

    protected TestableHandleEventsForPartition _jobStep;

    /// <summary>
    /// Gets the subscriber interface the observer is subscribed through.
    /// </summary>
    protected abstract Type SubscriberType { get; }

    async Task Establish()
    {
        var observerKey = new ObserverKey("observer-id", "event-store", "event-store-namespace", EventSequenceId.Log);

        var observer = Substitute.For<IObserver>();
        observer.GetSubscription().Returns(new ObserverSubscription(
            "observer-id",
            observerKey,
            [],
            SubscriberType,
            SiloAddress.Zero));

        _silo.AddProbe(_ => observer);
        _silo.AddProbe(_ => Substitute.For<Projections.ICollapsingProjectionObserverSubscriber>());
        _silo.AddProbe(_ => Substitute.For<Projections.IProjectionObserverSubscriber>());
        _silo.AddService(Substitute.For<Storage.IStorage>());
        _silo.AddService(Substitute.For<IJobStepThrottle>());
        _silo.AddService(Substitute.For<IEventCompliance>());
        _silo.AddService<IObserverSubscriberSelector>(new RoundRobinObserverSubscriberSelector());

        var logger = _silo.AddService(NullLogger<HandleEventsForPartition>.Instance);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        _silo.AddService(loggerFactory);
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(logger);
        _silo.AddPersistentStateStorage<HandleEventsForPartitionState>(nameof(JobStepState), WellKnownGrainStorageProviders.JobSteps);

        _jobStep = await _silo.CreateGrainAsync<TestableHandleEventsForPartition>(
            JobStepId.New(),
            new JobStepKey(JobId.New(), "event-store", "event-store-namespace"));

        await _jobStep.Prepare(new HandleEventsForPartitionArguments(
            observerKey,
            ObserverType.Projection,
            "some-partition",
            EventSequenceNumber.First,
            EventSequenceNumber.Max,
            EventObservationState.Initial,
            []));
    }
}
