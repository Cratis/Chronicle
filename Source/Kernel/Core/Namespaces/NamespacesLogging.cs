// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Namespaces;

internal static partial class NamespacesLogging
{
    [LoggerMessage(LogLevel.Trace, "Adding namespace '{Namespace}'")]
    internal static partial void AddingNamespace(this ILogger<INamespaces> logger, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Trace, "Broadcasting namespace '{Namespace}' has been added")]
    internal static partial void BroadcastAddedNamespace(this ILogger<INamespaces> logger, EventStoreNamespaceName @namespace);
}
