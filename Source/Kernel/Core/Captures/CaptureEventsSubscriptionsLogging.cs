// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Log messages for <see cref="CaptureEventsSubscriptions"/>.
/// </summary>
internal static partial class CaptureEventsSubscriptionsLogging
{
    [LoggerMessage(LogLevel.Error, "Failed unsubscribing capture '{Name}' in namespace '{Namespace}' while rolling back a failed start")]
    internal static partial void FailedRollingBackCaptureSubscription(this ILogger<CaptureEventsSubscriptions> logger, Exception exception, CaptureName name, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Warning, "Recovering the subscription of capture '{Name}' in {EventStore}/{Namespace}")]
    internal static partial void RecoveringCaptureSubscription(this ILogger<CaptureEventsSubscriptions> logger, CaptureName name, EventStoreName eventStore, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Warning, "The inbox of capture '{Name}' cannot be resolved - there is nothing to unsubscribe")]
    internal static partial void CouldNotResolveCaptureSequence(this ILogger<CaptureEventsSubscriptions> logger, CaptureName name);
}
