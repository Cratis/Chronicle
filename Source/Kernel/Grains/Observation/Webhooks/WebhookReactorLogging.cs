// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

internal static partial class WebhookReactorLogging
{
    [LoggerMessage(LogLevel.Information, "Adding webhook {EventSourceId} with identifier {Identifier}")]
    internal static partial void AddingWebhook(this ILogger<WebhookReactor> logger, EventSourceId eventSourceId, WebhookId identifier);

    [LoggerMessage(LogLevel.Information, "Webhook {EventSourceId} with identifier {Identifier} added")]
    internal static partial void WebhookAdded(this ILogger<WebhookReactor> logger, EventSourceId eventSourceId, WebhookId identifier);

    [LoggerMessage(LogLevel.Information, "Setting basic authorization for webhook {WebhookId}")]
    internal static partial void SettingBasicAuthorizationForWebhook(this ILogger<WebhookReactor> logger, WebhookId webhookId);

    [LoggerMessage(LogLevel.Information, "Setting bearer token authorization for webhook {WebhookId}")]
    internal static partial void SettingBearerTokenAuthorizationForWebhook(this ILogger<WebhookReactor> logger, WebhookId webhookId);

    [LoggerMessage(LogLevel.Information, "Setting OAuth authorization for webhook {WebhookId}")]
    internal static partial void SettingOAuthAuthorizationForWebhook(this ILogger<WebhookReactor> logger, WebhookId webhookId);

    [LoggerMessage(LogLevel.Information, "Setting event types for webhook {WebhookId}")]
    internal static partial void SettingEventTypesForWebhook(this ILogger<WebhookReactor> logger, WebhookId webhookId);

    [LoggerMessage(LogLevel.Information, "Setting target URL for webhook {WebhookId}")]
    internal static partial void SettingTargetUrlForWebhook(this ILogger<WebhookReactor> logger, WebhookId webhookId);

    [LoggerMessage(LogLevel.Information, "Setting target headers for webhook {WebhookId}")]
    internal static partial void SettingTargetHeadersForWebhook(this ILogger<WebhookReactor> logger, WebhookId webhookId);

    [LoggerMessage(LogLevel.Information, "Removing webhook {EventSourceId}")]
    internal static partial void RemovingWebhook(this ILogger<WebhookReactor> logger, EventSourceId eventSourceId);

    [LoggerMessage(LogLevel.Information, "Webhook {EventSourceId} removed")]
    internal static partial void WebhookRemoved(this ILogger<WebhookReactor> logger, EventSourceId eventSourceId);
}
