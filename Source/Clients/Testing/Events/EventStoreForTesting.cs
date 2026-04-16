// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Chronicle.Transactions;
using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Testing.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventStore"/> for testing.
/// </summary>
public class EventStoreForTesting : IEventStore
{
    readonly ReadModelsForTesting _readModels = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreForTesting"/> class.
    /// </summary>
    public EventStoreForTesting()
    {
        Connection = new ChronicleConnectionForTesting();
    }

    /// <inheritdoc/>
    public EventStoreName Name => nameof(EventStoreForTesting);

    /// <inheritdoc/>
    public EventStoreNamespaceName Namespace => "Default";

    /// <inheritdoc/>
    public IChronicleConnection Connection { get; }

    /// <inheritdoc/>
    public IEventTypes EventTypes => throw new NotSupportedException("EventTypes is not supported in EventStoreForTesting. Use Defaults.Instance.EventTypes instead.");

    /// <inheritdoc/>
    public IEventLog EventLog => throw new NotSupportedException("EventLog is not supported in EventStoreForTesting. Use EventScenario for event sequence testing.");

    /// <inheritdoc/>
    public IReactors Reactors => throw new NotSupportedException("Reactors is not supported in EventStoreForTesting.");

    /// <inheritdoc/>
    public IReducers Reducers => throw new NotSupportedException("Reducers is not supported in EventStoreForTesting.");

    /// <inheritdoc/>
    public IProjections Projections => throw new NotSupportedException("Projections is not supported in EventStoreForTesting.");

    /// <inheritdoc/>
    public IWebhooks Webhooks => throw new NotSupportedException("Webhooks is not supported in EventStoreForTesting.");

    /// <inheritdoc/>
    public IEventStoreSubscriptions Subscriptions => throw new NotSupportedException("Subscriptions is not supported in EventStoreForTesting.");

    /// <inheritdoc/>
    public IConstraints Constraints => throw new NotSupportedException("Constraints is not supported in EventStoreForTesting. Use EventScenario for constraint testing.");

    /// <inheritdoc/>
    public IUnitOfWorkManager UnitOfWorkManager => throw new NotSupportedException("UnitOfWorkManager is not supported in EventStoreForTesting.");

    /// <inheritdoc/>
    public IFailedPartitions FailedPartitions => throw new NotSupportedException("FailedPartitions is not supported in EventStoreForTesting.");

    /// <inheritdoc/>
    public IJobs Jobs => throw new NotSupportedException("Jobs is not supported in EventStoreForTesting.");

    /// <inheritdoc/>
    public IReadModels ReadModels => _readModels;

    /// <inheritdoc/>
    public Seeding.IEventSeeding Seeding => throw new NotSupportedException("Seeding is not supported in EventStoreForTesting.");

    /// <inheritdoc/>
    public Task DiscoverAll() => Task.CompletedTask;

    /// <inheritdoc/>
    public IEventSequence GetEventSequence(EventSequenceId id) => throw new NotSupportedException("GetEventSequence is not supported in EventStoreForTesting. Use EventScenario for event sequence testing.");

    /// <inheritdoc/>
    public Task<IEnumerable<EventStoreNamespaceName>> GetNamespaces(CancellationToken cancellationToken = default) => throw new NotSupportedException("GetNamespaces is not supported in EventStoreForTesting.");

    /// <inheritdoc/>
    public Task RegisterAll() => Task.CompletedTask;
}
