// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Log messages for <see cref="CaptureEventsNamespaceSubscriptions"/>.
/// </summary>
internal static partial class CaptureEventsNamespaceSubscriptionsLogging
{
    [LoggerMessage(LogLevel.Error, "Failed subscribing the events captures of {EventStore} in the added namespace '{Namespace}'")]
    internal static partial void FailedSubscribingCapturesInNamespace(this ILogger<CaptureEventsNamespaceSubscriptions> logger, Exception exception, EventStoreName eventStore, EventStoreNamespaceName @namespace);
}
