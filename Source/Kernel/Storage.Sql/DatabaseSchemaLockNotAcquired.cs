// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// The exception that is thrown when the schema lock of a database could not be acquired.
/// </summary>
/// <param name="database">The database.</param>
/// <param name="resource">The name of what is locked.</param>
/// <param name="reason">Why it was not acquired.</param>
#pragma warning disable CA1064 // Kernel-only provisioning diagnostic, not a public client contract.
internal sealed class DatabaseSchemaLockNotAcquired(string database, string resource, string reason)
    : Exception($"DatabaseSchemaLockNotAcquired: the schema lock '{resource}' of database '{database}' was not acquired: {reason}. Another kernel may still be changing its schema.");
