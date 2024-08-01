// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Storage.Observation.Reducers;

/// <summary>
/// Defines a system for working with <see cref="ReducerDefinition"/>.
/// </summary>
public interface IReducerDefinitionsStorage
{
    /// <summary>
    /// Get all <see cref="ReducerDefinition">definitions</see> registered.
    /// </summary>
    /// <returns>A collection of <see cref="ReducerDefinition"/>.</returns>
    Task<IEnumerable<ReducerDefinition>> GetAll();

    /// <summary>
    /// Check if a <see cref="ReducerDefinition"/> exists by its <see cref="ReducerId"/>.
    /// </summary>
    /// <param name="id"><see cref="ReducerId"/> to check for.</param>
    /// <returns>True if it exists, false if not.</returns>
    Task<bool> Has(ReducerId id);

    /// <summary>
    /// Get a specific <see cref="ReducerDefinition"/> by its <see cref="ReducerId"/>.
    /// </summary>
    /// <param name="id"><see cref="ReducerId"/> to get for.</param>
    /// <returns><see cref="ReducerDefinition"/>.</returns>
    Task<ReducerDefinition> Get(ReducerId id);

    /// <summary>
    /// Delete a <see cref="ReducerDefinition"/> by its <see cref="ReducerId"/>.
    /// </summary>
    /// <param name="id"><see cref="ReducerId"/> of the <see cref="ReducerDefinition"/> to delete.</param>
    /// <returns>Awaitable task.</returns>
    Task Delete(ReducerId id);

    /// <summary>
    /// Save a <see cref="ReducerDefinition"/>.
    /// </summary>
    /// <param name="definition">Definition to save.</param>
    /// <returns>Async task.</returns>
    Task Save(ReducerDefinition definition);
}
