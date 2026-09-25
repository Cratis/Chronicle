// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Projections;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.for_ObserverRemover.given;

public class all_dependencies : Specification
{
    protected static readonly EventStoreName _eventStore = "some-event-store";
    protected static readonly EventStoreNamespaceName _firstNamespace = "first-namespace";
    protected static readonly EventStoreNamespaceName _secondNamespace = "second-namespace";
    protected static readonly ObserverId _observerId = "some-observer";

    protected IGrainFactory _grainFactory;
    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IObserverDefinitionsStorage _observerDefinitions;
    protected IProjectionDefinitionsStorage _projectionDefinitions;
    protected INamespaces _namespaces;
    protected IProjectionsManager _projectionsManager;

    protected IEventStoreNamespaceStorage _firstNamespaceStorage;
    protected IEventStoreNamespaceStorage _secondNamespaceStorage;
    protected IObserver _observerInFirstNamespace;
    protected IObserver _observerInSecondNamespace;
    protected IJobsManager _firstNamespaceJobs;
    protected IJobsManager _secondNamespaceJobs;

    protected ObserverRemover _remover;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _observerDefinitions = Substitute.For<IObserverDefinitionsStorage>();
        _projectionDefinitions = Substitute.For<IProjectionDefinitionsStorage>();
        _namespaces = Substitute.For<INamespaces>();
        _projectionsManager = Substitute.For<IProjectionsManager>();

        _storage.GetEventStore(_eventStore).Returns(_eventStoreStorage);
        _eventStoreStorage.Observers.Returns(_observerDefinitions);
        _eventStoreStorage.Projections.Returns(_projectionDefinitions);

        _firstNamespaceStorage = SetupNamespaceStorage(_firstNamespace);
        _secondNamespaceStorage = SetupNamespaceStorage(_secondNamespace);

        _observerInFirstNamespace = SetupObserver(_firstNamespace);
        _observerInSecondNamespace = SetupObserver(_secondNamespace);

        _firstNamespaceJobs = SetupJobsManager(_firstNamespace);
        _secondNamespaceJobs = SetupJobsManager(_secondNamespace);

        _grainFactory.GetGrain<INamespaces>(_eventStore).Returns(_namespaces);
        _grainFactory.GetGrain<IProjectionsManager>(_eventStore).Returns(_projectionsManager);
        _namespaces.GetAll().Returns(Task.FromResult<IEnumerable<EventStoreNamespaceName>>([_firstNamespace, _secondNamespace]));

        // The observer exists and nothing is holding on to it - the shape every spec that is not specifically
        // about the guard or about a missing observer starts from.
        _observerDefinitions.Has(_observerId).Returns(true);
        _projectionDefinitions.Has(Arg.Any<ProjectionId>()).Returns(false);

        _remover = new ObserverRemover(_grainFactory, _storage, NullLogger<ObserverRemover>.Instance);
    }

    protected void ObserverIs(IObserver observer, ObserverRunningState runningState, bool isSubscribed)
    {
        observer.IsSubscribed().Returns(isSubscribed);
        observer.GetState().Returns(ObserverState.Empty with
        {
            Identifier = _observerId,
            RunningState = runningState
        });
    }

    protected Task<ObserverRemovalResult> Remove() => _remover.Remove(_eventStore, _observerId, EventSequenceId.Log);

    IEventStoreNamespaceStorage SetupNamespaceStorage(EventStoreNamespaceName @namespace)
    {
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        var observerStates = Substitute.For<IObserverStateStorage>();
        var failedPartitions = Substitute.For<IFailedPartitionsStorage>();
        var handledCounts = Substitute.For<IObserverHandledCountsStorage>();
        var inFlightEvents = Substitute.For<IInFlightEventsStorage>();

        namespaceStorage.Observers.Returns(observerStates);
        namespaceStorage.FailedPartitions.Returns(failedPartitions);
        namespaceStorage.ObserverHandledCounts.Returns(handledCounts);
        namespaceStorage.InFlightEvents.Returns(inFlightEvents);

        observerStates.Get(_observerId).Returns(ObserverState.Empty with { Identifier = _observerId });
        inFlightEvents.GetFor(_observerId).Returns([]);

        _eventStoreStorage.GetNamespace(@namespace).Returns(namespaceStorage);
        return namespaceStorage;
    }

    IObserver SetupObserver(EventStoreNamespaceName @namespace)
    {
        var observer = Substitute.For<IObserver>();
        _grainFactory.GetGrain<IObserver>(new ObserverKey(_observerId, _eventStore, @namespace, EventSequenceId.Log)).Returns(observer);
        ObserverIs(observer, ObserverRunningState.Disconnected, false);
        return observer;
    }

    IJobsManager SetupJobsManager(EventStoreNamespaceName @namespace)
    {
        var jobsManager = Substitute.For<IJobsManager>();
        jobsManager.GetAllJobs().Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        _grainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(_eventStore.Value, @namespace.Value)).Returns(jobsManager);
        return jobsManager;
    }
}
