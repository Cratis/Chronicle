// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Grains.Observation.Reducers.Clients;

/// <summary>
/// Defines a grain for working with all <see cref="IReducer">client reducers</see>.
/// </summary>
public interface IReducersManager : IGrainWithStringKey
{
    /// <summary>
    /// Register a collection of reducers for a specific connection.
    /// </summary>
    /// <param name="connectionId"><see cref="ConnectionId"/> to register for.</param>
    /// <param name="definitions">Collection of <see cref="ReducerDefinition"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Register(ConnectionId connectionId, IEnumerable<ReducerDefinition> definitions);
}
