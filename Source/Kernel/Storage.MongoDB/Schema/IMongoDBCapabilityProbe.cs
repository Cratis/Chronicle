// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Schema;

/// <summary>
/// Probes the storage features required by Chronicle without assuming MongoDB API compatibility implies support.
/// </summary>
public interface IMongoDBCapabilityProbe
{
    /// <summary>
    /// Inspects change-stream and collation support with bounded operations.
    /// </summary>
    /// <param name="cancellationToken">The startup cancellation token.</param>
    /// <returns>The capability report.</returns>
    Task<MongoDBCapabilityReport> Probe(CancellationToken cancellationToken);
}
