// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Validates ordinary sink acquisition independently of cached sink instances.
/// </summary>
public static class SinkAcquisitionProvenance
{
    /// <summary>
    /// Checks the destination and the current database marker before any cache hit or provisioning.
    /// </summary>
    /// <param name="definition">The actual destination definition.</param>
    /// <param name="provenance">The database-local provenance.</param>
    /// <returns>The admission task.</returns>
    public static Task Ensure(ReadModelDefinition definition, ISinkProvenanceStorage provenance)
    {
        SinkProvenanceNames.EnsureNotReserved(definition.ContainerName.Value);
        return Ensure(provenance);
    }

    /// <summary>
    /// Revalidates an already acquired sink before starting another read-modify-write session.
    /// </summary>
    /// <param name="provenance">The database-local marker.</param>
    /// <returns>The admission task.</returns>
    public static async Task Ensure(ISinkProvenanceStorage provenance)
    {
        if (await provenance.IsQuarantined())
        {
            throw new InvalidOperationException("Read-model storage is not available.");
        }
    }
}
