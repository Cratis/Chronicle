// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB;

internal static partial class SystemEventStoreRelocationLogging
{
    [LoggerMessage(LogLevel.Information, "The System event store's databases are already named '{Name}', nothing to move")]
    internal static partial void NothingToMove(this ILogger<SystemEventStoreRelocation> logger, string name);

    [LoggerMessage(LogLevel.Information, "Moving the System event store's databases from '{Prefix}{From}' to '{Prefix}{To}'")]
    internal static partial void Moving(this ILogger<SystemEventStoreRelocation> logger, string prefix, string from, string to);

    [LoggerMessage(LogLevel.Information, "Moved the System event store's databases from '{From}' to '{To}'")]
    internal static partial void Moved(this ILogger<SystemEventStoreRelocation> logger, string from, string to);

    [LoggerMessage(LogLevel.Warning, "Moved the System event store's databases from '{From}' to '{To}', but the MongoDB user may not drop some of the '{From}' databases, so they remain as stale copies the kernel does not read. Drop them with a user that has the dropDatabase action, and do not set storage.systemDatabaseName back to '{From}'")]
    internal static partial void MovedWithSourcesRetained(this ILogger<SystemEventStoreRelocation> logger, string from, string to);

    [LoggerMessage(LogLevel.Warning, "Moved database '{From}' to '{To}', but the server refused to drop '{From}' on authorization grounds. It remains as a stale copy the kernel does not read")]
    internal static partial void DatabaseMovedSourceRetained(this ILogger<SystemEventStoreRelocation> logger, string from, string to);

    [LoggerMessage(LogLevel.Information, "Moved database '{From}' to '{To}'")]
    internal static partial void DatabaseMoved(this ILogger<SystemEventStoreRelocation> logger, string from, string to);

    [LoggerMessage(LogLevel.Information, "Database '{Name}' is named explicitly by connection resolution and stays where it is")]
    internal static partial void DatabaseNameUnchanged(this ILogger<SystemEventStoreRelocation> logger, string name);

    [LoggerMessage(LogLevel.Warning, "Could not confirm that collection '{Collection}' was copied from '{From}' to '{To}': {SourceCount} documents in the source, {TargetCount} in the target. The source is kept")]
    internal static partial void UnconfirmedCopy(this ILogger logger, string collection, string from, string to, long sourceCount, long targetCount);

    [LoggerMessage(LogLevel.Warning, "Kept database '{Name}' because its copy could not be confirmed. Nothing was deleted; the System event store's data is incomplete in '{To}' until it is")]
    internal static partial void DatabaseKept(this ILogger<SystemEventStoreRelocation> logger, string name, string to);

    [LoggerMessage(LogLevel.Debug, "Keeping the existing index '{Index}' on '{Collection}' in database '{Database}'")]
    internal static partial void KeepingExistingIndex(this ILogger logger, string index, string collection, string database);
}
