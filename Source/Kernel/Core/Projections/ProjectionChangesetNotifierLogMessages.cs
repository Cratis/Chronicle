// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class ProjectionChangesetNotifierLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Changeset notifier {Notifier} activated")]
    internal static partial void Activated(this ILogger<ProjectionChangesetNotifier> logger, string notifier);

    [LoggerMessage(LogLevel.Debug, "Changeset notifier {Notifier} deactivated with {ObserverCount} observer(s) still registered (reason: {Reason})")]
    internal static partial void Deactivated(this ILogger<ProjectionChangesetNotifier> logger, string notifier, int observerCount, DeactivationReasonCode reason);

    [LoggerMessage(LogLevel.Debug, "Observer subscribed to changeset notifier {Notifier}; {ObserverCount} observer(s) now registered")]
    internal static partial void Subscribed(this ILogger<ProjectionChangesetNotifier> logger, string notifier, int observerCount);

    [LoggerMessage(LogLevel.Debug, "Changeset notifier {Notifier} notifying {ObserverCount} observer(s) for namespace {Namespace} key {ReadModelKey}")]
    internal static partial void Notifying(this ILogger<ProjectionChangesetNotifier> logger, string notifier, int observerCount, EventStoreNamespaceName @namespace, ReadModelKey readModelKey);
}
