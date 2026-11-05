// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// The exception that is thrown when the schema lock of a database could not be acquired.
/// </summary>
/// <param name="database">The database.</param>
/// <param name="resource">The name of what is locked.</param>
/// <param name="reason">Why it was not acquired.</param>
/// <param name="outcome">The structured lock outcome.</param>
#pragma warning disable CA1064 // Kernel-only provisioning diagnostic, not a public client contract.
internal sealed class DatabaseSchemaLockNotAcquired(string database, string resource, string reason, DatabaseSchemaLockOutcome outcome)
    : Exception($"DatabaseSchemaLockNotAcquired: the schema lock '{resource}' of database '{database}' was not acquired: {reason}. Another kernel may still be changing its schema.")
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseSchemaLockNotAcquired"/> class without a known lock outcome.
    /// </summary>
    /// <param name="database">The database.</param>
    /// <param name="resource">The name of what is locked.</param>
    /// <param name="reason">Why it was not acquired.</param>
    public DatabaseSchemaLockNotAcquired(string database, string resource, string reason)
        : this(database, resource, reason, DatabaseSchemaLockOutcome.Unknown)
    {
    }

    /// <summary>
    /// Gets the structured lock outcome, independent of its diagnostic text.
    /// </summary>
    internal DatabaseSchemaLockOutcome Outcome { get; } = outcome;
}
