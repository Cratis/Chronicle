// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.Jobs;
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
