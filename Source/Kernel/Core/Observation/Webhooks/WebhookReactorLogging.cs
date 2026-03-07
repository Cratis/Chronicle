// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Webhooks;

internal static partial class WebhookReactorLogging
{
    [LoggerMessage(LogLevel.Debug, "Adding webhook {EventSourceId} with identifier {Identifier}")]
    internal static partial void AddingWebhook(this ILogger<WebhookReactor> logger, EventSourceId eventSourceId, WebhookId identifier);

    [LoggerMessage(LogLevel.Debug, "Webhook {EventSourceId} with identifier {Identifier} added")]
    internal static partial void WebhookAdded(this ILogger<WebhookReactor> logger, EventSourceId eventSourceId, WebhookId identifier);

    [LoggerMessage(LogLevel.Debug, "Setting basic authorization for webhook {WebhookId}")]
    internal static partial void SettingBasicAuthorizationForWebhook(this ILogger<WebhookReactor> logger, WebhookId webhookId);

    [LoggerMessage(LogLevel.Debug, "Setting bearer token authorization for webhook {WebhookId}")]
    internal static partial void SettingBearerTokenAuthorizationForWebhook(this ILogger<WebhookReactor> logger, WebhookId webhookId);

    [LoggerMessage(LogLevel.Debug, "Setting OAuth authorization for webhook {WebhookId}")]
    internal static partial void SettingOAuthAuthorizationForWebhook(this ILogger<WebhookReactor> logger, WebhookId webhookId);

    [LoggerMessage(LogLevel.Debug, "Setting event types for webhook {WebhookId}")]
    internal static partial void SettingEventTypesForWebhook(this ILogger<WebhookReactor> logger, WebhookId webhookId);

    [LoggerMessage(LogLevel.Debug, "Setting target URL for webhook {WebhookId}")]
    internal static partial void SettingTargetUrlForWebhook(this ILogger<WebhookReactor> logger, WebhookId webhookId);

    [LoggerMessage(LogLevel.Debug, "Setting target headers for webhook {WebhookId}")]
    internal static partial void SettingTargetHeadersForWebhook(this ILogger<WebhookReactor> logger, WebhookId webhookId);

    [LoggerMessage(LogLevel.Debug, "Removing webhook {EventSourceId}")]
    internal static partial void RemovingWebhook(this ILogger<WebhookReactor> logger, EventSourceId eventSourceId);

    [LoggerMessage(LogLevel.Debug, "Webhook {EventSourceId} removed")]
    internal static partial void WebhookRemoved(this ILogger<WebhookReactor> logger, EventSourceId eventSourceId);
}
