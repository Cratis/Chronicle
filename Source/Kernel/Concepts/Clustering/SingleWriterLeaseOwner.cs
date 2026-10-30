// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Clustering;

/// <summary>
/// Identifies one process lifetime holding the single-writer lease.
/// </summary>
/// <param name="Value">The unique owner identity.</param>
public record SingleWriterLeaseOwner(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Creates an identity that is unique across process and container restarts.
    /// </summary>
    /// <returns>The owner identity.</returns>
    public static SingleWriterLeaseOwner New() => new($"{Environment.MachineName}/{Environment.ProcessId}/{Guid.NewGuid():N}");
}
