// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Observation.Reactors;
using Cratis.Chronicle.Storage.Observation.Reducers;
using Cratis.Chronicle.Storage.Projections;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreStorage"/> for a specific event store.
/// </summary>
public class EventStoreStorage : IEventStoreStorage
{
    /// <inheritdoc/>
    public EventStoreName EventStore => throw new NotImplementedException();

    /// <inheritdoc/>
    public INamespaceStorage Namespaces => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEventTypesStorage EventTypes => throw new NotImplementedException();

    /// <inheritdoc/>
    public IConstraintsStorage Constraints => throw new NotImplementedException();

    /// <inheritdoc/>
    public IObserverDefinitionsStorage Observers => throw new NotImplementedException();

    /// <inheritdoc/>
    public IReactorDefinitionsStorage Reactors => throw new NotImplementedException();

    /// <inheritdoc/>
    public IReducerDefinitionsStorage Reducers => throw new NotImplementedException();

    /// <inheritdoc/>
    public IProjectionDefinitionsStorage Projections => throw new NotImplementedException();

    /// <inheritdoc/>
    public IReadModelDefinitionsStorage ReadModels => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEventStoreNamespaceStorage GetNamespace(EventStoreNamespaceName @namespace) => throw new NotImplementedException();
}
