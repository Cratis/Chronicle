// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Reducers;
using Aksio.Execution;

namespace Aksio.Cratis.Specifications;

/// <summary>
/// Represents a null implementation of <see cref="IEventStore"/> that does nothing.
/// </summary>
public class NullEventStore : IEventStore
{
    /// <inheritdoc/>
    public EventStoreName EventStoreName => throw new NotImplementedException();

    /// <inheritdoc/>
    public TenantId TenantId => throw new NotImplementedException();

    /// <inheritdoc/>
    public ICratisConnection Connection => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEventTypes EventTypes => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEventLog EventLog => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEventOutbox EventOutbox => throw new NotImplementedException();

    /// <inheritdoc/>
    public IObservers Observers => throw new NotImplementedException();

    /// <inheritdoc/>
    public IReducers Reducers => throw new NotImplementedException();

    /// <inheritdoc/>
    public IProjections Projections => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task DiscoverAll() => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEventSequence GetEventSequence(EventSequenceId id) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task RegisterAll() => throw new NotImplementedException();
}
