// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Alerts;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.Keys;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Patterns;
using Cratis.Chronicle.Storage.Projections;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Recommendations;
using Cratis.Chronicle.Storage.Seeding;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Defines the storage for a specific instance of an event store namespace.
/// </summary>
public interface IEventStoreNamespaceStorage
{
    /// <summary>
    /// Gets this namespace's occurrence queue infrastructure.
    /// </summary>
    /// <exception cref="Occurrences.OccurrencesStorageNotSupported">The custom provider has not implemented occurrence storage.</exception>
    Occurrences.IOccurrencesStorage Occurrences => throw new Occurrences.OccurrencesStorageNotSupported();

    /// <summary>
    /// Gets this namespace's version-fenced schedule cursors and atomic turns.
    /// </summary>
    /// <exception cref="Occurrences.OccurrencesStorageNotSupported">The custom provider has not implemented schedule storage.</exception>
    Occurrences.IScheduleStorage Schedules => throw new Occurrences.OccurrencesStorageNotSupported();

    /// <summary>
    /// Gets the staging storage for import sessions.
    /// </summary>
    /// <exception cref="Imports.ImportStorageNotSupported">The custom provider has not implemented import staging.</exception>
    Imports.IImportStorage Imports => throw new Imports.ImportStorageNotSupported();

    /// <summary>
    /// Gets the <see cref="IEventSequenceStorage"/> for the event store namespace.
    /// </summary>
    IChangesetStorage Changesets { get; }

    /// <summary>
    /// Gets the <see cref="IIdentityStorage"/> for the event store.
    /// </summary>
    IIdentityStorage Identities { get; }

    /// <summary>
    /// Gets the <see cref="IJobStorage"/> for the event store namespace.
    /// </summary>
    IJobStorage Jobs { get; }

    /// <summary>
    /// Gets the <see cref="IJobStepStorage"/> for the event store namespace.
    /// </summary>
    IJobStepStorage JobSteps { get; }

    /// <summary>
    /// Gets the <see cref="IObserverStateStorage"/> for the event store namespace.
    /// </summary>
    IObserverStateStorage Observers { get; }

    /// <summary>
    /// Gets the independently paged definition-change progress, successor and inventory stores.
    /// </summary>
    ObserverDefinitionChangeStorage ObserverDefinitionChanges { get; }

    /// <summary>
    /// Gets the retained alert incident storage for this namespace.
    /// </summary>
    IAlertIncidentsStorage AlertIncidents { get; }

    /// <summary>
    /// Gets the <see cref="IEventSequenceStorage"/> for the event store namespace.
    /// </summary>
    IFailedPartitionsStorage FailedPartitions { get; }

    /// <summary>
    /// Gets the <see cref="IInFlightEventsStorage"/> for the event store namespace.
    /// </summary>
    IInFlightEventsStorage InFlightEvents { get; }

    /// <summary>
    /// Gets the <see cref="IObserverHandledCountsStorage"/> for the event store namespace.
    /// </summary>
    IObserverHandledCountsStorage ObserverHandledCounts { get; }

    /// <summary>
    /// Gets the <see cref="IRecommendationStorage"/> for the event store namespace.
    /// </summary>
    IRecommendationStorage Recommendations { get; }

    /// <summary>
    /// Gets the <see cref="IBehaviorPatternStorage"/> for the event store namespace.
    /// </summary>
    IBehaviorPatternStorage Patterns { get; }

    /// <summary>
    /// Gets the <see cref="IObserverKeyIndexes"/>  for the event store namespace.
    /// </summary>
    IObserverKeyIndexes ObserverKeyIndexes { get; }

    /// <summary>
    /// Gets the <see cref="IReplayContexts"/> for the event store namespace.
    /// </summary>
    IReplayContexts ReplayContexts { get; }

    /// <summary>
    /// Gets the <see cref="ISinks"/> for the event store namespace.
    /// </summary>
    ISinks Sinks { get; }

    /// <summary>
    /// Gets the <see cref="IReplayedReadModelsStorage"/> for the event store namespace.
    /// </summary>
    IReplayedReadModelsStorage ReplayedReadModels { get; }

    /// <summary>
    /// Gets the <see cref="IEventSeedingStorage"/> for the event store namespace.
    /// </summary>
    IEventSeedingStorage EventSeeding { get; }

    /// <summary>
    /// Gets the <see cref="IProjectionFuturesStorage"/> for the event store namespace.
    /// </summary>
    IProjectionFuturesStorage ProjectionFutures { get; }

    /// <summary>
    /// Gets the namespace-scoped permanent event sequence mutation registry.
    /// </summary>
    IEventSequenceMutationRegistry EventSequenceMutations => UnsupportedEventSequenceMutationRegistry.Instance;

    /// <summary>
    /// Get the event sequences that exist for the event store namespace.
    /// </summary>
    /// <returns>A collection of <see cref="EventSequenceId"/>.</returns>
    /// <remarks>
    /// An event sequence exists from the moment it holds state, so this is what the namespace has
    /// actually been used for rather than what it could be used for. The well-known sequences are
    /// not implied - a caller that needs them regardless has to add them itself.
    /// </remarks>
    Task<IEnumerable<EventSequenceId>> GetEventSequences();

    /// <summary>
    /// Checks whether any event sequence in the namespace contains a persisted event.
    /// </summary>
    /// <returns>True if an event sequence contains an event, false otherwise.</returns>
    /// <remarks>
    /// Known operational state, empty event sequence handles, and sequence snapshots do not establish event presence.
    /// Unknown or auxiliary physical containers with rows may conservatively report true, avoiding missed history.
    /// This read-only check does not materialize storage. Recovery callers should use <see cref="HasPersistedState"/>.
    /// </remarks>
    Task<bool> HasData();

    /// <summary>
    /// Checks conservatively whether the namespace has persisted operational state that may require recovery.
    /// </summary>
    /// <returns>True if the namespace may have persisted state, even without events; false if unused.</returns>
    /// <remarks>
    /// Use this check before recovering jobs, observers, and other state independently of event presence.
    /// It preserves the provider's existing namespace-use check, including its provisioning behavior.
    /// </remarks>
    Task<bool> HasPersistedState();

    /// <summary>
    /// Get the <see cref="IEventSequenceStorage"/> for a specific <see cref="EventSequenceId"/>.
    /// </summary>
    /// <param name="eventSequenceId"><see cref="EventSequenceId"/> to get for.</param>
    /// <returns>The <see cref="IEventStoreNamespaceStorage"/> instance.</returns>
    IEventSequenceStorage GetEventSequence(EventSequenceId eventSequenceId);

    /// <summary>
    /// Gets the storage for unique constraints.
    /// </summary>
    /// <param name="eventSequenceId"><see cref="EventSequenceId"/> to get for.</param>
    /// <returns>The <see cref="IUniqueConstraintsStorage"/> instance.</returns>
    IUniqueConstraintsStorage GetUniqueConstraintsStorage(EventSequenceId eventSequenceId);

    /// <summary>
    /// Gets the storage for unique event type constraints.
    /// </summary>
    /// <param name="eventSequenceId"><see cref="EventSequenceId"/> to get for.</param>
    /// <returns>The <see cref="IUniqueEventTypesConstraintsStorage"/> instance.</returns>
    IUniqueEventTypesConstraintsStorage GetUniqueEventTypesConstraints(EventSequenceId eventSequenceId);

    /// <summary>
    /// Gets the storage for closed streams.
    /// </summary>
    /// <param name="eventSequenceId"><see cref="EventSequenceId"/> to get for.</param>
    /// <returns>The <see cref="IClosedStreamsConstraintStorage"/> instance.</returns>
    IClosedStreamsConstraintStorage GetClosedStreamsConstraints(EventSequenceId eventSequenceId);
}
