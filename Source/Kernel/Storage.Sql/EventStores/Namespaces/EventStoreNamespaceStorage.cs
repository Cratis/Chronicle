// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Chronicle.Storage.Keys;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Projections;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Recommendations;
using Cratis.Chronicle.Storage.Seeding;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Types;
using Microsoft.Extensions.Logging.Abstractions;
using ReadModelsReplayContexts = Cratis.Chronicle.Storage.ReadModels.ReplayContexts;
using SinksSinks = Cratis.Chronicle.Storage.Sinks.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreNamespaceStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="namespace">The name of the namespace.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
/// <param name="sinkFactories"><see cref="IInstancesOf{T}"/> for getting all <see cref="ISinkFactory"/> instances.</param>
/// <param name="jobTypes">The <see cref="IJobTypes"/> that knows about job types.</param>
/// <param name="observerDefinitionsStorage">The <see cref="IObserverDefinitionsStorage"/> for working with observer definitions.</param>
/// <param name="jsonSerializerOptions">The global <see cref="JsonSerializerOptions"/>.</param>
public class EventStoreNamespaceStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, IDatabase database, IInstancesOf<ISinkFactory> sinkFactories, IJobTypes jobTypes, IObserverDefinitionsStorage observerDefinitionsStorage, JsonSerializerOptions jsonSerializerOptions) : IEventStoreNamespaceStorage
{
    /// <inheritdoc/>
    public IChangesetStorage Changesets { get; } = new Changesets.ChangesetStorage(eventStore, @namespace, database);

    /// <inheritdoc/>
    public IIdentityStorage Identities { get; } = new Identities.IdentityStorage(eventStore, @namespace, database);

    /// <inheritdoc/>
    public IJobStorage Jobs { get; } = new Jobs.JobStorage(eventStore, @namespace, database, jobTypes);

    /// <inheritdoc/>
    public IJobStepStorage JobSteps { get; } = new JobSteps.JobStepStorage(eventStore, @namespace, database);

    /// <inheritdoc/>
    public IObserverStateStorage Observers { get; } = new Observers.ObserverStateStorage(eventStore, @namespace, database);

    /// <inheritdoc/>
    public IFailedPartitionsStorage FailedPartitions { get; } = new FailedPartitions.FailedPartitionStorage(eventStore, @namespace, database);

    /// <inheritdoc/>
    public IRecommendationStorage Recommendations { get; } = new Recommendations.RecommendationStorage(eventStore, @namespace, database);

    /// <inheritdoc/>
    public IObserverKeyIndexes ObserverKeyIndexes { get; } = new ObserverKeyIndexes.ObserverKeyIndexes(eventStore, @namespace, database, observerDefinitionsStorage);

    /// <inheritdoc/>
    public IReplayContexts ReplayContexts { get; } = new ReadModelsReplayContexts(new ReplayContexts.ReplayContextsStorage(eventStore, @namespace, database));

    /// <inheritdoc/>
    public ISinks Sinks { get; } = new SinksSinks(eventStore, @namespace, sinkFactories);

    /// <inheritdoc/>
    public IReplayedReadModelsStorage ReplayedReadModels { get; } = new ReplayedModels.ReplayedModelsStorage(eventStore, @namespace, database);

    /// <inheritdoc/>
    public IEventSeedingStorage EventSeeding { get; } = new Seeding.EventSeedingStorage(eventStore, @namespace, database, jsonSerializerOptions);

    /// <inheritdoc/>
    public IProjectionFuturesStorage ProjectionFutures { get; } = new Projections.ProjectionFuturesStorage(eventStore, @namespace, database, jsonSerializerOptions);

    /// <inheritdoc/>
    public IEventSequenceStorage GetEventSequence(EventSequenceId eventSequenceId) =>
        new EventSequences.EventSequenceStorage(
            eventStore,
            @namespace,
            eventSequenceId,
            database,
            Identities, // Use existing Identities property
            NullLogger<EventSequences.EventSequenceStorage>.Instance); // Null logger for now

    /// <inheritdoc/>
    public IUniqueConstraintsStorage GetUniqueConstraintsStorage(EventSequenceId eventSequenceId) => new UniqueConstraints.UniqueConstraintsStorage(eventStore, @namespace, eventSequenceId, database);

    /// <inheritdoc/>
    public IUniqueEventTypesConstraintsStorage GetUniqueEventTypesConstraints(EventSequenceId eventSequenceId) => new UniqueEventTypesConstraints.UniqueEventTypesConstraintsStorage(eventStore, @namespace, eventSequenceId, database);
}
