// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Storage;
using Aksio.Cratis.Kernel.Storage.Changes;
using Aksio.Cratis.Kernel.Storage.EventSequences;
using Aksio.Cratis.Kernel.Storage.EventTypes;
using Aksio.Cratis.Kernel.Storage.Jobs;
using Aksio.Cratis.Kernel.Storage.MongoDB;
using Aksio.Cratis.Kernel.Storage.MongoDB.EventSequences;
using Aksio.Cratis.Kernel.Storage.MongoDB.Jobs;
using Aksio.Cratis.Kernel.Storage.MongoDB.Observation;
using Aksio.Cratis.Kernel.Storage.MongoDB.Projections;
using Aksio.Cratis.Kernel.Storage.MongoDB.Recommendations;
using Aksio.Cratis.Kernel.Storage.Observation;
using Aksio.Cratis.Kernel.Storage.Recommendations;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreInstanceStorage"/> for MongoDB.
/// </summary>
public class EventStoreInstanceStorage : IEventStoreInstanceStorage
{
    readonly EventStore _eventStore;
    readonly TenantId _tenantId;
    readonly IEventStoreInstanceDatabase _eventStoreInstanceDatabase;
    readonly IEventConverter _converter;
    readonly IEventTypesStorage _eventTypesStorage;
    readonly Json.ExpandoObjectConverter _expandoObjectConverter;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IExecutionContextManager _executionContextManager;
    readonly ILoggerFactory _loggerFactory;
    readonly ConcurrentDictionary<EventSequenceId, IEventSequenceStorage> _eventSequences = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreInstanceStorage"/> class.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStore"/> the storage is for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the storage is for.</param>
    /// <param name="eventStoreInstanceDatabase">Provider for <see cref="IEventStoreInstanceDatabase"/> to use.</param>
    /// <param name="converter"><see cref="IEventConverter"/> to convert event types.</param>
    /// <param name="eventTypesStorage">The <see cref="IEventTypesStorage"/> for working with the schema types.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between expando object and json objects.</param>
    /// <param name="jsonSerializerOptions">The global <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for getting the execution context.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
    public EventStoreInstanceStorage(
        EventStore eventStore,
        TenantId tenantId,
        IEventStoreInstanceDatabase eventStoreInstanceDatabase,
        IEventConverter converter,
        IEventTypesStorage eventTypesStorage,
        Json.ExpandoObjectConverter expandoObjectConverter,
        JsonSerializerOptions jsonSerializerOptions,
        IExecutionContextManager executionContextManager,
        ILoggerFactory loggerFactory)
    {
        _eventStore = eventStore;
        _tenantId = tenantId;
        _eventStoreInstanceDatabase = eventStoreInstanceDatabase;
        _converter = converter;
        _eventTypesStorage = eventTypesStorage;
        _expandoObjectConverter = expandoObjectConverter;
        _jsonSerializerOptions = jsonSerializerOptions;
        _executionContextManager = executionContextManager;
        _loggerFactory = loggerFactory;
        Changesets = new ChangesetStorage();
        Jobs = new JobStorage(eventStoreInstanceDatabase);
        JobSteps = new JobStepStorage(eventStoreInstanceDatabase);
        Observers = new ObserverStorage(eventStoreInstanceDatabase);
        FailedPartitions = new FailedPartitionStorage(eventStoreInstanceDatabase);
        Recommendations = new RecommendationStorage(eventStoreInstanceDatabase);
    }

    /// <inheritdoc/>
    public IChangesetStorage Changesets { get; }

    /// <inheritdoc/>
    public IJobStorage Jobs { get; }

    /// <inheritdoc/>
    public IJobStepStorage JobSteps { get; }

    /// <inheritdoc/>
    public IObserverStorage Observers { get; }

    /// <inheritdoc/>
    public IFailedPartitionsStorage FailedPartitions { get; }

    /// <inheritdoc/>
    public IRecommendationStorage Recommendations { get; }

    /// <inheritdoc/>
    public IJobStorage<TJobState> GetJobStorage<TJobState>()
        where TJobState : JobState => JobStorageCache<TJobState>.GetInstance(_eventStoreInstanceDatabase);

    /// <inheritdoc/>
    public IJobStepStorage<TJobStepState> GetJobStepStorage<TJobStepState>()
        where TJobStepState : JobStepState => JobStepStorageCache<TJobStepState>.GetInstance(_eventStoreInstanceDatabase);

    /// <inheritdoc/>
    public IEventSequenceStorage GetEventSequence(EventSequenceId eventSequenceId)
    {
        if (_eventSequences.TryGetValue(eventSequenceId, out var eventSequenceStorage))
        {
            return eventSequenceStorage;
        }

        eventSequenceStorage = new EventSequenceStorage(
            _eventStore,
            _tenantId,
            eventSequenceId,
            _eventStoreInstanceDatabase,
            _converter,
            _eventTypesStorage,
            _expandoObjectConverter,
            _jsonSerializerOptions,
            _executionContextManager,
            _loggerFactory.CreateLogger<EventSequenceStorage>());
        _eventSequences[eventSequenceId] = eventSequenceStorage;

        return eventSequenceStorage;
    }

    static class JobStorageCache<TJobState>
        where TJobState : JobState
    {
        static readonly object _lock = new();
        static IJobStorage<TJobState>? _instance;

        public static IJobStorage<TJobState> GetInstance(IEventStoreInstanceDatabase database)
        {
            if (_instance is not null)
            {
                return _instance;
            }

            lock (_lock)
            {
                _instance = new JobStorage<TJobState>(database);
                return _instance;
            }
        }
    }

    static class JobStepStorageCache<TJobStepState>
        where TJobStepState : JobStepState
    {
        static readonly object _lock = new();
        static IJobStepStorage<TJobStepState>? _instance;

        public static IJobStepStorage<TJobStepState> GetInstance(IEventStoreInstanceDatabase database)
        {
            if (_instance is not null)
            {
                return _instance;
            }

            lock (_lock)
            {
                _instance = new JobStepStorage<TJobStepState>(database);
                return _instance;
            }
        }
    }
}
