// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.XUnit.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventStore"/> for testing.
/// </summary>
public class EventStoreForTesting : IEventStore
{
    EventSequenceForTesting? _defaultEventSequence;

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
    public Aggregates.IAggregateRootFactory AggregateRootFactory => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEventTypes EventTypes => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEventLog EventLog => throw new NotImplementedException();

    /// <inheritdoc/>
    public IReactors Reactors => throw new NotImplementedException();

    /// <inheritdoc/>
    public IReducers Reducers => throw new NotImplementedException();

    /// <inheritdoc/>
    public IProjections Projections => throw new NotImplementedException();

    /// <inheritdoc/>
    public IConstraints Constraints => throw new NotImplementedException();

    /// <inheritdoc/>
    public IUnitOfWorkManager UnitOfWorkManager => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task DiscoverAll() => Task.CompletedTask;

    /// <inheritdoc/>
    public IEventSequence GetEventSequence(EventSequenceId id) =>
        _defaultEventSequence ??= new(Defaults.Instance.EventTypes);

    /// <inheritdoc/>
    public Task<IEnumerable<EventStoreNamespaceName>> GetNamespaces(CancellationToken cancellationToken = default) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task RegisterAll() => Task.CompletedTask;
}
