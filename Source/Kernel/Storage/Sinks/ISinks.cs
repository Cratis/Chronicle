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
    /// Resolve a sink, optionally without creating indexes or containers during publication recovery.
    /// </summary>
    /// <param name="readModel">The read model.</param>
    /// <param name="ensureIndexes">Whether initialization may create storage objects.</param>
    /// <returns>The sink.</returns>
    /// <exception cref="ReplayIsolationNotSupported">The provider cannot resolve a sink without creating storage objects.</exception>
    Task<ISink> GetFor(ReadModelDefinition readModel, bool ensureIndexes) => ensureIndexes
        ? GetFor(readModel)
        : throw new ReplayIsolationNotSupported(readModel.Sink.Type);
}
