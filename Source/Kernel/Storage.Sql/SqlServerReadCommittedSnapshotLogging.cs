// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql;

internal static partial class SqlServerReadCommittedSnapshotLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Enabling read-committed snapshot on database {Database} is blocked: {Diagnostics}")]
    internal static partial void Blocked(this ILogger logger, string database, string diagnostics);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Existing database {Database} has read-committed snapshot off. Provisioning will not change an existing database or evict its sessions. Enable it in a maintenance window with ALTER DATABASE ... SET READ_COMMITTED_SNAPSHOT ON.")]
    internal static partial void ReadCommittedSnapshotOff(this ILogger logger, string database);
}
