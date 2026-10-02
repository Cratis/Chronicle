// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patterns;

internal static partial class PatternCaptureSubscriptionsLogging
{
    [LoggerMessage(LogLevel.Warning, "Failed subscribing pattern capture for event store {EventStore} in namespace {Namespace} on attempt {Attempt} of {MaxAttempts}; retrying in {Delay}")]
    internal static partial void RetryingSubscription(this ILogger<PatternCaptureSubscriptions> logger, Exception exception, EventStoreName eventStore, EventStoreNamespaceName @namespace, int attempt, int maxAttempts, TimeSpan delay);

    [LoggerMessage(LogLevel.Warning, "Failed subscribing pattern capture for event store {EventStore} in namespace {Namespace}. The event log will reconcile the subscription when active")]
    internal static partial void FailedSubscribing(this ILogger<PatternCaptureSubscriptions> logger, Exception exception, EventStoreName eventStore, EventStoreNamespaceName @namespace);
}
