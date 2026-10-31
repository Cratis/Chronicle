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

    [LoggerMessage(LogLevel.Warning, "SQL read model {ReadModel} has a composite key that cannot be rebuilt from its stored id ({Reason}); instances are returned without their key. Rows whose key parts contain '_' or are empty may also share a row with another key; replaying the read model does not change this until the key format changes (#5335).")]
    internal static partial void CompositeKeyNotRebuilt(this ILogger<Sink> logger, string readModel, string reason);
}
