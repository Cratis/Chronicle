// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_activating;

/// <summary>
/// A persisted, prepared step whose observer has since been unsubscribed must still activate. Resolving the
/// placeholder subscriber type of an unsubscribed observer threw, so the step failed activation on every retry.
/// </summary>
public class and_the_observer_is_unsubscribed : Specification
{
    readonly TestKitSilo _silo = new();
    Exception _error;
    IObserver _observer;

    async Task Establish()
    {
        var observerKey = new ObserverKey("observer-id", "event-store", "event-store-namespace", EventSequenceId.Log);

        var observer = _observer = Substitute.For<IObserver>();
        observer.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        _silo.AddProbe(_ => observer);
        _silo.AddService(Substitute.For<Storage.IStorage>());
        _silo.AddService(Substitute.For<IJobStepThrottle>());
        _silo.AddService(Substitute.For<IEventCompliance>());
        _silo.AddService<IObserverSubscriberSelector>(new RoundRobinObserverSubscriberSelector());

        var logger = _silo.AddService(NullLogger<HandleEventsForPartition>.Instance);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        _silo.AddService(loggerFactory);
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(logger);
        _silo.AddPersistentStateStorage<HandleEventsForPartitionState>(nameof(JobStepState), Cratis.Orleans.WellKnownGrainStorageProviders.JobSteps);

        var storage = _silo.StorageManager.GetStorage<HandleEventsForPartitionState>(nameof(JobStepState));
        storage.State.ObserverKey = observerKey;
        storage.State.Partition = "some-partition";
        storage.State.IsPrepared = true;
        await Task.CompletedTask;
    }

    async Task Because() => _error = await Catch.Exception(async () => await _silo.CreateGrainAsync<TestableHandleEventsForPartition>(
        JobStepId.New(),
        new JobStepKey(JobId.New(), "event-store", "event-store-namespace")));

    [Fact] void should_have_read_the_subscription() => _observer.Received().GetSubscription();
    [Fact] void should_activate() => _error.ShouldBeNull();
}
