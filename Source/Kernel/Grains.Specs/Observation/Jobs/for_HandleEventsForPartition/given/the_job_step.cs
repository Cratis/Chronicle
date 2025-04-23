// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.Core;
using Orleans.TestKit;
using Orleans.TestKit.Storage;
namespace Cratis.Chronicle.Grains.Observation.Jobs.for_HandleEventsForPartition.given;

public class the_job_step : Specification
{
    protected HandleEventsForPartition _jobStep;
    protected IPersistentState<HandleEventsForPartitionState> _stateStorage;
    protected TestKitSilo _silo = new();
    protected JobStepId _jobStepId;
    protected JobStepKey _jobStepKey;
    protected Storage.IStorage _storage;
    protected IObserver _observer;
    protected ISomeObserverType _observerSubscriber;
    protected HandleEventsForPartitionArguments _request;

    async Task Establish()
    {
        var observerKey = new ObserverKey("observer-id", "event-store", "event-store-namespace", EventSequenceId.Log);
        _request = new(
            new("observer-id", "event-store", "event-store-namespace", EventSequenceId.Log),
            ObserverType.Projection,
            "some-partition",
            EventSequenceNumber.First,
            EventSequenceNumber.Max,
            EventObservationState.Initial,
            []);
        _observer = Substitute.For<IObserver>();
        _storage = Substitute.For<Storage.IStorage>();
        _silo.AddService(_storage);
        _observerSubscriber = Substitute.For<ISomeObserverType>();
        _silo.AddProbe(_ => _observer);
        _silo.AddProbe(_ => _observerSubscriber);

        _jobStepId = JobStepId.New();
        _jobStepKey = "7b4d6a8b-a4c3-4c83-83b6-666c39c271ee";
        var logger = _silo.AddService(NullLogger<Observer>.Instance);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        _silo.AddService(loggerFactory);
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(logger);
        _stateStorage = _silo.AddPersistentStateStorage<HandleEventsForPartitionState>(nameof(JobStepState), WellKnownGrainStorageProviders.JobSteps);
        _jobStep = await _silo.CreateGrainAsync<HandleEventsForPartition>(_jobStepId, _jobStepKey);
    }
}
