// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Storage.Observation.Reactors;

/// <summary>
/// Defines a system for working with <see cref="ReactorDefinition"/>.
/// </summary>
public interface IReactorDefinitionsStorage
{
    /// <summary>
    /// Get all <see cref="ReactorDefinition">definitions</see> registered.
    /// </summary>
    /// <returns>A collection of <see cref="ReactorDefinition"/>.</returns>
    Task<IEnumerable<ReactorDefinition>> GetAll();

    /// <summary>
    /// Check if a <see cref="ReactorDefinition"/> exists by its <see cref="ReactorId"/>.
    /// </summary>
    /// <param name="id"><see cref="ReactorId"/> to check for.</param>
    /// <returns>True if it exists, false if not.</returns>
    Task<bool> Has(ReactorId id);

    /// <summary>
    /// Get a specific <see cref="ReactorDefinition"/> by its <see cref="ReactorId"/>.
    /// </summary>
    /// <param name="id"><see cref="ReactorId"/> to get for.</param>
    /// <returns><see cref="ReactorDefinition"/>.</returns>
    Task<ReactorDefinition> Get(ReactorId id);

    /// <summary>
    /// Delete a <see cref="ReactorDefinition"/> by its <see cref="ReactorId"/>.
    /// </summary>
    /// <param name="id"><see cref="ReactorId"/> of the <see cref="ReactorDefinition"/> to delete.</param>
    /// <returns>Awaitable task.</returns>
    Task Delete(ReactorId id);

    /// <summary>
    /// Save a <see cref="ReactorDefinition"/>.
    /// </summary>
    /// <param name="definition">Definition to save.</param>
    /// <returns>Async task.</returns>
    Task Save(ReactorDefinition definition);

    /// <summary>
    /// Rename a reactor by its current identifier.
    /// </summary>
    /// <param name="currentId">The current <see cref="ReactorId"/>.</param>
    /// <param name="newId">The new <see cref="ReactorId"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Rename(ReactorId currentId, ReactorId newId);
}
