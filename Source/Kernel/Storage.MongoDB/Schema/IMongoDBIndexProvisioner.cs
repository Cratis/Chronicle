// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Schema;

/// <summary>
/// Verifies or provisions Chronicle-owned MongoDB indexes without starting the kernel.
/// </summary>
public interface IMongoDBIndexProvisioner
{
    /// <summary>
    /// Inspects existing indexes without writing data or issuing index DDL.
    /// </summary>
    /// <param name="eventStores">Additional stores, or none for discovered stores.</param>
    /// <param name="namespaces">Additional namespaces for explicitly requested stores.</param>
    /// <param name="cancellationToken">Cancellation of the operation.</param>
    /// <returns>The index state per scope.</returns>
    Task<MongoDBIndexReport> Verify(IEnumerable<string>? eventStores = null, IEnumerable<string>? namespaces = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates missing indexes, prunes obsolete event indexes, and verifies the result.
    /// </summary>
    /// <param name="eventStores">Additional stores, or none for discovered stores.</param>
    /// <param name="namespaces">Additional namespaces for explicitly requested stores.</param>
    /// <param name="cancellationToken">Cancellation of the operation.</param>
    /// <returns>A fresh index report.</returns>
    Task<MongoDBIndexReport> Migrate(IEnumerable<string>? eventStores = null, IEnumerable<string>? namespaces = null, CancellationToken cancellationToken = default);
}
