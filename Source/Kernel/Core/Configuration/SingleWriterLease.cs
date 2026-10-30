// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Configures single-writer protection for Localhost clustering on persistent storage.
/// </summary>
public class SingleWriterLease
{
    /// <summary>
    /// Gets or sets whether to enable the lease. Null selects it automatically where applicable.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Gets or sets the database lease lifetime, at least five seconds.
    /// </summary>
    public TimeSpan Ttl { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets how long startup may wait. Zero attempts acquisition once.
    /// </summary>
    public TimeSpan AcquireTimeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Determines whether this lease applies to the configured clustering and storage.
    /// </summary>
    /// <param name="clustering">The clustering mode.</param>
    /// <param name="storageType">The storage type.</param>
    /// <returns>Whether the lease should be activated.</returns>
    public bool AppliesTo(ClusteringType clustering, string storageType) =>
        Enabled != false && clustering == ClusteringType.Localhost && !string.Equals(storageType, "InMemory", StringComparison.OrdinalIgnoreCase);
}
