// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Log messages for <see cref="WebhookDefinitionComparerLogging"/>.
/// </summary>
internal static partial class WebhookDefinitionComparerLogging
{
    [LoggerMessage(LogLevel.Debug, "Webhook {WebhookId} is new, no need to compare definitions")]
    internal static partial void WebhookIsNew(this ILogger<WebhookDefinitionComparer> logger, WebhookId webhookId);

    [LoggerMessage(LogLevel.Debug, "Comparing definitions for Webhook {WebhookId}")]
    internal static partial void ComparingDefinitions(this ILogger<WebhookDefinitionComparer> logger, WebhookId webhookId);
}
