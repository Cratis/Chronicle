// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB;

internal static partial class DatabaseNameCasingLogging
{
    [LoggerMessage(LogLevel.Warning, "Could not list the databases on the server to match the casing of database '{Name}'; using the name as it is")]
    internal static partial void CouldNotListDatabases(this ILogger logger, string name, Exception exception);
}
