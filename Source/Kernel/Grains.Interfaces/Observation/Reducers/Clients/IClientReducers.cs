// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Reactions.Reducers;

namespace Cratis.Chronicle.Grains.Observation.Reducers.Clients;

/// <summary>
/// Defines a grain for working with all <see cref="IClientReducer">client reducers</see>.
/// </summary>
public interface IClientReducers : IGrainWithStringKey
{
    /// <summary>
    /// Register a collection of client reducers.
    /// </summary>
    /// <param name="connectionId"><see cref="ConnectionId"/> to register with.</param>
    /// <param name="definitions">Collection of <see cref="ReducerDefinition"/>.</param>
    /// <param name="namespaces">Collection of <see cref="EventStoreNamespaceName">namespaces</see> to register for.</param>
    /// <returns>Awaitable task.</returns>
    Task Register(ConnectionId connectionId, IEnumerable<ReducerDefinition> definitions, IEnumerable<EventStoreNamespaceName> namespaces);
}
