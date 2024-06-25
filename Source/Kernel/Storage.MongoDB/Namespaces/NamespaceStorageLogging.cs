// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces;

internal static partial class NamespaceStorageLogging
{
    [LoggerMessage(LogLevel.Trace, "Creating namespace '{Namespace}'")]
    internal static partial void Creating(this ILogger<NamespaceStorage> logger, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Trace, "Deleting namespace '{Namespace}'")]
    internal static partial void Deleting(this ILogger<NamespaceStorage> logger, EventStoreNamespaceName @namespace);
}
