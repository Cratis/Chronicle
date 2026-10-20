// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Defines a lock held in a database, so schema changes to it are serialized across every kernel process using it.
/// </summary>
/// <remarks>
/// The lock belongs to the session of the connection it is acquired on, so that connection must stay open, and be
/// used for nothing else, until the lock is released. Closing the connection releases the lock too.
/// </remarks>
internal interface IDatabaseSchemaLock
{
    /// <summary>
    /// Acquire the lock, waiting for another holder to release it.
    /// </summary>
    /// <param name="connection">The open connection to hold the lock on.</param>
    /// <param name="resource">The name of what is locked.</param>
    /// <param name="timeout">How long to wait before giving up.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="DatabaseSchemaLockNotAcquired">The lock was not granted in time.</exception>
    Task Acquire(DbConnection connection, string resource, TimeSpan timeout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Release the lock.
    /// </summary>
    /// <param name="connection">The connection the lock was acquired on.</param>
    /// <param name="resource">The name of what is locked.</param>
    /// <returns>Awaitable task.</returns>
    Task Release(DbConnection connection, string resource);
}
