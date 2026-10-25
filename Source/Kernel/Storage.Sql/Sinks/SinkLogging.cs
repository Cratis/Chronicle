// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Replay recovery diagnostics for <see cref="Sink"/>.
/// </summary>
internal static partial class SinkLogging
{
    [LoggerMessage(LogLevel.Warning, "SQL index for {ReadModel} at {PropertyPath} was not created: {Reason}")]
    internal static partial void IndexNotCreated(this ILogger<Sink> logger, string readModel, string propertyPath, string reason);

    [LoggerMessage(LogLevel.Warning, "SQL array index for {ReadModel} at {PropertyPath}: direction is ignored because PostgreSQL GIN indexes are unordered")]
    internal static partial void ArrayIndexDirectionIgnored(this ILogger<Sink> logger, string readModel, string propertyPath);

    [LoggerMessage(LogLevel.Warning, "SQL index for {ReadModel} at {PropertyPath} could not be created; the read model keeps working without it")]
    internal static partial void IndexCreationFailed(this ILogger<Sink> logger, Exception exception, string readModel, string propertyPath);

    [LoggerMessage(LogLevel.Warning, "SQL replay for {EventStore}/{Namespace}/{ReadModel} preserved markerless backup {OriginalTable} as {RecoveryTable} because the revert identifier was truncated. The recovery table may hold an older backup or the previous read-model state from an interrupted pre-upgrade promotion. It is not subject to replay retention; inspect it before removing it manually.")]
    internal static partial void PreservedLegacyReplayBackup(this ILogger<Sink> logger, string eventStore, string @namespace, string readModel, string originalTable, string recoveryTable);

    [LoggerMessage(LogLevel.Warning, "SQL read model table {Table} for {EventStore}/{Namespace} stores composite keys in the legacy format. Writes are refused until a replay rebuilds it")]
    internal static partial void CompositeKeyWriteRefused(this ILogger<Sink> logger, string eventStore, string @namespace, string table);

    [LoggerMessage(LogLevel.Warning, "SQL read model table {Table} for {EventStore}/{Namespace} stores composite keys in the legacy format and is being read. The read model is waiting for a replay")]
    internal static partial void LegacyCompositeKeyRead(this ILogger<Sink> logger, string eventStore, string @namespace, string table);

    [LoggerMessage(LogLevel.Warning, "SQL read model table {Table} for {EventStore}/{Namespace} uses the current composite key format but holds a legacy-format key. It was probably written by an older kernel that was still running")]
    internal static partial void LegacyCompositeKeyInCurrentFormatTable(this ILogger<Sink> logger, string eventStore, string @namespace, string table);

    [LoggerMessage(LogLevel.Warning, "SQL read model table {Table} for the global scope in {EventStore} stores composite keys in the legacy format and is still being written in that format. A replay cannot rebuild a global read model yet")]
    internal static partial void GlobalCompositeKeyWrittenInLegacyFormat(this ILogger<Sink> logger, string eventStore, string table);

    [LoggerMessage(LogLevel.Warning, "SQL replay for {EventStore}/{Namespace}/{ReadModel} produced no rows, so the populated legacy composite-key table was kept and is not migrated to the current key format")]
    internal static partial void LegacyCompositeKeyTableNotMigrated(this ILogger<Sink> logger, string eventStore, string @namespace, string readModel);
}
