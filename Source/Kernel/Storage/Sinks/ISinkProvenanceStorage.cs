// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Defines the monotonic provenance marker of a physical read-model database.
/// </summary>
public interface ISinkProvenanceStorage
{
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
