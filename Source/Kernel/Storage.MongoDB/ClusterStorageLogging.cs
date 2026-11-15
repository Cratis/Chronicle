// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB;

internal static partial class ClusterStorageLogging
{
    [LoggerMessage(LogLevel.Warning, "Skipping the registered event store '{Name}': its name is reserved or malformed and cannot be resolved to a database")]
    internal static partial void SkippingEventStoreWithReservedName(this ILogger<ClusterStorage> logger, string name);
}
