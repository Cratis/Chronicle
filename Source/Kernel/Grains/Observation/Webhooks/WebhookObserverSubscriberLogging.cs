// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Log messages for <see cref="WebhookObserverSubscriber"/>.
/// </summary>
internal static partial class WebhookObserverSubscriberLogging
{
    [LoggerMessage(LogLevel.Warning, "An error occurred while handling webhook for key {Key}.")]
    internal static partial void ErrorHandling(this ILogger<WebhookObserverSubscriber> logger, Exception ex, ObserverKey key);

    [LoggerMessage(LogLevel.Trace, "Successfully handled all events for webhook for key {Key}")]
    internal static partial void SuccessfullyHandledAllEvents(this ILogger<WebhookObserverSubscriber> logger, ObserverKey key);
}