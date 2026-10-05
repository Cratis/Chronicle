// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// The exception that is thrown when a replay backup cannot be distinguished from a legacy half-swap or a colliding table.
/// </summary>
/// <param name="tableName">The read model being promoted.</param>
/// <param name="revertName">The ambiguous backup table.</param>
public class UnverifiedReplayBackup(string tableName, string revertName)
    : Exception($"Cannot promote SQL replay for '{tableName}': revert table '{revertName}' has no unambiguous committed-backup ownership. It may be a legacy half-swap or a colliding table. No read model tables were changed; inspect and recover the existing tables before retrying.");
