// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Shares physical database observations and leases within one silo.
/// </summary>
public interface ISinkProvenanceCache
{
    /// <summary>
    /// Resolves a logical route to a lazily initialized physical database handle.
    /// </summary>
    /// <param name="route">A provider-specific route, not an authorization identity.</param>
    /// <param name="create">Creates raw durable metadata access.</param>
    /// <returns>The cached handle.</returns>
    ISinkProvenanceStorage Bind(object route, Func<ISinkProvenanceStorage> create);
}
