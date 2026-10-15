// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Discovery;

internal static partial class DiscoveryLogMessages
{
    [LoggerMessage(LogLevel.Information, "Discovered artifacts for event store '{EventStore}' namespace '{Namespace}': {EventTypes} event types, {Constraints} constraints, {Reactors} reactors, {Reducers} reducers, {Projections} projections, {ReadModels} read models")]
    internal static partial void ArtifactsDiscovered(this ILogger logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, int eventTypes, int constraints, int reactors, int reducers, int projections, int readModels);

    [LoggerMessage(LogLevel.Debug, "Discovered {Count} {Kind} artifacts for event store '{EventStore}' namespace '{Namespace}': {Ids}")]
    internal static partial void ArtifactsOfKindDiscovered(this ILogger logger, ArtifactKind kind, int count, EventStoreName eventStore, EventStoreNamespaceName @namespace, string ids);

    [LoggerMessage(LogLevel.Warning, "Reactor '{PreviousId}' likely renamed to '{CurrentId}' in event store '{EventStore}' namespace '{Namespace}'. The new reactor catches up from the start as ordinary observation: side effects re-run and [OnceOnly] does not help. Pin [Reactor(\"{PreviousId}\")] to preserve its position; Observers.Remove(\"{PreviousId}\") removes the orphan")]
    internal static partial void ReactorLikelyRenamed(this ILogger logger, string previousId, string currentId, EventStoreName eventStore, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Information, "{Kind} observer '{PreviousId}' likely renamed to '{CurrentId}' in event store '{EventStore}' namespace '{Namespace}'. The read model is rebuilt; renamed projections are also retired by the kernel")]
    internal static partial void ObserverLikelyRenamed(this ILogger logger, ArtifactKind kind, string previousId, string currentId, EventStoreName eventStore, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Warning, "Event type '{PreviousId}' likely renamed to '{CurrentId}' in event store '{EventStore}'. Stored history stays under the old id and is invisible to handlers of the new id")]
    internal static partial void EventTypeLikelyRenamed(this ILogger logger, string previousId, string currentId, EventStoreName eventStore);

    [LoggerMessage(LogLevel.Warning, "Identity drift check skipped for event store '{EventStore}' namespace '{Namespace}'; registration will continue")]
    internal static partial void IdentityDriftCheckSkipped(this ILogger logger, Exception exception, EventStoreName eventStore, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Debug, "Could not confirm subscription for observer '{ObserverId}'; omitting the identity drift candidate")]
    internal static partial void IdentityDriftConfirmationFailed(this ILogger logger, Exception exception, string observerId);

    [LoggerMessage(LogLevel.Debug, "Could not build discovery report for event store '{EventStore}' namespace '{Namespace}'; omitting discovery diagnostics")]
    internal static partial void DiscoveryReportFailed(this ILogger logger, Exception exception, EventStoreName eventStore, EventStoreNamespaceName @namespace);

    /// <summary>
    /// Logs discovery totals and, when enabled, the identities of each kind.
    /// </summary>
    /// <param name="logger">The diagnostic logger.</param>
    /// <param name="report">The discovered artifacts.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The event store namespace.</param>
    internal static void LogDiscovery(this ILogger logger, DiscoveryReport report, EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        logger.ArtifactsDiscovered(eventStore, @namespace, report.CountOf(ArtifactKind.EventType), report.CountOf(ArtifactKind.Constraint), report.CountOf(ArtifactKind.Reactor), report.CountOf(ArtifactKind.Reducer), report.CountOf(ArtifactKind.Projection), report.CountOf(ArtifactKind.ReadModel));
        if (logger.IsEnabled(LogLevel.Debug))
        {
            foreach (var kind in Enum.GetValues<ArtifactKind>())
            {
                logger.ArtifactsOfKindDiscovered(kind, report.CountOf(kind), eventStore, @namespace, string.Join(", ", report.OfKind(kind).Select(_ => _.Id)));
            }
        }
    }
}
