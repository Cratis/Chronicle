// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Keys;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Cratis.Traces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute.Extensions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class two_clients_with_different_definitions : an_observer
{
    protected static readonly EventSequenceId _secondSequence = "inbox-other-store";
    protected static readonly EventType _firstType = new("first-type", EventTypeGeneration.First);
    protected static readonly EventType _secondType = new("second-type", EventTypeGeneration.First);
    protected readonly List<ObserverDefinition> _definitions = [];
    protected readonly List<ObserverState> _states = [];
    protected readonly List<int> _catchupCounts = [];
    protected readonly List<CatchUpObserverRequest> _catchups = [];
    readonly List<JobId> _catchupIds = [];
    protected Observer _first;
    protected Observer _second;
    protected ObserverDefinition _persistedDefinition = ObserverDefinition.Empty;
    protected ObserverState _persistedState = new();
    protected ObserverSubscription _firstSubscription;
    protected ObserverSubscription _secondSubscription;
    IImmutableList<JobState> _unfinishedJobs = ImmutableList<JobState>.Empty;

    async Task Establish()
    {
        _persistedState = _stateStorage.State with
        {
            Identifier = _observerId,
            LastHandledEventSequenceNumber = 41UL,
            NextEventSequenceNumber = 42UL,
            FailedPartitionCount = 1
        };
        _failedPartitionsState.AddFailedPartition("failed-source");
        ConfigureSharedStorage();
        ConfigureJobs();
        ConfigureSequenceLookup();
        _first = await CreateObserver(_observerKey);
        _second = await CreateObserver(new(_observerId, _observerKey.EventStore, _observerKey.Namespace, _secondSequence));
        _jobsManager.ClearReceivedCalls();
    }

    protected async Task RegisterAlternatingClients(ObserverType type)
    {
        var firstClient = new ConnectedClient { ConnectionId = "client-a", Version = "1.0.0" };
        var secondClient = new ConnectedClient { ConnectionId = "client-b", Version = "2.0.0" };
        await _first.Subscribe<NullObserverSubscriber>(type, [_firstType], SiloAddress.Zero, firstClient);
        RecordRegistration();
        await _second.Subscribe<NullObserverSubscriber>(type, [_secondType], SiloAddress.Zero, secondClient);
        RecordRegistration();
        await _first.Subscribe<NullObserverSubscriber>(type, [_firstType], SiloAddress.Zero, firstClient);
        RecordRegistration();
        _firstSubscription = await _first.GetSubscription();
        _secondSubscription = await _second.GetSubscription();
    }

    protected Task CompleteOldSequence() => _second.CaughtUp(_catchupIds[1], 150UL, 150UL);

    void RecordRegistration()
    {
        _definitions.Add(_persistedDefinition);
        _states.Add(_persistedState);
        _catchupCounts.Add(_catchups.Count);
    }

    void ConfigureSharedStorage()
    {
        // Keep persisted records separate from each activation, using the production providers' identity mapping.
        _eventStoreStorage.Observers.Configure().Get(_observerId).Returns(_ => _persistedDefinition);
        _eventStoreStorage.Observers.When(_ => _.Save(Arg.Any<ObserverDefinition>()))
            .Do(call => _persistedDefinition = call.Arg<ObserverDefinition>());
        _eventStoreNamespaceStorage.Observers.Configure().Get(_observerId).Returns(_ => _persistedState);
        _eventStoreNamespaceStorage.Observers.When(_ => _.Save(Arg.Any<ObserverState>()))
            .Do(call => _persistedState = call.Arg<ObserverState>());
    }

    void ConfigureJobs()
    {
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(_ => Task.FromResult(_unfinishedJobs));
        _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>()).Returns(call =>
        {
            var request = call.Arg<CatchUpObserverRequest>();
            var jobId = JobId.New();
            _catchups.Add(request);
            _catchupIds.Add(jobId);
            _unfinishedJobs = _unfinishedJobs.Add(new JobState { Id = jobId, Status = JobStatus.PreparingJob, Request = request });
            return Task.FromResult(Result<JobId, StartJobError>.Success(jobId));
        });
    }

    void ConfigureSequenceLookup()
    {
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)200UL);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(call => call.Arg<EventSequenceNumber>());
        var index = Substitute.For<IObserverKeyIndex>();
        index.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(_ => new ObserverKeysForTesting([new Key("healthy-source", ArrayIndexers.NoIndexers)]));
        var indexes = Substitute.For<IObserverKeyIndexes>();
        indexes.GetFor(Arg.Any<ObserverKey>(), Arg.Any<ObserverReaderScope>()).Returns(index);
        _eventStoreNamespaceStorage.ObserverKeyIndexes.Returns(indexes);
    }

    async Task<Observer> CreateObserver(ObserverKey key)
    {
        var silo = new TestKitSilo();
        silo.Options.StorageFactory = type =>
        {
            if (type == typeof(ObserverDefinition))
            {
                return new shared_definition_storage<ObserverDefinition>(new ObserverDefinitionGrainStorageProvider(_storage), key);
            }
            if (type == typeof(ObserverState))
            {
                return new shared_definition_storage<ObserverState>(new ObserverStateGrainStorageProvider(_storage, NullLogger<ObserverStateGrainStorageProvider>.Instance), key);
            }
            if (type == typeof(FailedPartitions))
            {
                return new shared_definition_storage<FailedPartitions>(new FailedPartitionGrainStorageProvider(_storage), key);
            }
            return null!;
        };
        var options = Substitute.For<IOptionsMonitor<ChronicleOptions>>();
        options.CurrentValue.Returns(new ChronicleOptions { Webhooks = new() { Retry = CreateWebhookRetryConfig() } });
        silo.AddService(options);
        silo.AddService(_configurationProvider);
        silo.AddService(_storage);
        silo.AddService<TimeProvider>(TimeProvider.System);
        silo.AddService(_webhookTargetPolicy);
        silo.AddService(_eventCompliance);
        silo.AddService(_eventGenerationRelease);
        silo.AddService<IObserverSubscriberSelector>(new RoundRobinObserverSubscriberSelector());
        silo.AddService(_observerServiceClient);
        silo.AddKeyedService<IActivitySource<Observer>>(WellKnown.MeterName, new ActivitySource<Observer>());
        silo.AddService(_logger);
        silo.AddService(_loggerFactory);
        silo.AddProbe(_ => _observerAlerts);
        silo.AddProbe(_ => _subscriber);
        silo.AddProbe(_ => _jobsManager);
        silo.AddProbe(_ => _appendedEventsQueues);
        silo.AddProbe(_ => _eventSequence);
        return await silo.CreateGrainAsync<Observer>(key);
    }
}
