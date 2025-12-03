// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.ReadModels;

/// <summary>
/// Defines the storage for read model definitions.
/// </summary>
public interface IReadModelDefinitionsStorage
{
    /// <summary>
    /// Gets all read model definitions.
    /// </summary>
    /// <returns>A collection of read model definitions.</returns>
    Task<IEnumerable<ReadModelDefinition>> GetAll();

    /// <summary>
    /// Checks if a read model definition exists by its name.
    /// </summary>
    /// <param name="identifier">The identifier of the read model definition.</param>
    /// <returns>True if it exists, false otherwise.</returns>
    Task<bool> Has(ReadModelIdentifier identifier);

    /// <summary>
    /// Gets a specific read model definition by its name.
    /// </summary>
    /// <param name="identifier">The identifier of the read model definition.</param>
    /// <returns>The read model definition.</returns>
    Task<ReadModelDefinition> Get(ReadModelIdentifier identifier);

    /// <summary>
    /// Deletes a read model definition by its name.
    /// </summary>
    /// <param name="identifier">The identifier of the read model definition to delete.</param>
    /// <returns>An awaitable task.</returns>
    Task Delete(ReadModelIdentifier identifier);

    /// <summary>
    /// Saves a read model definition.
    /// </summary>
    /// <param name="definition">The read model definition to save.</param>
    /// <returns>An awaitable task.</returns>
    Task Save(ReadModelDefinition definition);
}
