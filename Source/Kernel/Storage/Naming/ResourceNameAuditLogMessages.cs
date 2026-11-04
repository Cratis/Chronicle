// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Holds log messages for <see cref="ResourceNameAudit"/>.
/// </summary>
internal static partial class ResourceNameAuditLogMessages
{
    [LoggerMessage(LogLevel.Critical, "The names in use could not be read, so the installation stays unavailable")]
    internal static partial void ReadingFailed(this ILogger<ResourceNameAudit> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Reading the names in use failed and will be retried")]
    internal static partial void ReadingRetried(this ILogger<ResourceNameAudit> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "The event store is unavailable because its storage overlaps the storage of another name: '{EventStore}'")]
    internal static partial void EventStoreQuarantined(this ILogger<ResourceNameAudit> logger, string eventStore);

    [LoggerMessage(LogLevel.Warning, "The namespace is unavailable because its storage overlaps the storage of another name: '{EventStore}#{Namespace}'")]
    internal static partial void NamespaceQuarantined(this ILogger<ResourceNameAudit> logger, string eventStore, string @namespace);

    [LoggerMessage(LogLevel.Critical, "A name overlaps storage the kernel keeps for itself: '{Names}'")]
    internal static partial void KernelOverlap(this ILogger<ResourceNameAudit> logger, string names);

    [LoggerMessage(LogLevel.Error, "Names overlap in storage: '{Names}'")]
    internal static partial void Overlap(this ILogger<ResourceNameAudit> logger, string names);

    [LoggerMessage(LogLevel.Information, "The names in use were checked: {EventStores} event stores and {Namespaces} namespaces are unavailable because of {Overlaps} overlaps")]
    internal static partial void Completed(this ILogger<ResourceNameAudit> logger, int eventStores, int namespaces, int overlaps);
}
