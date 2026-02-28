// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.Reducers.Clients;

/// <summary>
/// Defines a client reducer.
/// </summary>
public interface IReducer : IGrainWithStringKey
{
    /// <summary>
    /// Set the reducer definition and subscribe as an observer.
    /// </summary>
    /// <param name="definition"><see cref="ReducerDefinition"/> to refresh with.</param>
    /// <returns>Awaitable task.</returns>
    Task SetDefinitionAndSubscribe(ReducerDefinition definition);

    /// <summary>
    /// Unsubscribe the reducer.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Unsubscribe();
}
