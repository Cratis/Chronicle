// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.Kernel;

/// <summary>
/// Chooses the sink the kernel's own projections write to.
/// </summary>
/// <remarks>
/// The kernel's projections live alongside the rest of what the kernel stores, so they write to the sink of the
/// storage the kernel runs on. A fixed sink fails every event on any other storage - the sink cannot even be
/// built without the database it is for.
/// </remarks>
public static class KernelProjectionSinks
{
    /// <summary>
    /// Gets the sink type for the storage the kernel is configured with.
    /// </summary>
    /// <param name="storageType">The configured storage type.</param>
    /// <returns>The <see cref="SinkTypeId"/>.</returns>
    public static SinkTypeId ForStorage(string storageType) => storageType.ToLowerInvariant() switch
    {
        StorageType.Sqlite or StorageType.MsSql or StorageType.PostgreSql => WellKnownSinkTypes.SQL,
        StorageType.InMemory => WellKnownSinkTypes.InMemory,
        _ => WellKnownSinkTypes.MongoDB
    };
}
