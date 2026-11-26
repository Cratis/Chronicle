// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name

internal static partial class UnseenObserverRemovalLogMessages
{
    [LoggerMessage(LogLevel.Information, "Removed observer {ObserverId} in event store {EventStore} and namespace {Namespace}: no client has subscribed to it for the configured period")]
    internal static partial void RemovedUnseenObserver(this ILogger<UnseenObserverRemoval> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, ObserverId observerId);

    [LoggerMessage(LogLevel.Warning, "Removing unseen observer {ObserverId} in event store {EventStore} failed; it is tried again by the next sweep")]
    internal static partial void RemovalFailed(this ILogger<UnseenObserverRemoval> logger, EventStoreName eventStore, ObserverId observerId, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Sweeping for unseen observers failed in event store '{EventStore}'")]
    internal static partial void SweepFailed(this ILogger<UnseenObserverRemoval> logger, EventStoreName eventStore, Exception exception);
}
