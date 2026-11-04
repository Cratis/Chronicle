// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// The exception that is thrown when read-committed snapshot could not be enabled on a database.
/// </summary>
/// <param name="database">The database.</param>
/// <param name="diagnostics">What is known about the sessions holding the database.</param>
/// <param name="inner">The SQL Server failure.</param>
#pragma warning disable CA1064 // Kernel-only provisioning diagnostic, not a public client contract.
internal sealed class ReadCommittedSnapshotNotEnabled(string database, ReadCommittedSnapshotDiagnostics diagnostics, Exception inner)
    : Exception($"ReadCommittedSnapshotNotEnabled: read-committed snapshot could not be enabled on database '{database}'; sessions that hold the database open may prevent it: {diagnostics.Describe()}.", inner);
