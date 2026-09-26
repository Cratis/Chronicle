// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Orleans.Concurrency;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Defines the manager for managing read models.
/// </summary>
public interface IReadModelsManager : IGrainWithStringKey
{
    /// <summary>
    /// Ensure the existence of the read models manager.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Interleaved because it does nothing - its whole purpose is to force activation, so there is no
    /// state for non-reentrancy to protect and nothing to gain from queueing it behind real work.
    /// </remarks>
    [AlwaysInterleave]
    Task Ensure();

    /// <summary>
    /// Register a collection of read model definitions.
    /// </summary>
    /// <param name="definitions">Collection of read model definitions to register.</param>
    /// <returns>Awaitable task.</returns>
    Task Register(IEnumerable<ReadModelDefinition> definitions);

    /// <summary>
    /// Register a single read model definition.
    /// </summary>
    /// <param name="definition">The read model definition to register.</param>
    /// <returns>Awaitable task.</returns>
    Task RegisterSingle(ReadModelDefinition definition);

    /// <summary>
    /// Update a read model definition.
    /// </summary>
    /// <param name="definition">The read model definition to update.</param>
    /// <returns>Awaitable task.</returns>
    Task UpdateDefinition(ReadModelDefinition definition);

    /// <summary>
    /// Get all registered read model definitions.
    /// </summary>
    /// <returns>Collection of <see cref="ReadModelDefinition"/>.</returns>
    Task<IEnumerable<ReadModelDefinition>> GetDefinitions();
}
