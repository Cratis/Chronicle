// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Describes why a database schema lock was not acquired.
/// </summary>
internal enum DatabaseSchemaLockOutcome
{
    /// <summary>
    /// The lock was not acquired for an unspecified reason.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Waiting for the lock timed out.
    /// </summary>
    Timeout = 1,

    /// <summary>
    /// The lock request was chosen as a deadlock victim.
    /// </summary>
    Deadlock = 2,

    /// <summary>
    /// The lock request was canceled by the database.
    /// </summary>
    Canceled = 3,

    /// <summary>
    /// The database reported an error for the lock request.
    /// </summary>
    Error = 4
}
