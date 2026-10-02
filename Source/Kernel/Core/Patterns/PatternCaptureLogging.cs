// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patterns;

internal static partial class PatternCaptureLogging
{
    [LoggerMessage(LogLevel.Warning, "Failed capturing behavior patterns for event store {EventStore} in namespace {Namespace}")]
    internal static partial void FailedCapturingPatterns(this ILogger<PatternCaptureSubscriber> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, Exception exception);

    [LoggerMessage(LogLevel.Debug, "Subscribing pattern capture for event store {EventStore} in namespace {Namespace} to {EventTypeCount} event types")]
    internal static partial void SubscribingPatternCapture(this ILogger<PatternCapture> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, int eventTypeCount);

    [LoggerMessage(LogLevel.Debug, "No event types are registered for event store {EventStore}, so there is nothing for pattern capture to observe yet")]
    internal static partial void NoEventTypesToCapture(this ILogger<PatternCapture> logger, EventStoreName eventStore);
}
