// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Defines the monotonic provenance marker of a physical read-model database.
/// </summary>
public interface ISinkProvenanceStorage
{
    /// <summary>
    /// Gets a revocable lease from the silo cache. Raw or unsupported providers authorize nothing.
    /// </summary>
    /// <returns>The physical database lease.</returns>
    Task<SinkProvenanceLease> GetLease() => Task.FromResult(SinkProvenanceLease.Unavailable);

    /// <summary>
    /// Reads or atomically bootstraps the incomplete database-local inventory state.
    /// </summary>
    /// <returns>The committed state. Missing required schema must fail closed.</returns>
    Task<SinkProvenanceState> ReadState() => throw new NotSupportedException("Read-model storage is not available.");

    /// <summary>
    /// Atomically commits inventory evidence and completion if inventory is still incomplete.
    /// Already completed inventory discards late evidence from another inventory worker.
    /// </summary>
    /// <param name="restrictedEvidence">Whether this inventory found restricted evidence.</param>
    /// <returns>The committed state, including independent explicit marks.</returns>
    Task<SinkProvenanceState> CompleteInventory(bool restrictedEvidence) => throw new NotSupportedException("Read-model storage is not available.");

    /// <summary>
    /// Gets whether the database has hosted a protected producer. Failures must not be treated as absence.
    /// </summary>
    /// <returns>Whether the marker exists.</returns>
    Task<bool> IsQuarantined();

    /// <summary>
    /// Durably marks the database. There is deliberately no clearing operation.
    /// </summary>
    /// <returns>The persistence task.</returns>
    Task MarkQuarantined();
}
