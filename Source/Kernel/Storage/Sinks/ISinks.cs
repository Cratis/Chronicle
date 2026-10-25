// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Defines a system for working with available <see cref="ISink">projection sinks</see>.
/// </summary>
public interface ISinks
{
    /// <summary>
    /// Check if there is a <see cref="ISink"/> of a specific <see cref="SinkTypeId"/> and <see cref="SinkConfigurationId"/>  registered in the system.
    /// </summary>
    /// <param name="typeId"><see cref="SinkTypeId"/> to check for.</param>
    /// <returns>True if it exists, false if not.</returns>
    bool HasType(SinkTypeId typeId);

    /// <summary>
    /// Get a <see cref="ISink"/> of a specific <see cref="SinkTypeId"/>.
    /// </summary>
    /// <param name="readModel"><see cref="ReadModelDefinition"/> to get for.</param>
    /// <returns><see cref="ISink"/> instance.</returns>
    Task<ISink> GetFor(ReadModelDefinition readModel);

    /// <summary>
    /// Get a <see cref="ISink"/> that writes to an explicit destination container instead of the read model's own container.
    /// </summary>
    /// <param name="readModel"><see cref="ReadModelDefinition"/> to get for.</param>
    /// <param name="container">The <see cref="ReadModelContainerName"/> the sink addresses.</param>
    /// <returns><see cref="ISink"/> instance.</returns>
    /// <remarks>
    /// A distinct container yields a distinct sink instance, so no mode (replay, bulk) is shared with the sink
    /// for the read model's own container. Used by materialization generations, each writing only to its own destination.
    /// </remarks>
    Task<ISink> GetFor(ReadModelDefinition readModel, ReadModelContainerName container);

    /// <summary>
    /// Gets a generation sink without sharing legacy mode or grant-specific cache entries.
    /// </summary>
    /// <param name="context">The immutable execution and fixed admission.</param>
    /// <returns>The generation sink.</returns>
    /// <exception cref="Generations.Bindings.MaterializationBindingsNotSupported">This provider cannot create generation writers.</exception>
    Task<ISink> GetFor(GenerationSinkContext context) => throw new Generations.Bindings.MaterializationBindingsNotSupported();
}
