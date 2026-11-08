// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Diagnostics;

internal static partial class StorageAuthorizationDiagnosticsLogging
{
    [LoggerMessage(0, LogLevel.Error, "The storage server refused '{Operation}' on collection '{Collection}' in database '{Database}' (Unauthorized). The storage user Chronicle connects with is missing a privilege on '{Database}'. Grant it, or set storage.databaseNamePrefix so Chronicle's databases fall inside those the user may use. See https://www.cratis.io/chronicle/hosting/configuration/mongodb-permissions/#troubleshooting-authorization-errors")]
    internal static partial void StorageRefused(this ILogger logger, string operation, string collection, string database);

    [LoggerMessage(1, LogLevel.Information, "Storage access to collection '{Collection}' in database '{Database}' is authorized again")]
    internal static partial void StorageRecovered(this ILogger logger, string collection, string database);
}
