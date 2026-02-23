// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Holds log messages for <see cref="Webhooks"/>.
/// </summary>
internal static partial class WebhooksLogging
{
    [LoggerMessage(LogLevel.Debug, "Setting definition for webhook '{Identifier}'")]
    internal static partial void SettingDefinition(this ILogger<Webhooks> logger, WebhookId identifier);

    [LoggerMessage(LogLevel.Debug, "Definition for webhook '{Identifier}' is not active. Observer will not be subscribed")]
    internal static partial void NotActive(this ILogger<Webhooks> logger, WebhookId identifier);

    [LoggerMessage(LogLevel.Debug, "Subscribing webhook '{Identifier}' in namespace '{Namespace}'")]
    internal static partial void Subscribing(this ILogger<Webhooks> logger, WebhookId identifier, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Debug, "Webhook '{Identifier}' in namespace '{Namespace}' was already subscribed")]
    internal static partial void AlreadySubscribed(this ILogger<Webhooks> logger, WebhookId identifier, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Debug, "Setting definition for webhook '{Identifier}'")]
    internal static partial void Unregistering(this ILogger<Webhooks> logger, WebhookId identifier);

    [LoggerMessage(LogLevel.Debug, "Unsubscribing webhook '{Identifier}' in namespace '{Namespace}'")]
    internal static partial void Unsubscribing(this ILogger<Webhooks> logger, WebhookId identifier, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Debug, "Webhook '{Identifier}' in namespace '{Namespace}' was already unsubscribed")]
    internal static partial void AlreadyUnsubscribed(this ILogger<Webhooks> logger, WebhookId identifier, EventStoreNamespaceName @namespace);
}
