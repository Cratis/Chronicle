// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Namespaces;

internal static partial class NamespacesLogging
{
    [LoggerMessage(LogLevel.Warning, "Namespace '{Requested}' in event store '{EventStore}' is a case variant of '{Canonical}'; refusing sequence activation")]
    internal static partial void RefusedCaseVariant(this ILogger<Cratis.Chronicle.EventSequences.EventSequence> logger, string requested, string eventStore, string canonical);

    [LoggerMessage(LogLevel.Information, "Namespace '{Variant}' in event store '{EventStore}' is a case variant of '{Canonical}'; merged into '{Canonical}'")]
    internal static partial void MergedNamespace(this ILogger<NamespacesStateStorageProvider> logger, string eventStore, EventStoreNamespaceName variant, EventStoreNamespaceName canonical);

    [LoggerMessage(LogLevel.Warning, "Namespace '{Variant}' in event store '{EventStore}' also has data; it is left in place and not reachable after merging into '{Canonical}'")]
    internal static partial void VariantDataLeftInPlace(this ILogger<NamespacesStateStorageProvider> logger, string eventStore, EventStoreNamespaceName variant, EventStoreNamespaceName canonical);

    [LoggerMessage(LogLevel.Warning, "Could not delete case variant '{Variant}' from event store '{EventStore}'; the namespace list will be repaired on the next load")]
    internal static partial void CouldNotDeleteVariant(this ILogger<NamespacesStateStorageProvider> logger, Exception exception, string eventStore, EventStoreNamespaceName variant);

    [LoggerMessage(LogLevel.Warning, "Namespace '{Namespace}' was removed but the removal could not be recorded")]
    internal static partial void CouldNotRecordNamespaceRemoval(this ILogger<INamespaces> logger, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Trace, "Adding namespace '{Namespace}'")]
    internal static partial void AddingNamespace(this ILogger<INamespaces> logger, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Trace, "Broadcasting namespace '{Namespace}' has been added")]
    internal static partial void BroadcastAddedNamespace(this ILogger<INamespaces> logger, EventStoreNamespaceName @namespace);
}
