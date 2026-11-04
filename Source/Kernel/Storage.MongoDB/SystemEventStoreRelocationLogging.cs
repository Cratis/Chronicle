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

    [LoggerMessage(LogLevel.Information, "Moved database '{From}' to '{To}'")]
    internal static partial void DatabaseMoved(this ILogger<SystemEventStoreRelocation> logger, string from, string to);

    [LoggerMessage(LogLevel.Information, "Database '{Name}' is named explicitly by connection resolution and stays where it is")]
    internal static partial void DatabaseNameUnchanged(this ILogger<SystemEventStoreRelocation> logger, string name);

    [LoggerMessage(LogLevel.Debug, "Keeping the existing index '{Index}' on '{Collection}' in database '{Database}'")]
    internal static partial void KeepingExistingIndex(this ILogger logger, string index, string collection, string database);
}
