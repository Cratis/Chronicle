// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Replay recovery diagnostics for <see cref="Sink"/>.
/// </summary>
internal static partial class SinkLogging
{
    [LoggerMessage(LogLevel.Warning, "SQL replay for {EventStore}/{Namespace}/{ReadModel} preserved markerless backup {OriginalTable} as {RecoveryTable} because the revert identifier was truncated. The recovery table is not subject to replay retention; inspect it and remove it manually only after confirming it is no longer needed.")]
    internal static partial void PreservedLegacyReplayBackup(this ILogger<Sink> logger, string eventStore, string @namespace, string readModel, string originalTable, string recoveryTable);
}
