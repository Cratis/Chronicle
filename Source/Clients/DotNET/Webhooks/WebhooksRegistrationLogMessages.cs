// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Webhooks;

internal static partial class WebhooksRegistrationLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Webhook registration failed for '{WebhookId}': {Details}")]
    internal static partial void RegisterWebhookFailed(this ILogger<Webhooks> logger, WebhookId webhookId, string details);
}
