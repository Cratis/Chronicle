// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Namespaces;

internal static partial class NamespaceRemoverLogging
{
    [LoggerMessage(LogLevel.Information, "Removing namespace '{Namespace}' from event store '{EventStore}'")]
    internal static partial void RemovingNamespace(this ILogger<NamespaceRemover> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Information, "Removed namespace '{Namespace}' from event store '{EventStore}'")]
    internal static partial void RemovedNamespace(this ILogger<NamespaceRemover> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace);
}
